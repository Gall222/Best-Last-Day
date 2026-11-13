using Unity.Entities;
using UnityEngine;
using Components;

namespace Authoring
{
    public class ZombieTestAuthoring : MonoBehaviour
    {
        private class Baker : Baker<ZombieTestAuthoring>
        {
            public override void Bake(ZombieTestAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<ZombieTag>(entity);
            }
        }
    }
}