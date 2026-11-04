using System.Collections.Generic;
using Components;
using Settings;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Visuals
{
    public class CitizenVisualManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CitizenSpawnSettings settings;

        [Header("Visuals")]
        [SerializeField] private Transform visualsRoot;

        private EntityManager _entityManager;
        private EntityQuery _citizenQuery;

        private readonly Dictionary<Entity, GameObject> _visuals = new();
        private readonly HashSet<Entity> _activeEntities = new();
        private readonly List<Entity> _entitiesToRemove = new();

        private bool _initialized;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (settings == null)
            {
                Debug.LogError(
                    "CitizenVisualManager: Settings не назначен!",
                    this
                );

                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
            {
                Debug.LogError(
                    "CitizenVisualManager: ECS World не найден!",
                    this
                );

                return;
            }

            _entityManager = world.EntityManager;

            _citizenQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Citizen>(),
                ComponentType.ReadOnly<CitizenAppearance>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
                return;

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _activeEntities.Clear();

            using var entities =
                _citizenQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in entities)
            {
                _activeEntities.Add(entity);

                var appearance =
                    _entityManager.GetComponentData<CitizenAppearance>(
                        entity
                    );

                var localTransform =
                    _entityManager.GetComponentData<LocalTransform>(
                        entity
                    );

                // Если визуальная модель ещё не создана.
                if (!_visuals.TryGetValue(entity, out GameObject visual))
                {
                    visual = CreateVisual(appearance.VariantId);

                    if (visual == null)
                        continue;

                    _visuals.Add(entity, visual);
                }

                // Синхронизируем позицию и поворот.
                UpdateVisualTransform(visual.transform, localTransform);
            }

            // Удаляем визуальные объекты тех ECS-сущностей,
            // которые больше не существуют.
            _entitiesToRemove.Clear();

            foreach (var pair in _visuals)
            {
                if (!_activeEntities.Contains(pair.Key))
                {
                    Destroy(pair.Value);
                    _entitiesToRemove.Add(pair.Key);
                }
            }

            foreach (Entity entity in _entitiesToRemove)
            {
                _visuals.Remove(entity);
            }
        }

        private GameObject CreateVisual(int variantId)
        {
            if (variantId < 0 ||
                variantId >= settings.prefabs.Count)
            {
                Debug.LogError(
                    $"CitizenVisualManager: неправильный VariantId: {variantId}"
                );

                return null;
            }

            GameObject prefab = settings.prefabs[variantId].prefab;

            if (prefab == null)
            {
                Debug.LogError(
                    $"CitizenVisualManager: префаб {variantId} не назначен!"
                );

                return null;
            }

            return Instantiate(prefab, visualsRoot);
        }

        private void UpdateVisualTransform(
            Transform visual,
            LocalTransform localTransform)
        {
            float3 position = localTransform.Position;
            quaternion rotation = localTransform.Rotation;

            visual.SetPositionAndRotation(
                new Vector3(position.x, position.y, position.z),
                new Quaternion(
                    rotation.value.x,
                    rotation.value.y,
                    rotation.value.z,
                    rotation.value.w
                )
            );

            visual.localScale = Vector3.one * localTransform.Scale;
        }

        private void OnDestroy()
        {
            foreach (var visual in _visuals.Values)
            {
                if (visual != null)
                    Destroy(visual);
            }

            _visuals.Clear();

            _initialized = false;
        }
    }
}