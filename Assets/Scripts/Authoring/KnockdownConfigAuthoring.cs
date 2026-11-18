using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class KnockdownConfigAuthoring : MonoBehaviour
    {
        [Header("Collision Chances")]

        [Range(0f, 1f)]
        public float citizenCollisionChance = 0.05f;

        [Range(0f, 1f)]
        public float panicCollisionChance = 0.2f;

        [Range(0f, 1f)]
        public float zombieCollisionChance = 0.3f;

        [Range(0f, 1f)]
        public float zombieAttackChance = 0.6f;

        [Header("Susceptibility")]

        [Min(0f)]
        public float citizenSusceptibility = 1f;

        [Min(0f)]
        public float zombieSusceptibility = 1f;

        [Header("Collision Radius")]

        [Min(0.01f)]
        public float citizenRadius = 0.45f;

        [Min(0.01f)]
        public float zombieRadius = 0.5f;

        public class Baker : Baker<KnockdownConfigAuthoring>
        {
            public override void Bake(KnockdownConfigAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new KnockdownConfig
                {
                    CitizenCollisionChance = authoring.citizenCollisionChance,
                    PanicCollisionChance = authoring.panicCollisionChance,
                    ZombieCollisionChance = authoring.zombieCollisionChance,
                    ZombieAttackChance = authoring.zombieAttackChance,

                    CitizenSusceptibility = authoring.citizenSusceptibility,
                    ZombieSusceptibility = authoring.zombieSusceptibility,

                    CitizenRadius = authoring.citizenRadius,
                    ZombieRadius = authoring.zombieRadius
                });
                
                AddBuffer<KnockdownEvent>(entity);
            }
        }
    }
}