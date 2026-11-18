
using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CitizenDeathProgressSystem))]
    public partial struct CitizenReviveSystem : ISystem
    {
        private EntityQuery _configQuery;

        public void OnCreate(ref SystemState state)
        {
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ZombieVirusConfig>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            // Конфигурация должна существовать в единственном экземпляре.
            if (_configQuery.CalculateEntityCount() != 1)
                return;

            ZombieVirusConfig config =
                _configQuery.GetSingleton<ZombieVirusConfig>();

            float deltaTime = SystemAPI.Time.DeltaTime;

            EntityCommandBuffer ecb =
                new EntityCommandBuffer(Allocator.Temp);

            foreach (var (deathProgress, movement, entity) in
                SystemAPI.Query<
                    RefRW<CitizenDeathProgress>,
                    RefRO<CitizenMovement>>()
                    .WithAll<Citizen>()
                    .WithNone<ZombieTag>()
                    .WithEntityAccess())
            {
                // Сначала персонаж должен полностью изменить цвет кожи.
                if (deathProgress.ValueRO.Amount < 1f)
                    continue;

                // После этого начинается отсчёт времени до превращения.
                deathProgress.ValueRW.ReviveElapsedTime += deltaTime;

                if (deathProgress.ValueRO.ReviveElapsedTime <
                    math.max(0f, config.ReviveDelay))
                {
                    continue;
                }

                float zombieHealth = math.max(
                    1f,
                    config.ZombieMaxHealth
                );

                // Помечаем существующую сущность как зомби.
                ecb.AddComponent<ZombieTag>(entity);

                ecb.AddComponent(entity, new ZombieMovement
                {
                    Speed = movement.ValueRO.Speed,
                    RotationSpeed = movement.ValueRO.RotationSpeed
                });

                ecb.AddComponent(entity, new ZombieTarget
                {
                    Target = Entity.Null
                });

                ecb.AddComponent(entity, new ZombieAttack
                {
                    Damage = config.ZombieDamage,
                    AttackRange = 1.8f,
                    AttackCooldown = 1.5f,
                    CooldownRemaining = 0f
                });

                ecb.AddComponent(entity, new ZombieHealth
                {
                    Current = zombieHealth,
                    Max = zombieHealth
                });

                // Меняем состояние, не создавая новую сущность.
                ecb.SetComponent(entity, new CitizenStateComponent
                {
                    Value = CitizenState.Zombie
                });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
