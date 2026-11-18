using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(CitizenDeathProgressSystem))]
    public partial struct CitizenDeathStartSystem : ISystem
    {
        private EntityQuery _query;
        private EntityQuery _configQuery;

        public void OnCreate(ref SystemState state)
        {
            _query = state.GetEntityQuery(
                ComponentType.ReadOnly<CitizenHealth>(),
                ComponentType.ReadWrite<CitizenStateComponent>(),
                ComponentType.Exclude<CitizenDeathProgress>()
            );
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ZombieVirusConfig>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_configQuery.CalculateEntityCount() != 1)
                return;

            var config = _configQuery.GetSingleton<ZombieVirusConfig>();

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
                        Duration = math.max(0.1f, config.SkinChangeDuration),
                        Amount = 0f
                    }
                );
            }
        }
    }
}