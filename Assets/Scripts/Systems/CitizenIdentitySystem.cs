using Components;
using Unity.Collections;
using Unity.Entities;

namespace Systems
{
    public partial struct CitizenIdentitySystem : ISystem
    {
        private EntityQuery _unassignedCitizens;

        public void OnCreate(ref SystemState state)
        {
            // Создаём счётчик ID.
            Entity counterEntity = state.EntityManager.CreateEntity();

            state.EntityManager.AddComponentData(
                counterEntity,
                new CitizenIdCounter
                {
                    NextId = 1
                }
            );

            // Находим жителей, которым ещё не назначен ID.
            _unassignedCitizens = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Citizen>()
                .WithNone<CitizenIdentity>()
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_unassignedCitizens.IsEmpty)
                return;

            Entity counterEntity =
                SystemAPI.GetSingletonEntity<CitizenIdCounter>();

            var counter = state.EntityManager
                .GetComponentData<CitizenIdCounter>(counterEntity);

            using var entities =
                _unassignedCitizens.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in entities)
            {
                state.EntityManager.AddComponentData(
                    entity,
                    new CitizenIdentity
                    {
                        Id = counter.NextId
                    }
                );

                counter.NextId++;

                UnityEngine.Debug.Log(
                    $"Создан житель ID: {counter.NextId - 1}"
                );
            }

            state.EntityManager.SetComponentData(
                counterEntity,
                counter
            );
        }
    }
}