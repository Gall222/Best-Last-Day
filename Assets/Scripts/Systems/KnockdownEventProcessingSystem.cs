
using Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(KnockdownDetectionSystem))]
    public partial class KnockdownEventProcessingSystem : SystemBase
    {
        private EntityQuery _configQuery;

        protected override void OnCreate()
        {
            _configQuery = GetEntityQuery(
                ComponentType.ReadOnly<KnockdownConfig>(),
                ComponentType.ReadWrite<KnockdownEvent>()
            );

            RequireForUpdate(_configQuery);
        }

        protected override void OnUpdate()
        {
            if (_configQuery.CalculateEntityCount() != 1)
                return;

            Entity configEntity = _configQuery.GetSingletonEntity();

            DynamicBuffer<KnockdownEvent> events =
                EntityManager.GetBuffer<KnockdownEvent>(configEntity);
            
            int eventCount = events.Length;
            int fallenCount = 0;
            
            for (int i = 0; i < events.Length; i++)
            {
                KnockdownEvent knockdownEvent = events[i];

                Entity target = knockdownEvent.Target;

                if (!EntityManager.Exists(target))
                    continue;

                if (!EntityManager.HasComponent<KnockdownTarget>(target))
                    continue;

                KnockdownTarget knockdownTarget =
                    EntityManager.GetComponentData<KnockdownTarget>(target);

                // Персонаж уже падает или лежит.
                if (knockdownTarget.State != BodyState.Standing)
                    continue;

                // Создаём воспроизводимый генератор случайных чисел.
                uint seed = (uint)(
                    target.Index * 73856093 ^
                    knockdownEvent.Source.Index * 19349663 ^
                    (int)(SystemAPI.Time.ElapsedTime * 1000.0)
                );

                if (seed == 0)
                    seed = 1;

                var random = Random.CreateFromIndex(seed);

                // Проверяем вероятность падения.
                if (random.NextFloat(0f, 1f) > knockdownEvent.Chance)
                    continue;

                knockdownTarget.State = BodyState.Falling;
                knockdownTarget.Direction = random.NextBool()
                    ? FallDirection.Back
                    : FallDirection.Front;

                knockdownTarget.Timer = 0f;

                EntityManager.SetComponentData(target, knockdownTarget);
                fallenCount++;
            }
            if (eventCount > 0)
            {
                UnityEngine.Debug.Log(
                    $"Knockdown: events={eventCount}, fallen={fallenCount}"
                );
            }
        }
    }
}
