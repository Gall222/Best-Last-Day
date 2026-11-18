using Unity.Entities;

namespace Components
{
    // Состояние тела персонажа независимо от его игрового состояния.
    public enum BodyState : byte
    {
        Standing,
        Falling,
        Lying,
        GettingUp
    }

    public enum FallDirection : byte
    {
        Back,
        Front
    }

    // Компонент объектов, которые могут сбивать других с ног.
    public struct KnockdownSource : IComponentData
    {
        // Множитель вероятности при столкновении.
        public float CollisionModifier;

        // Множитель вероятности при атаке.
        public float AttackModifier;

        // Радиус объекта.
        public float Radius;

        public byte Enabled;
    }

    // Компонент объектов, которые могут упасть.
    public struct KnockdownTarget : IComponentData
    {
        // Насколько легко объект сбить с ног.
        // 1 = обычная восприимчивость.
        public float Susceptibility;

        // Радиус объекта для обнаружения столкновений.
        public float Radius;

        // Текущее состояние тела.
        public BodyState State;

        // Направление падения.
        public FallDirection Direction;

        // Таймер текущего состояния.
        public float Timer;
        
        public float CollisionCooldown;
    }

    // Событие попытки сбить объект с ног.
    public struct KnockdownEvent : IBufferElementData
    {
        public Entity Source;
        public Entity Target;
        public float Chance;
    }
    
    public struct KnockdownConfig : IComponentData
    {
        // Вероятность падения при обычном столкновении жителей.
        public float CitizenCollisionChance;

        // Вероятность падения при столкновении с жителем в панике.
        public float PanicCollisionChance;

        // Вероятность падения при столкновении с зомби.
        public float ZombieCollisionChance;

        // Вероятность падения при атаке зомби.
        public float ZombieAttackChance;

        // Базовая восприимчивость к падению.
        public float CitizenSusceptibility;
        public float ZombieSusceptibility;

        // Радиусы персонажей для обнаружения контакта.
        public float CitizenRadius;
        public float ZombieRadius;
    }
}