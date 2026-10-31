using Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [BurstCompile]
    public partial struct CitizenNavigationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var pointLookup =
                SystemAPI.GetComponentLookup<WalkPoint>(true);

            var neighbourLookup =
                SystemAPI.GetBufferLookup<WalkPointNeighbour>(true);

            foreach (var (
                         transform,
                         movement,
                         navigation,
                         citizenState
                         ) in SystemAPI.Query<
                         RefRW<LocalTransform>,
                         RefRO<CitizenMovement>,
                         RefRW<CitizenNavigation>,
                         RefRO<CitizenStateComponent>>())
            {
                if (citizenState.ValueRO.Value != CitizenState.Walking)
                    continue;

                float3 currentPosition = transform.ValueRO.Position;
                float3 targetPosition = navigation.ValueRO.TargetPosition;

                float3 direction = targetPosition - currentPosition;
                float distance = math.length(direction);

                // Достигли текущей точки.
                if (distance < 0.15f)
                {
                    Entity currentPoint = navigation.ValueRO.CurrentPoint;

                    if (!neighbourLookup.HasBuffer(currentPoint))
                        continue;

                    var neighbours = neighbourLookup[currentPoint];

                    if (neighbours.Length == 0)
                    {
                        continue;
                    }

                    // Выбираем случайную соседнюю точку.
                    int index = navigation.ValueRW.Random.NextInt(
                        0,
                        neighbours.Length
                    );

                    Entity nextPoint = neighbours[index].Entity;

                    if (!pointLookup.HasComponent(nextPoint))
                        continue;

                    navigation.ValueRW.CurrentPoint = nextPoint;
                    navigation.ValueRW.TargetPosition =
                        pointLookup[nextPoint].Position;

                    continue;
                }

                direction /= distance;

                // Перемещение.
                transform.ValueRW.Position +=
                    direction *
                    movement.ValueRO.Speed *
                    deltaTime;

                // Поворот персонажа по горизонтали.
                float3 horizontalDirection = direction;
                horizontalDirection.y = 0f;

                if (math.lengthsq(horizontalDirection) > 0.001f)
                {
                    quaternion targetRotation =
                        quaternion.LookRotationSafe(
                            horizontalDirection,
                            math.up()
                        );

                    transform.ValueRW.Rotation = math.slerp(
                        transform.ValueRO.Rotation,
                        targetRotation,
                        movement.ValueRO.RotationSpeed * deltaTime
                    );
                }
            }
        }
    }
}