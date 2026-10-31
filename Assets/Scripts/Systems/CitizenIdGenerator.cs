using Unity.Entities;

namespace Systems
{
    public struct CitizenIdCounter : IComponentData
    {
        public int NextId;
    }
}