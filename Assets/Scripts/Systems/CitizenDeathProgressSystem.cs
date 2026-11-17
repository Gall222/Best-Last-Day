using Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CitizenDeathProgressSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var progress in
                     SystemAPI.Query<RefRW<CitizenDeathProgress>>())
            {
                ref var death = ref progress.ValueRW;

                if (death.Amount >= 1f)
                    continue;

                if (death.Duration <= 0f)
                {
                    death.Amount = 1f;
                    continue;
                }

                death.ElapsedTime = math.min(
                    death.ElapsedTime + deltaTime,
                    death.Duration
                );

                death.Amount = math.saturate(
                    death.ElapsedTime / death.Duration
                );
            }
        }
    }
}