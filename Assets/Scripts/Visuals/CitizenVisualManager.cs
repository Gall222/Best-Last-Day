using System.Collections.Generic;
using Components;
using Settings;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

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

        private readonly Dictionary<Entity, VisualInstance> _visuals = new();
        private readonly HashSet<Entity> _activeEntities = new();
        private readonly List<Entity> _entitiesToRemove = new();

        private bool _initialized;

        // Animation properties
        private static readonly int AnimStateHash =
            Animator.StringToHash("AnimState");

        private static readonly int IdleStateHash =
            Animator.StringToHash("Base Layer.idle");

        private static readonly int WalkingStateHash =
            Animator.StringToHash("Base Layer.walking");

        private static readonly int SlowRunStateHash =
            Animator.StringToHash("Base Layer.slow run");

        // Skin shader properties
        private static readonly int HealthySkinColorId =
            Shader.PropertyToID("_HealthySkinColor");

        private static readonly int DeadSkinColorId =
            Shader.PropertyToID("_DeadSkinColor");

        private static readonly int DeathAmountId =
            Shader.PropertyToID("_DeathAmount");

        // Skin material slots
        private static readonly int[] SkinMaterialSlots =
        {
            1, 2, 6, 7
        };

        private sealed class SkinMaterialSlot
        {
            public int Index;

            public MaterialPropertyBlock PropertyBlock;

            public Color HealthySkinColor;
            public Color DeadSkinColor;
        }

        private sealed class VisualInstance
        {
            public GameObject GameObject;
            public Transform Transform;
            public Animator Animator;

            public SkinnedMeshRenderer SkinRenderer;

            public readonly List<SkinMaterialSlot> SkinSlots = new();

            public int LastAnimationState = -1;
            public float LastDeathAmount = -1f;
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (settings == null || visualsRoot == null)
            {
                Debug.LogError(
                    "CitizenVisualManager: Settings или VisualsRoot не назначен!",
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
                ComponentType.ReadOnly<CitizenStateComponent>(),
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

                UpdateCitizen(entity);
            }

            RemoveUnusedVisuals();
        }

        private void UpdateCitizen(Entity entity)
        {
            var appearance =
                _entityManager.GetComponentData<CitizenAppearance>(entity);

            var transform =
                _entityManager.GetComponentData<LocalTransform>(entity);

            var state =
                _entityManager.GetComponentData<CitizenStateComponent>(entity);

            if (!_visuals.TryGetValue(entity, out VisualInstance visual))
            {
                visual = CreateVisualInstance(entity, appearance.VariantId);

                if (visual == null)
                    return;

                _visuals.Add(entity, visual);
            }

            UpdateTransform(visual.Transform, transform);
            UpdateAnimation(visual, state.Value);
            
            UpdateTransform(visual.Transform, transform);
            UpdateAnimation(visual, state.Value);

// Получаем прогресс изменения цвета кожи.
            float deathAmount = 0f;

            if (_entityManager.HasComponent<CitizenDeathProgress>(entity))
            {
                deathAmount = _entityManager
                    .GetComponentData<CitizenDeathProgress>(entity)
                    .Amount;
            }

// Обновляем материал только при изменении значения.
            if (!Mathf.Approximately(visual.LastDeathAmount, deathAmount))
            {
                SetSkinDeathAmount(entity, deathAmount);
                visual.LastDeathAmount = deathAmount;
            }
        }

        private VisualInstance CreateVisualInstance(
            Entity entity,
            int variantId)
        {
            if (variantId < 0 || variantId >= settings.prefabs.Count)
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

            GameObject gameObject = Instantiate(prefab, visualsRoot);

            Animator animator =
                gameObject.GetComponentInChildren<Animator>(true);

            if (animator == null)
            {
                Debug.LogError(
                    $"CitizenVisualManager: у жителя {entity} не найден Animator!",
                    gameObject
                );
            }
            else
            {
                // Individual animation speed
                animator.speed = Random.Range(0.9f, 1.0f);
            }

            var visual = new VisualInstance
            {
                GameObject = gameObject,
                Transform = gameObject.transform,
                Animator = animator,

                SkinRenderer =
                    gameObject.GetComponentInChildren<SkinnedMeshRenderer>(true)
            };

            InitializeSkinMaterials(visual);

            return visual;
        }

        // ==========================================
        // SKIN
        // ==========================================

        private void InitializeSkinMaterials(VisualInstance visual)
        {
            if (visual.SkinRenderer == null)
            {
                Debug.LogError(
                    "CitizenVisualManager: SkinnedMeshRenderer не найден!",
                    visual.GameObject
                );

                return;
            }

            Material[] materials =
                visual.SkinRenderer.sharedMaterials;

            foreach (int index in SkinMaterialSlots)
            {
                if (index < 0 || index >= materials.Length)
                {
                    Debug.LogWarning(
                        $"CitizenVisualManager: слот кожи {index} отсутствует!",
                        visual.GameObject
                    );

                    continue;
                }

                Material material = materials[index];

                if (material == null)
                    continue;

                if (!material.HasProperty(HealthySkinColorId) ||
                    !material.HasProperty(DeadSkinColorId) ||
                    !material.HasProperty(DeathAmountId))
                {
                    Debug.LogWarning(
                        $"CitizenVisualManager: материал {material.name} " +
                        $"в слоте {index} не содержит необходимых свойств SkinShader!",
                        visual.GameObject
                    );

                    continue;
                }

                var slot = new SkinMaterialSlot
                {
                    Index = index,
                    PropertyBlock = new MaterialPropertyBlock(),

                    HealthySkinColor =
                        material.GetColor(HealthySkinColorId),

                    DeadSkinColor =
                        material.GetColor(DeadSkinColorId)
                };

                // Получаем существующие свойства конкретного слота
                visual.SkinRenderer.GetPropertyBlock(
                    slot.PropertyBlock,
                    index
                );

                // Сохраняем индивидуальные цвета
                slot.PropertyBlock.SetColor(
                    HealthySkinColorId,
                    slot.HealthySkinColor
                );

                slot.PropertyBlock.SetColor(
                    DeadSkinColorId,
                    slot.DeadSkinColor
                );

                // Изначально кожа здоровая
                slot.PropertyBlock.SetFloat(
                    DeathAmountId,
                    0f
                );

                visual.SkinRenderer.SetPropertyBlock(
                    slot.PropertyBlock,
                    index
                );

                visual.SkinSlots.Add(slot);
            }

            if (visual.SkinSlots.Count == 0)
            {
                Debug.LogWarning(
                    "CitizenVisualManager: не удалось инициализировать слоты кожи!",
                    visual.GameObject
                );
            }
        }

        /// <summary>
        /// Изменяет степень изменения цвета кожи конкретного жителя.
        /// 0 = здоровая кожа.
        /// 1 = цвет мертвой кожи.
        /// </summary>
        public void SetSkinDeathAmount(Entity entity, float amount)
        {
            if (!_visuals.TryGetValue(entity, out VisualInstance visual))
                return;

            if (visual.SkinRenderer == null)
                return;

            amount = Mathf.Clamp01(amount);

            foreach (var slot in visual.SkinSlots)
            {
                slot.PropertyBlock.SetFloat(
                    DeathAmountId,
                    amount
                );

                visual.SkinRenderer.SetPropertyBlock(
                    slot.PropertyBlock,
                    slot.Index
                );
            }
        }

        /// <summary>
        /// Возвращает кожу конкретного жителя в исходное состояние.
        /// </summary>
        public void ResetSkinDeathAmount(Entity entity)
        {
            SetSkinDeathAmount(entity, 0f);
        }

        // ==========================================
        // TRANSFORM
        // ==========================================

        private void UpdateTransform(
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

        // ==========================================
        // ANIMATION
        // ==========================================

        private void UpdateAnimation(
            VisualInstance visual,
            CitizenState state)
        {
            if (visual.Animator == null)
                return;

            int animationState = GetAnimationState(state);

            if (visual.LastAnimationState == animationState)
                return;

            bool isFirstAnimation = visual.LastAnimationState == -1;

            visual.Animator.SetInteger(
                AnimStateHash,
                animationState
            );

            if (isFirstAnimation)
                PlayRandomAnimationPhase(visual.Animator, animationState);

            visual.LastAnimationState = animationState;
        }

        private void PlayRandomAnimationPhase(
            Animator animator,
            int animationState)
        {
            int stateHash = animationState switch
            {
                0 => IdleStateHash,
                1 => WalkingStateHash,
                4 => SlowRunStateHash,
                _ => 0
            };

            if (stateHash == 0)
                return;

            animator.Play(
                stateHash,
                0,
                Random.value
            );
        }

        private int GetAnimationState(CitizenState state)
        {
            return state switch
            {
                CitizenState.Idle => 0,
                CitizenState.Walking => 1,
                CitizenState.Talking => 2,
                CitizenState.Injured => 3,
                CitizenState.Panic => 4,
                CitizenState.Dead => 6,
                _ => 0
            };
        }

        // ==========================================
        // CLEANUP
        // ==========================================

        private void RemoveUnusedVisuals()
        {
            _entitiesToRemove.Clear();

            foreach (var pair in _visuals)
            {
                if (_activeEntities.Contains(pair.Key))
                    continue;

                if (pair.Value.GameObject != null)
                    Destroy(pair.Value.GameObject);

                _entitiesToRemove.Add(pair.Key);
            }

            foreach (Entity entity in _entitiesToRemove)
                _visuals.Remove(entity);
        }

        private void OnDestroy()
        {
            foreach (var visual in _visuals.Values)
            {
                if (visual.GameObject != null)
                    Destroy(visual.GameObject);
            }

            _visuals.Clear();

            _initialized = false;
        }
        
        [ContextMenu("Test Skin Death")]
        private void TestSkinDeath()
        {
            foreach (var pair in _visuals)
            {
                SetSkinDeathAmount(pair.Key, 1f);
                Debug.Log("TestSkinDeath");
                break;
            }
        }
        
        [ContextMenu("Reset Skin")]
        private void TestSkinReset()
        {
            foreach (var pair in _visuals)
            {
                ResetSkinDeathAmount(pair.Key);
                Debug.Log("TestSkinReset");
                break;
            }
        }
    }
}
