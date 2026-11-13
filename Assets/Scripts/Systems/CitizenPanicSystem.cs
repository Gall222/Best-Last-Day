using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AgentDecisionSystem))]
    [UpdateBefore(typeof(CitizenNavigationSystem))]
    public partial struct CitizenPanicSystem : ISystem
    {
        private const float PanicDistance = 8f;
        private const float TargetReachedDistance = 0.5f;

        private EntityQuery _walkPointQuery;

        public void OnCreate(ref SystemState state)
        {
            _walkPointQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<WalkPoint>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            var transformLookup =
                SystemAPI.GetComponentLookup<LocalTransform>(true);

            NativeArray<Entity> walkPointEntities = default;
            NativeArray<WalkPoint> walkPoints = default;
            bool walkPointsLoaded = false;

            foreach (var (citizenState, navigation, transform, decision, perception)
                     in SystemAPI.Query<
                         RefRW<CitizenStateComponent>,
                         RefRW<CitizenNavigation>,
                         RefRO<LocalTransform>,
                         RefRO<AgentDecision>,
                         RefRO<AgentPerception>>()
                         .WithAll<Citizen>())
            {
                float3 position = transform.ValueRO.Position;

                bool hasThreat =
                    perception.ValueRO.HasThreat != 0 &&
                    transformLookup.HasComponent(perception.ValueRO.Threat);

                // Начало или продолжение паники.
                if (hasThreat &&
                    (decision.ValueRO.Action == AgentAction.Panic ||
                     citizenState.ValueRO.Value == CitizenState.Panic))
                {
                    bool shouldChooseNewTarget =
                        citizenState.ValueRO.Value != CitizenState.Panic;

                    if (!shouldChooseNewTarget)
                    {
                        float distanceToTarget = math.distance(
                            position,
                            navigation.ValueRO.TargetPosition
                        );

                        shouldChooseNewTarget =
                            distanceToTarget < TargetReachedDistance;
                    }

                    if (shouldChooseNewTarget)
                    {
                        float3 threatPosition =
                            transformLookup[perception.ValueRO.Threat].Position;

                        float3 fleeDirection = position - threatPosition;
                        fleeDirection.y = 0f;

                        fleeDirection = math.normalizesafe(
                            fleeDirection,
                            new float3(1f, 0f, 0f)
                        );

                        navigation.ValueRW.TargetPosition =
                            position + fleeDirection * PanicDistance;

                        citizenState.ValueRW.Value = CitizenState.Panic;
                    }

                    continue;
                }

                // Угроза исчезла: возвращаемся к обычной навигации.
                if (citizenState.ValueRO.Value == CitizenState.Panic &&
                    !hasThreat)
                {
                    if (!walkPointsLoaded)
                    {
                        walkPointEntities = _walkPointQuery.ToEntityArray(
                            state.WorldUpdateAllocator
                        );

                        walkPoints = _walkPointQuery.ToComponentDataArray<WalkPoint>(
                            state.WorldUpdateAllocator
                        );

                        walkPointsLoaded = true;
                    }

                    Entity nearestPoint = Entity.Null;
                    float nearestDistanceSq = float.MaxValue;

                    for (int i = 0; i < walkPoints.Length; i++)
                    {
                        float distanceSq = math.distancesq(
                            position,
                            walkPoints[i].Position
                        );

                        if (distanceSq < nearestDistanceSq)
                        {
                            nearestDistanceSq = distanceSq;
                            nearestPoint = walkPointEntities[i];
                        }
                    }

                    if (nearestPoint != Entity.Null)
                    {
                        navigation.ValueRW.CurrentPoint = nearestPoint;

                        // Используем именно позицию найденной точки.
                        for (int i = 0; i < walkPointEntities.Length; i++)
                        {
                            if (walkPointEntities[i] == nearestPoint)
                            {
                                navigation.ValueRW.TargetPosition =
                                    walkPoints[i].Position;
                                break;
                            }
                        }

                        citizenState.ValueRW.Value = CitizenState.Walking;
                    }
                    else
                    {
                        citizenState.ValueRW.Value = CitizenState.Idle;
                        navigation.ValueRW.CurrentPoint = Entity.Null;
                    }
                }
            }
        }
    }
}