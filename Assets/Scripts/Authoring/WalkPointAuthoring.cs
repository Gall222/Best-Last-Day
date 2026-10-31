using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class WalkPointAuthoring : MonoBehaviour
    {
        [Tooltip("Точки, в которые NPC может перейти отсюда.")]
        public WalkPointAuthoring[] neighbours;

        class Baker : Baker<WalkPointAuthoring>
        {
            public override void Bake(WalkPointAuthoring authoring)
            {
                Entity entity = GetEntity(
                    TransformUsageFlags.None
                );

                AddComponent(entity, new WalkPoint
                {
                    Position = authoring.transform.position
                });

                var buffer = AddBuffer<WalkPointNeighbour>(entity);

                foreach (var neighbour in authoring.neighbours)
                {
                    if (neighbour == null)
                        continue;

                    buffer.Add(new WalkPointNeighbour
                    {
                        Entity = GetEntity(
                            neighbour,
                            TransformUsageFlags.None
                        )
                    });
                }
            }
        }
    }
}