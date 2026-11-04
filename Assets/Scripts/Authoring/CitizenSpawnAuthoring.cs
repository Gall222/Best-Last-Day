using Components;
using Settings;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class CitizenSpawnAuthoring : MonoBehaviour
    {
        public CitizenSpawnSettings settings;

        class Baker : Baker<CitizenSpawnAuthoring>
        {
            public override void Bake(CitizenSpawnAuthoring authoring)
            {
                if (authoring.settings == null)
                {
                    Debug.LogError(
                        "CitizenSpawnSettings not assigned!",
                        authoring
                    );
                    return;
                }

                if (authoring.settings.prefabs.Count == 0)
                {
                    Debug.LogError(
                        "No citizen visual variants assigned!",
                        authoring
                    );
                    return;
                }

                DependsOn(authoring.settings);

                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new CitizenSpawnConfig
                {
                    Count = Mathf.Max(0, authoring.settings.count),
                    HasSpawned = false,

                    Speed = authoring.settings.speed,
                    RunSpeed = authoring.settings.runSpeed,
                    RotationSpeed = authoring.settings.rotationSpeed,
                    MaxHealth = authoring.settings.maxHealth
                });

                var buffer =
                    AddBuffer<CitizenVisualVariantElement>(entity);

                for (int i = 0; i < authoring.settings.prefabs.Count; i++)
                {
                    CitizenVisualVariant variant =
                        authoring.settings.prefabs[i];

                    if (variant == null || variant.prefab == null)
                    {
                        Debug.LogWarning(
                            $"CitizenSpawnAuthoring: visual variant {i} is missing!",
                            authoring
                        );
                        continue;
                    }

                    buffer.Add(new CitizenVisualVariantElement
                    {
                        VariantId = i,
                        Weight = Mathf.Max(1, variant.weight)
                    });
                }
            }
        }
    }
}