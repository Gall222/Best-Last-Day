using Unity.Entities;

namespace Components
{
    public struct CitizenSpawnConfig : IComponentData
    {
        public int Count;
        public bool HasSpawned;
    }

    public struct CitizenPrefabElement : IBufferElementData
    {
        public Entity Prefab;
        public int Weight;
    }

    public struct CitizenAppearance : IComponentData
    {
        public int VariantId;
    }
}