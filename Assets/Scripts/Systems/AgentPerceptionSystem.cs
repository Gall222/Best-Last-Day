using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AgentPerceptionSystem : ISystem
    {
        private EntityQuery _zombieQuery;

        private const float DetectionRadius = 12f;
        private const float ForgetThreatRadius = 15f;
        
        public void OnCreate(ref SystemState state)
        {
            _zombieQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ZombieTag>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            using var zombies = _zombieQuery.ToEntityArray(Allocator.Temp);

            var transformLookup =
                SystemAPI.GetComponentLookup<LocalTransform>(true);

            var zombieLookup =
                SystemAPI.GetComponentLookup<ZombieTag>(true);

            float detectionRadiusSq = DetectionRadius * DetectionRadius;
            float forgetThreatRadiusSq = ForgetThreatRadius * ForgetThreatRadius;

            foreach (var (perception, transform) in
                     SystemAPI.Query<
                             RefRW<AgentPerception>,
                             RefRO<LocalTransform>>()
                         .WithAll<Citizen>()
                         .WithNone<ZombieTag>())
            {
                float3 citizenPosition = transform.ValueRO.Position;

                Entity nearestThreat = Entity.Null;
                float nearestDistanceSq = detectionRadiusSq;

                // Сначала проверяем, не находится ли прежняя угроза
                // всё ещё в радиусе сохранения.
                Entity previousThreat = perception.ValueRO.Threat;

                if (previousThreat != Entity.Null &&
                    zombieLookup.HasComponent(previousThreat) &&
                    transformLookup.HasComponent(previousThreat))
                {
                    float distanceSq = math.distancesq(
                        citizenPosition,
                        transformLookup[previousThreat].Position
                    );

                    if (distanceSq <= forgetThreatRadiusSq)
                    {
                        nearestThreat = previousThreat;
                        nearestDistanceSq = distanceSq;
                    }
                }

                // Если прежняя угроза потеряна, ищем новую.
                if (nearestThreat == Entity.Null)
                {
                    for (int i = 0; i < zombies.Length; i++)
                    {
                        Entity zombie = zombies[i];

                        float distanceSq = math.distancesq(
                            citizenPosition,
                            transformLookup[zombie].Position
                        );

                        if (distanceSq < nearestDistanceSq)
                        {
                            nearestDistanceSq = distanceSq;
                            nearestThreat = zombie;
                        }
                    }
                }

                perception.ValueRW.Threat = nearestThreat;

                if (nearestThreat != Entity.Null)
                {
                    perception.ValueRW.HasThreat = 1;
                    perception.ValueRW.ThreatDistance =
                        math.sqrt(nearestDistanceSq);
                }
                else
                {
                    perception.ValueRW.HasThreat = 0;
                    perception.ValueRW.ThreatDistance = float.MaxValue;
                }
            }
        }
    }
}