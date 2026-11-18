
using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class KnockdownDetectionSystem : SystemBase
    {
        private const float CellSize = 2f;
        private const float CollisionCooldownDuration = 0.5f;

        private EntityQuery _configQuery;
        private EntityQuery _citizenQuery;

        protected override void OnCreate()
        {
            _configQuery = GetEntityQuery(
                ComponentType.ReadOnly<KnockdownConfig>(),
                ComponentType.ReadWrite<KnockdownEvent>()
            );

            _citizenQuery = GetEntityQuery(
                ComponentType.ReadOnly<KnockdownSource>(),
                ComponentType.ReadWrite<KnockdownTarget>(),
                ComponentType.ReadOnly<CitizenStateComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            RequireForUpdate(_configQuery);
        }

        protected override void OnUpdate()
        {
            if (_configQuery.CalculateEntityCount() != 1)
                return;

            var configEntity = _configQuery.GetSingletonEntity();
            var config = EntityManager.GetComponentData<KnockdownConfig>(
                configEntity
            );

            var events = EntityManager.GetBuffer<KnockdownEvent>(
                configEntity
            );

            // События предыдущего кадра больше не нужны.
            events.Clear();

            int entityCount = _citizenQuery.CalculateEntityCount();

            if (entityCount < 2)
                return;

            // Создаём пространственную сетку.
            // В каждой ячейке хранятся сущности, находящиеся в ней.
            var grid = new NativeParallelMultiHashMap<int2, Entity>(
                math.max(1, entityCount),
                Allocator.Temp
            );

            var sourceLookup =
                SystemAPI.GetComponentLookup<KnockdownSource>(true);

            var targetLookup =
                SystemAPI.GetComponentLookup<KnockdownTarget>(false);

            var stateLookup =
                SystemAPI.GetComponentLookup<CitizenStateComponent>(true);

            var transformLookup =
                SystemAPI.GetComponentLookup<LocalTransform>(true);

            var zombieLookup =
                SystemAPI.GetComponentLookup<ZombieTag>(true);

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Этап 1. Раскладываем жителей по ячейкам.
            foreach (var (transform, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<
                             KnockdownSource,
                             KnockdownTarget,
                             CitizenStateComponent
                         >()
                         .WithEntityAccess())
            {
                int2 cell = GetCell(transform.ValueRO.Position);

                grid.Add(cell, entity);
            }

            // Этап 2. Проверяем соседние ячейки.
            foreach (var (transform, entityA) in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<
                             KnockdownSource,
                             KnockdownTarget,
                             CitizenStateComponent
                         >()
                         .WithEntityAccess())
            {
                var sourceA = sourceLookup[entityA];
                var targetA = targetLookup[entityA];

                // Уменьшаем задержку повторного столкновения.
                targetA.CollisionCooldown = math.max(
                    0f,
                    targetA.CollisionCooldown - deltaTime
                );

                targetLookup[entityA] = targetA;

                float2 positionA = new float2(
                    transform.ValueRO.Position.x,
                    transform.ValueRO.Position.z
                );

                int2 cellA = GetCell(transform.ValueRO.Position);

                // Проверяем текущую и восемь соседних ячеек.
                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        int2 neighborCell = cellA + new int2(x, z);

                        if (!grid.TryGetFirstValue(
                                neighborCell,
                                out Entity entityB,
                                out NativeParallelMultiHashMapIterator<int2> iterator
                            ))
                        {
                            continue;
                        }

                        do
                        {
                            // Каждую пару проверяем только один раз.
                            if (entityA.Index >= entityB.Index)
                                continue;

                            var sourceB = sourceLookup[entityB];
                            var targetB = targetLookup[entityB];

                            float2 positionB = new float2(
                                transformLookup[entityB].Position.x,
                                transformLookup[entityB].Position.z
                            );

                            float radiusA = math.max(
                                sourceA.Radius,
                                targetA.Radius
                            );

                            float radiusB = math.max(
                                sourceB.Radius,
                                targetB.Radius
                            );

                            float combinedRadius = radiusA + radiusB;

                            // Проверяем реальное пересечение радиусов.
                            if (math.distancesq(positionA, positionB) >
                                combinedRadius * combinedRadius)
                            {
                                continue;
                            }

                            var stateA = stateLookup[entityA].Value;
                            var stateB = stateLookup[entityB].Value;

                            bool panicCollision =
                                stateA == CitizenState.Panic ||
                                stateB == CitizenState.Panic;

                            // Проверяем возможность падения в обе стороны.
                            bool canAffectB =
                                sourceA.Enabled != 0 &&
                                targetA.State == BodyState.Standing &&
                                targetB.State == BodyState.Standing &&
                                targetB.CollisionCooldown <= 0f;

                            bool canAffectA =
                                sourceB.Enabled != 0 &&
                                targetB.State == BodyState.Standing &&
                                targetA.State == BodyState.Standing &&
                                targetA.CollisionCooldown <= 0f;

                            // Житель A сбивает жителя B.
                            if (canAffectB)
                            {
                                float baseChance = zombieLookup.HasComponent(entityA)
                                    ? config.ZombieCollisionChance
                                    : panicCollision
                                        ? config.PanicCollisionChance
                                        : config.CitizenCollisionChance;

                                float chance = math.saturate(
                                    baseChance *
                                    sourceA.CollisionModifier *
                                    targetB.Susceptibility
                                );

                                if (chance > 0f)
                                {
                                    events.Add(new KnockdownEvent
                                    {
                                        Source = entityA,
                                        Target = entityB,
                                        Chance = chance
                                    });

                                    targetB.CollisionCooldown =
                                        CollisionCooldownDuration;

                                    targetLookup[entityB] = targetB;
                                }
                            }

                            // Житель B сбивает жителя A.
                            if (canAffectA)
                            {
                                float baseChance = zombieLookup.HasComponent(entityB)
                                    ? config.ZombieCollisionChance
                                    : panicCollision
                                        ? config.PanicCollisionChance
                                        : config.CitizenCollisionChance;

                                float chance = math.saturate(
                                    baseChance *
                                    sourceB.CollisionModifier *
                                    targetA.Susceptibility
                                );

                                if (chance > 0f)
                                {
                                    events.Add(new KnockdownEvent
                                    {
                                        Source = entityB,
                                        Target = entityA,
                                        Chance = chance
                                    });

                                    targetA.CollisionCooldown =
                                        CollisionCooldownDuration;

                                    targetLookup[entityA] = targetA;
                                }
                            }

                        } while (grid.TryGetNextValue(
                            out entityB,
                            ref iterator
                        ));
                    }
                }
            }

            grid.Dispose();
        }

        private static int2 GetCell(float3 position)
        {
            return (int2)math.floor(
                new float2(position.x, position.z) / CellSize
            );
        }
    }
}
