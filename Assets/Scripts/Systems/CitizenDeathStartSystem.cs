using Components;
using Unity.Collections;
using Unity.Entities;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(CitizenDeathProgressSystem))]
    public partial struct CitizenDeathStartSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = state.GetEntityQuery(
                ComponentType.ReadOnly<CitizenHealth>(),
                ComponentType.ReadWrite<CitizenStateComponent>(),
                ComponentType.Exclude<CitizenDeathProgress>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;

            using var entities =
                _query.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in entities)
            {
                var health =
                    entityManager.GetComponentData<CitizenHealth>(entity);

                if (health.Current > 0f)
                    continue;

                var citizenState =
                    entityManager.GetComponentData<CitizenStateComponent>(entity);

                citizenState.Value = CitizenState.Dead;

                entityManager.SetComponentData(
                    entity,
                    citizenState
                );

                entityManager.AddComponentData(
                    entity,
                    new CitizenDeathProgress
                    {
                        ElapsedTime = 0f,
                        Duration = 3f,
                        Amount = 0f
                    }
                );
            }
        }
    }
}