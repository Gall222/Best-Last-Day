using Components;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Authoring
{
    public class CitizenAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 1.5f;
        public float rotationSpeed = 5f;

        [Header("Health")]
        public float maxHealth = 100f;

        [Header("Navigation")]
        public WalkPointAuthoring startPoint;

        class Baker : Baker<CitizenAuthoring>
        {
            public override void Bake(CitizenAuthoring authoring)
            {
                Entity entity = GetEntity(
                    TransformUsageFlags.Dynamic
                );

                AddComponent<Citizen>(entity);

                AddComponent(entity, new CitizenStateComponent
                {
                    Value = authoring.startPoint != null
                        ? CitizenState.Walking
                        : CitizenState.Idle
                });

                AddComponent(entity, new CitizenMovement
                {
                    Speed = authoring.speed,
                    RotationSpeed = authoring.rotationSpeed,
                    RunSpeed = authoring.speed * 2f
                });

                AddComponent(entity, new CitizenHealth
                {
                    Current = authoring.maxHealth,
                    Max = authoring.maxHealth
                });

                // Маршрут добавляем только при наличии стартовой точки.
                if (authoring.startPoint != null)
                {
                    Entity startPoint = GetEntity(
                        authoring.startPoint,
                        TransformUsageFlags.None
                    );

                    AddComponent(entity, new CitizenNavigation
                    {
                        CurrentPoint = startPoint,
                        TargetPosition =
                            authoring.startPoint.transform.position,

                        Random = Random.CreateFromIndex(
                            (uint)(entity.Index + 1)
                        )
                    });
                }
            }
        }
    }
}