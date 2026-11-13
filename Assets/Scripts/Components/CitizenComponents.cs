using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public enum CitizenState
    {
        Walking,
        Idle,
        Talking,
        Injured,
        Panic,
        SeekingPolice,
        Dead,
        Zombie
    }
    
    public enum AgentAction
    {
        None,
        Panic,
        Attack,
        Observe
    }
    
    public struct AgentDecision : IComponentData
    {
        public AgentAction Action;
        public Entity Target;

        // Время до следующего пересмотра решения
        public float Cooldown;

        // Оценки вариантов — пригодятся для отладки
        public float PanicScore;
        public float AttackScore;
        public float ObserveScore;
    }

    public struct Citizen : IComponentData
    {
    }

    public struct CitizenStateComponent : IComponentData
    {
        public CitizenState Value;
    }

    public struct CitizenMovement : IComponentData
    {
        public float Speed;
        public float RunSpeed;
        public float RotationSpeed;
    }

    public struct CitizenTarget : IComponentData
    {
        public float3 Position;
    }

    public struct CitizenHealth : IComponentData
    {
        public float Current;
        public float Max;
    }
    
    public struct CitizenNavigation : IComponentData
    {
        public Entity CurrentPoint;
        public float3 TargetPosition;
        public float LateralOffset;
        public Random Random;
    }
    
    public struct CitizenIdentity : IComponentData
    {
        public int Id;
    }

    public struct CitizenName : IComponentData
    {
        public FixedString64Bytes Value;
    }
    
    public struct AgentPersonality : IComponentData
    {
        // Склонность к агрессии.
        public float Aggression;

        // Смелость: сопротивление бегству.
        public float Courage;

        // Склонность к страху.
        public float Fear;

        // Любопытство.
        public float Curiosity;

        // Устойчивость к панике.
        public float PanicResistance;

        // Неуклюжесть: вероятность споткнуться.
        public float Clumsiness;
    }
    
    public struct ZombieTag : IComponentData
    {
    }

    public struct AgentPerception : IComponentData
    {
        public Entity Threat;
        public float ThreatDistance;
        public byte HasThreat;

        // Сколько зомби находится поблизости.
        public int NearbyZombieCount;

        // Видит ли житель нападение.
        public byte WitnessedAttack;

        // Видел ли житель смерть человека.
        public byte WitnessedDeath;

        // Есть ли возможность отступить.
        public byte HasEscapeRoute;
    }
    
    public struct AgentEquipment : IComponentData
    {
        public byte IsArmed;
        public float AttackDamage;
        public float AttackRange;
    }
}