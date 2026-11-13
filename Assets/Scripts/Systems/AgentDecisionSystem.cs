using Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Systems
{
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AgentPerceptionSystem))]
public partial struct AgentDecisionSystem : ISystem
{
    private const float DetectionRadius = 12f;
    private const float AttackDistance = 3f;
    private const float DecisionCooldown = 1.5f;

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (decision, perception, personality) in
                 SystemAPI.Query<
                     RefRW<AgentDecision>,
                     RefRO<AgentPerception>,
                     RefRO<AgentPersonality>>())
        {
            ref var currentDecision = ref decision.ValueRW;

            // Если угрозы больше нет — сразу забываем о ней.
            if (perception.ValueRO.HasThreat == 0)
            {
                currentDecision.Action = AgentAction.None;
                currentDecision.Target = Entity.Null;
                currentDecision.Cooldown = 0f;

                currentDecision.PanicScore = 0f;
                currentDecision.AttackScore = 0f;
                currentDecision.ObserveScore = 0f;

                continue;
            }

            // Пока решение действует, не пересматриваем его.
            currentDecision.Cooldown -= deltaTime;

            if (currentDecision.Cooldown > 0f)
                continue;

            var p = personality.ValueRO;
            float distance = perception.ValueRO.ThreatDistance;

            // Чем ближе зомби, тем выше опасность.
            float proximity = math.saturate(
                1f - distance / DetectionRadius
            );

            // Паника — базовая реакция большинства жителей.
            float panicScore =
                0.35f +
                p.Fear * 0.30f +
                (1f - p.Courage) * 0.20f +
                (1f - p.PanicResistance) * 0.20f +
                proximity * 0.25f;

            // Атаковать будут преимущественно смелые и агрессивные.
            float attackScore =
                p.Aggression * 0.45f +
                p.Courage * 0.35f +
                (1f - p.Fear) * 0.20f;

            float attackDistanceFactor = math.saturate(
                1f - math.max(0f, distance - AttackDistance) / 6f
            );

            attackScore *= attackDistanceFactor;

            // Наблюдение — для любопытных и устойчивых к панике.
            float observeScore =
                p.Curiosity * 0.25f +
                p.PanicResistance * 0.20f +
                p.Courage * 0.15f;

            // Выбираем действие с наибольшей оценкой.
            AgentAction selectedAction = AgentAction.Observe;

            float bestScore = observeScore;

            if (panicScore > bestScore)
            {
                bestScore = panicScore;
                selectedAction = AgentAction.Panic;
            }

            if (attackScore > bestScore)
            {
                bestScore = attackScore;
                selectedAction = AgentAction.Attack;
            }

            // Сохраняем результат.
            currentDecision.Action = selectedAction;
            currentDecision.Target = perception.ValueRO.Threat;
            currentDecision.Cooldown = DecisionCooldown;

            currentDecision.PanicScore = panicScore;
            currentDecision.AttackScore = attackScore;
            currentDecision.ObserveScore = observeScore;
        }
    }
}
}