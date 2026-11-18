using Unity.Entities;

namespace Components
{
    public struct ZombieVirusConfig : IComponentData
    {
        // Время изменения цвета кожи.
        public float SkinChangeDuration;

        // Задержка перед превращением в зомби.
        public float ReviveDelay;

        // Максимальное здоровье зомби.
        public float ZombieMaxHealth;

        // Урон зомби.
        public float ZombieDamage;
    }
}