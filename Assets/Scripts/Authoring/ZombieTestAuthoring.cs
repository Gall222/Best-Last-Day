using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class ZombieTestAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 1.8f;
        public float rotationSpeed = 7f;

        [Header("Attack")]
        public float damage = 25f;
        public float attackRange = 1.8f;
        public float attackCooldown = 1.5f;

        [Header("Health")]
        public float maxHealth = 100f;

        private class Baker : Baker<ZombieTestAuthoring>
        {
            public override void Bake(ZombieTestAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<ZombieTag>(entity);

                AddComponent(entity, new ZombieMovement
                {
                    Speed = authoring.speed,
                    RotationSpeed = authoring.rotationSpeed
                });

                AddComponent(entity, new ZombieTarget
                {
                    Target = Entity.Null
                });

                AddComponent(entity, new ZombieAttack
                {
                    Damage = authoring.damage,
                    AttackRange = authoring.attackRange,
                    AttackCooldown = authoring.attackCooldown,
                    CooldownRemaining = 0f
                });

                AddComponent(entity, new ZombieHealth
                {
                    Current = authoring.maxHealth,
                    Max = authoring.maxHealth
                });
            }
        }
    }
}