using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public enum CitizenState
    {
        Walking,
        Idle,
        Talking,
        Injured,
        Panic,
        SeekingPolice,
        Dead,
        Zombie
    }

    public struct Citizen : IComponentData
    {
    }

    public struct CitizenStateComponent : IComponentData
    {
        public CitizenState Value;
    }

    public struct CitizenMovement : IComponentData
    {
        public float Speed;
        public float RunSpeed;
        public float RotationSpeed;
    }

    public struct CitizenTarget : IComponentData
    {
        public float3 Position;
    }

    public struct CitizenHealth : IComponentData
    {
        public float Current;
        public float Max;
    }
    
    public struct CitizenNavigation : IComponentData
    {
        public Entity CurrentPoint;
        public float3 TargetPosition;
        public Random Random;
    }
    
    public struct CitizenIdentity : IComponentData
    {
        public int Id;
    }

    public struct CitizenName : IComponentData
    {
        public FixedString64Bytes Value;
    }
}