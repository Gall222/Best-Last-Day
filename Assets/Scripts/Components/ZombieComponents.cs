using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct ZombieMovement : IComponentData
    {
        public float Speed;
        public float RotationSpeed;
    }

    public struct ZombieTarget : IComponentData
    {
        public Entity Target;
    }

    public struct ZombieAttack : IComponentData
    {
        public float Damage;
        public float AttackRange;
        public float AttackCooldown;
        public float CooldownRemaining;
    }

    public struct ZombieHealth : IComponentData
    {
        public float Current;
        public float Max;
    }
}