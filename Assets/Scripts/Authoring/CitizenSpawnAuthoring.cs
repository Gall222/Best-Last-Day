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
                    Debug.LogError("CitizenSpawnSettings not assigned!");
                    return;
                }

                if (authoring.settings.prefabs.Count == 0)
                {
                    Debug.LogError("No citizen prefabs assigned!");
                    return;
                }

                DependsOn(authoring.settings);

                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new CitizenSpawnConfig
                {
                    Count = Mathf.Max(0, authoring.settings.count),
                    HasSpawned = false
                });

                var buffer = AddBuffer<CitizenPrefabElement>(entity);

                foreach (var entry in authoring.settings.prefabs)
                {
                    if (entry.prefab == null)
                        continue;

                    DependsOn(entry.prefab);

                    Entity prefab = GetEntity(
                        entry.prefab,
                        TransformUsageFlags.Dynamic
                    );

                    buffer.Add(new CitizenPrefabElement
                    {
                        Prefab = prefab,
                        Weight = Mathf.Max(1, entry.weight)
                    });
                }
            }
        }
    }
}