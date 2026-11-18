using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class ZombieVirusAuthoring : MonoBehaviour
    {
        [Header("Death")]
        [SerializeField, Min(0.1f)]
        private float skinChangeDuration = 3f;

        [SerializeField, Min(0f)]
        private float reviveDelay = 5f;

        [Header("Zombie")]
        [SerializeField, Min(1f)]
        private float zombieMaxHealth = 150f;

        [SerializeField, Min(0f)]
        private float zombieDamage = 20f;

        private class Baker : Baker<ZombieVirusAuthoring>
        {
            public override void Bake(ZombieVirusAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new ZombieVirusConfig
                {
                    SkinChangeDuration = authoring.skinChangeDuration,
                    ReviveDelay = authoring.reviveDelay,
                    ZombieMaxHealth = authoring.zombieMaxHealth,
                    ZombieDamage = authoring.zombieDamage
                });
            }
        }
    }
}