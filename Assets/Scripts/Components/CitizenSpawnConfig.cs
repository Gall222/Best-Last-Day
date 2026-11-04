using Unity.Entities;

namespace Components
{
    public struct CitizenSpawnConfig : IComponentData
    {
        public int Count;
        public bool HasSpawned;

        public float Speed;
        public float RunSpeed;
        public float RotationSpeed;
        public float MaxHealth;
    }

    public struct CitizenVisualVariantElement : IBufferElementData
    {
        public int VariantId;
        public int Weight;
    }

    public struct CitizenAppearance : IComponentData
    {
        public int VariantId;
    }
}