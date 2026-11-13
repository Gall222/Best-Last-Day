
using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    public partial struct CitizenNavigationSystem : ISystem
    {
        private const float ArrivalDistance = 0.15f;

        // Размер ячейки пространственной сетки.
        private const float CellSize = 1.5f;

        // Радиус обнаружения соседних жителей.
        private const float AvoidanceRadius = 1.2f;

        // Сила отклонения от соседних жителей.
        private const float AvoidanceStrength = 1.5f;

        private EntityQuery _citizenQuery;
        
        public void OnCreate(ref SystemState state)
        {
            _citizenQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Citizen>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var pointLookup =
                SystemAPI.GetComponentLookup<WalkPoint>(true);

            var neighbourLookup =
                SystemAPI.GetBufferLookup<WalkPointNeighbour>(true);

            // Получаем позиции всех жителей.
            var transforms = _citizenQuery.ToComponentDataArray<LocalTransform>(
                state.WorldUpdateAllocator
            );

            // Создаём пространственную сетку.
            var grid = new NativeParallelMultiHashMap<int2, float3>(
                math.max(1, transforms.Length),
                state.WorldUpdateAllocator
            );

            // Распределяем жителей по ячейкам.
            foreach (var citizenTransform in transforms)
            {
                float3 position = citizenTransform.Position;

                int2 cell = (int2)math.floor(
                    position.xz / CellSize
                );

                grid.Add(cell, position);
            }

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
                CitizenState currentState = citizenState.ValueRO.Value;

                if (currentState != CitizenState.Walking &&
                    currentState != CitizenState.Panic)
                    continue;

                float3 currentPosition = transform.ValueRO.Position;
                float3 targetPosition = navigation.ValueRO.TargetPosition;

                float3 direction = targetPosition - currentPosition;
                float distance = math.length(direction);

                // Достигли текущей точки.
                if (distance < ArrivalDistance)
                {
                    if (currentState != CitizenState.Walking)
                        continue;
                    
                    Entity currentPoint = navigation.ValueRO.CurrentPoint;

                    if (!pointLookup.HasComponent(currentPoint))
                        continue;

                    if (!neighbourLookup.HasBuffer(currentPoint))
                        continue;

                    var neighbours = neighbourLookup[currentPoint];

                    if (neighbours.Length == 0)
                        continue;

                    // Выбираем случайную соседнюю точку.
                    int index = navigation.ValueRW.Random.NextInt(
                        0,
                        neighbours.Length
                    );

                    Entity nextPoint = neighbours[index].Entity;

                    if (!pointLookup.HasComponent(nextPoint))
                        continue;

                    float3 nextPosition = pointLookup[nextPoint].Position;

                    // Направление между точками маршрута.
                    float3 segmentDirection =
                        nextPosition - pointLookup[currentPoint].Position;

                    segmentDirection.y = 0f;

                    segmentDirection = math.normalizesafe(
                        segmentDirection
                    );

                    // Перпендикуляр к маршруту.
                    float3 perpendicular = new float3(
                        -segmentDirection.z,
                        0f,
                        segmentDirection.x
                    );

                    // Индивидуальное боковое смещение.
                    navigation.ValueRW.TargetPosition =
                        nextPosition +
                        perpendicular * navigation.ValueRO.LateralOffset;

                    navigation.ValueRW.CurrentPoint = nextPoint;

                    continue;
                }

                direction /= distance;

                // Ищем ближайших жителей в пространственной сетке.
                int2 currentCell = (int2)math.floor(
                    currentPosition.xz / CellSize
                );

                float3 avoidance = float3.zero;

                float avoidanceRadiusSq =
                    AvoidanceRadius * AvoidanceRadius;

                // Проверяем текущую и восемь соседних ячеек.
                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        int2 cell = currentCell + new int2(x, z);

                        if (!grid.TryGetFirstValue(
                                cell,
                                out float3 neighbourPosition,
                                out var iterator))
                        {
                            continue;
                        }

                        do
                        {
                            float3 away =
                                currentPosition - neighbourPosition;

                            // Учитываем только горизонтальное расстояние.
                            away.y = 0f;

                            float distanceSq = math.lengthsq(away);

                            if (distanceSq < 0.0001f ||
                                distanceSq >= avoidanceRadiusSq)
                            {
                                continue;
                            }

                            float neighbourDistance =
                                math.sqrt(distanceSq);

                            // Чем ближе сосед, тем сильнее отклонение.
                            float weight =
                                1f - neighbourDistance / AvoidanceRadius;

                            avoidance +=
                                away / neighbourDistance * weight;

                        } while (grid.TryGetNextValue(
                                     out neighbourPosition,
                                     ref iterator
                                 ));
                    }
                }

                // Ограничиваем силу отклонения.
                float avoidanceLength = math.length(avoidance);

                if (avoidanceLength > 0.001f)
                {
                    avoidance =
                        math.normalizesafe(avoidance) *
                        math.saturate(avoidanceLength);
                }

                // Формируем горизонтальное направление движения.
                float3 horizontalDirection = new float3(
                    direction.x,
                    0f,
                    direction.z
                );

                horizontalDirection = math.normalizesafe(
                    horizontalDirection
                );

                horizontalDirection +=
                    avoidance * AvoidanceStrength;

                horizontalDirection = math.normalizesafe(
                    horizontalDirection
                );

                // Сохраняем вертикальную составляющую движения.
                float3 finalDirection = new float3(
                    horizontalDirection.x,
                    direction.y,
                    horizontalDirection.z
                );

                finalDirection = math.normalizesafe(finalDirection);

                // Перемещение.
                float currentSpeed = currentState == CitizenState.Panic
                    ? movement.ValueRO.RunSpeed
                    : movement.ValueRO.Speed;

                float step = math.min(
                    currentSpeed * deltaTime,
                    distance
                );

                transform.ValueRW.Position += finalDirection * step;

                // Поворот персонажа.
                float3 rotationDirection = horizontalDirection;

                if (math.lengthsq(rotationDirection) > 0.001f)
                {
                    quaternion targetRotation =
                        quaternion.LookRotationSafe(
                            rotationDirection,
                            math.up()
                        );

                    transform.ValueRW.Rotation = math.slerp(
                        transform.ValueRO.Rotation,
                        targetRotation,
                        math.saturate(
                            movement.ValueRO.RotationSpeed * deltaTime
                        )
                    );
                }
            }
        }
    }
}
