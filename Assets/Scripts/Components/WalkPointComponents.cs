using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct WalkPoint : IComponentData
    {
        public float3 Position;
    }

    public struct WalkPointNeighbour : IBufferElementData
    {
        public Entity Entity;
    }
}