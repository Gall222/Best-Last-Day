
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

        private Random _random;

        public void OnCreate(ref SystemState state)
        {
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CitizenSpawnConfig>(),
                ComponentType.ReadOnly<CitizenPrefabElement>()
            );

            _walkPointsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<WalkPoint>()
            );

            // Начальное значение генератора случайных чисел.
            _random = Random.CreateFromIndex(12345);
        }

        public void OnUpdate(ref SystemState state)
        {
            // Нужен ровно один объект с настройками спавна.
            if (_configQuery.CalculateEntityCount() != 1)
                return;

            Entity configEntity = _configQuery.GetSingletonEntity();

            var entityManager = state.EntityManager;

            var config = entityManager
                .GetComponentData<CitizenSpawnConfig>(configEntity);

            // Спавн выполняется только один раз.
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

            // Копируем каталог префабов.
            // Это важно: ниже будут структурные изменения ECS,
            // поэтому не держим DynamicBuffer во время Instantiate.
            var prefabBuffer = entityManager
                .GetBuffer<CitizenPrefabElement>(configEntity, true);

            using var prefabs =
                prefabBuffer.ToNativeArray(Allocator.Temp);

            if (prefabs.Length == 0)
            {
                UnityEngine.Debug.LogError(
                    "CitizenSpawnSystem: список префабов пуст!"
                );

                return;
            }

            // Считаем общий вес всех вариантов.
            int totalWeight = 0;

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i].Prefab == Entity.Null)
                    continue;

                totalWeight += math.max(1, prefabs[i].Weight);
            }

            if (totalWeight <= 0)
            {
                UnityEngine.Debug.LogError(
                    "CitizenSpawnSystem: нет корректных префабов!"
                );

                return;
            }

            // Создаём жителей.
            for (int i = 0; i < config.Count; i++)
            {
                // Выбираем префаб с учётом веса.
                int value = _random.NextInt(0, totalWeight);

                Entity selectedPrefab = Entity.Null;
                int selectedIndex = -1;

                for (int j = 0; j < prefabs.Length; j++)
                {
                    if (prefabs[j].Prefab == Entity.Null)
                        continue;

                    value -= math.max(1, prefabs[j].Weight);

                    if (value < 0)
                    {
                        selectedPrefab = prefabs[j].Prefab;
                        selectedIndex = j;
                        break;
                    }
                }

                if (selectedPrefab == Entity.Null)
                    continue;

                // Выбираем случайную точку маршрута.
                int pointIndex = _random.NextInt(
                    0,
                    walkPointEntities.Length
                );

                Entity startPoint = walkPointEntities[pointIndex];
                float3 position = walkPoints[pointIndex].Position;

                // Создаём ECS-сущность из выбранного префаба.
                Entity citizen = entityManager.Instantiate(
                    selectedPrefab
                );

                // Устанавливаем позицию.
                if (entityManager.HasComponent<LocalTransform>(citizen))
                {
                    var transform =
                        entityManager.GetComponentData<LocalTransform>(
                            citizen
                        );

                    transform.Position = position;

                    entityManager.SetComponentData(
                        citizen,
                        transform
                    );
                }

                // Назначаем состояние.
                entityManager.SetComponentData(
                    citizen,
                    new CitizenStateComponent
                    {
                        Value = CitizenState.Walking
                    }
                );

                // Сохраняем выбранный вариант внешности.
                entityManager.AddComponentData(
                    citizen,
                    new CitizenAppearance
                    {
                        VariantId = selectedIndex
                    }
                );

                // Добавляем навигацию.
                var navigation = new CitizenNavigation
                {
                    CurrentPoint = startPoint,
                    TargetPosition = position,
                    Random = Random.CreateFromIndex(
                        (uint)(i + 1000)
                    )
                };

                if (entityManager.HasComponent<CitizenNavigation>(citizen))
                {
                    entityManager.SetComponentData(
                        citizen,
                        navigation
                    );
                }
                else
                {
                    entityManager.AddComponentData(
                        citizen,
                        navigation
                    );
                }
            }

            // Запоминаем, что спавн завершён.
            config.HasSpawned = true;

            entityManager.SetComponentData(
                configEntity,
                config
            );

            UnityEngine.Debug.Log(
                $"CitizenSpawnSystem: создано жителей: {config.Count}"
            );
        }
    }
}
