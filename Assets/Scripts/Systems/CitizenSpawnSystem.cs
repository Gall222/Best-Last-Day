using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CitizenSpawnSystem : ISystem
    {
        private EntityQuery _configQuery;
        private EntityQuery _walkPointsQuery;
        private EntityQuery _citizensQuery;

        private EntityArchetype _citizenArchetype;

        private Random _random;

        // Минимальное расстояние между жителями.
        private const float MinSpawnDistance = 1.8f;

        // Максимальное смещение от WalkPoint.
        private const float SpawnRadius = 1.5f;

        // Сколько раз пробуем найти свободное место.
        private const int MaxPlacementAttempts = 50;

        public void OnCreate(ref SystemState state)
        {
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CitizenSpawnConfig>(),
                ComponentType.ReadOnly<CitizenVisualVariantElement>()
            );

            _walkPointsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<WalkPoint>()
            );

            // Все существующие жители, в том числе ручные.
            _citizensQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Citizen>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            // Описание структуры новой ECS-сущности.
            _citizenArchetype = state.EntityManager.CreateArchetype(
                ComponentType.ReadWrite<Citizen>(),
                ComponentType.ReadWrite<CitizenStateComponent>(),
                ComponentType.ReadWrite<CitizenMovement>(),
                ComponentType.ReadWrite<CitizenHealth>(),
                ComponentType.ReadWrite<CitizenNavigation>(),
                ComponentType.ReadWrite<CitizenAppearance>(),
                ComponentType.ReadWrite<LocalTransform>()
            );

            _random = Random.CreateFromIndex(12345);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_configQuery.CalculateEntityCount() != 1)
                return;

            EntityManager entityManager = state.EntityManager;

            Entity configEntity = _configQuery.GetSingletonEntity();

            var config = entityManager
                .GetComponentData<CitizenSpawnConfig>(configEntity);

            if (config.HasSpawned)
                return;

            if (config.Count <= 0)
            {
                config.HasSpawned = true;
                entityManager.SetComponentData(configEntity, config);
                return;
            }

            // Получаем точки маршрутов.
            using var walkPointEntities =
                _walkPointsQuery.ToEntityArray(Allocator.Temp);

            using var walkPoints =
                _walkPointsQuery.ToComponentDataArray<WalkPoint>(
                    Allocator.Temp
                );

            if (walkPointEntities.Length == 0)
            {
                UnityEngine.Debug.LogError(
                    "CitizenSpawnSystem: WalkPoint не найдены!"
                );
                return;
            }

            // Получаем визуальные варианты.
            var variantBuffer = entityManager
                .GetBuffer<CitizenVisualVariantElement>(
                    configEntity,
                    true
                );

            using var variants =
                variantBuffer.ToNativeArray(Allocator.Temp);

            if (variants.Length == 0)
            {
                UnityEngine.Debug.LogError(
                    "CitizenSpawnSystem: список визуальных вариантов пуст!"
                );
                return;
            }

            // Считаем общий вес вариантов.
            int totalWeight = 0;

            for (int i = 0; i < variants.Length; i++)
            {
                totalWeight += math.max(1, variants[i].Weight);
            }

            if (totalWeight <= 0)
            {
                UnityEngine.Debug.LogError(
                    "CitizenSpawnSystem: нет корректных визуальных вариантов!"
                );
                return;
            }

            // Запоминаем позиции уже существующих жителей.
            using var existingTransforms =
                _citizensQuery.ToComponentDataArray<LocalTransform>(
                    Allocator.Temp
                );

            using var occupiedPositions =
                new NativeList<float3>(Allocator.Temp);

            foreach (var transform in existingTransforms)
            {
                occupiedPositions.Add(transform.Position);
            }

            float minDistanceSq =
                MinSpawnDistance * MinSpawnDistance;

            int createdCount = 0;

            for (int i = 0; i < config.Count; i++)
            {
                // Ищем свободное место.
                bool positionFound = false;

                float3 spawnPosition = float3.zero;
                Entity startPoint = Entity.Null;

                for (int attempt = 0;
                     attempt < MaxPlacementAttempts;
                     attempt++)
                {
                    // Случайная точка маршрута.
                    int pointIndex = _random.NextInt(
                        0,
                        walkPointEntities.Length
                    );

                    float3 pointPosition =
                        walkPoints[pointIndex].Position;

                    // Случайное смещение в пределах круга.
                    float angle = _random.NextFloat(
                        0f,
                        math.PI * 2f
                    );

                    float radius = math.sqrt(
                        _random.NextFloat()
                    ) * SpawnRadius;

                    float3 offset = new float3(
                        math.cos(angle) * radius,
                        0f,
                        math.sin(angle) * radius
                    );

                    float3 candidatePosition =
                        pointPosition + offset;

                    // Проверяем расстояние до занятых мест.
                    bool isOccupied = false;

                    for (int j = 0;
                         j < occupiedPositions.Length;
                         j++)
                    {
                        float3 difference =
                            candidatePosition - occupiedPositions[j];

                        // Только горизонтальное расстояние.
                        difference.y = 0f;

                        if (math.lengthsq(difference) < minDistanceSq)
                        {
                            isOccupied = true;
                            break;
                        }
                    }

                    if (isOccupied)
                        continue;

                    spawnPosition = candidatePosition;
                    startPoint = walkPointEntities[pointIndex];

                    positionFound = true;
                    break;
                }

                if (!positionFound)
                {
                    UnityEngine.Debug.LogWarning(
                        "CitizenSpawnSystem: не удалось найти " +
                        $"свободное место для жителя {i + 1}."
                    );

                    continue;
                }

                // Выбираем визуальный вариант с учётом веса.
                int value = _random.NextInt(0, totalWeight);

                int selectedVariantId = -1;

                for (int j = 0; j < variants.Length; j++)
                {
                    value -= math.max(1, variants[j].Weight);

                    if (value < 0)
                    {
                        selectedVariantId = variants[j].VariantId;
                        break;
                    }
                }

                if (selectedVariantId < 0)
                    continue;

                // Создаём пустую Entity с нужным набором компонентов.
                Entity citizen = entityManager.CreateEntity(
                    _citizenArchetype
                );

                // Позиция и поворот.
                entityManager.SetComponentData(
                    citizen,
                    LocalTransform.FromPositionRotationScale(
                        spawnPosition,
                        quaternion.identity,
                        1f
                    )
                );

                // Начальное состояние.
                entityManager.SetComponentData(
                    citizen,
                    new CitizenStateComponent
                    {
                        Value = CitizenState.Walking
                    }
                );

                // Параметры движения.
                entityManager.SetComponentData(
                    citizen,
                    new CitizenMovement
                    {
                        Speed = config.Speed,
                        RunSpeed = config.RunSpeed,
                        RotationSpeed = config.RotationSpeed
                    }
                );

                // Здоровье.
                entityManager.SetComponentData(
                    citizen,
                    new CitizenHealth
                    {
                        Current = config.MaxHealth,
                        Max = config.MaxHealth
                    }
                );

                // Внешность.
                entityManager.SetComponentData(
                    citizen,
                    new CitizenAppearance
                    {
                        VariantId = selectedVariantId
                    }
                );

                // Начальная навигация.
                entityManager.SetComponentData(
                    citizen,
                    new CitizenNavigation
                    {
                        CurrentPoint = startPoint,
                        TargetPosition = spawnPosition,
                        Random = Random.CreateFromIndex(
                            (uint)(i + 1000)
                        )
                    }
                );

                // Занимаем найденное место.
                occupiedPositions.Add(spawnPosition);

                createdCount++;
            }

            config.HasSpawned = true;

            entityManager.SetComponentData(
                configEntity,
                config
            );

            UnityEngine.Debug.Log(
                $"CitizenSpawnSystem: создано {createdCount} " +
                $"из {config.Count} жителей."
            );
        }
    }
}