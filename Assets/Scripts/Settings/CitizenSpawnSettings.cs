using System.Collections.Generic;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(
        fileName = "CitizenVisualVariant",
        menuName = "Game/Settings/Citizen Visual Variant"
    )]
    public class CitizenVisualVariant : ScriptableObject
    {
        public GameObject prefab;

        [Min(1)]
        public int weight = 1;
    }

    [CreateAssetMenu(
        fileName = "CitizenSpawnSettings",
        menuName = "Game/Settings/Spawn Settings"
    )]
    public class CitizenSpawnSettings : ScriptableObject
    {
        [Header("Spawn")]
        [Min(0)]
        public int count = 100;

        [Header("Movement")]
        public float speed = 1.5f;
        public float runSpeed = 3f;
        public float rotationSpeed = 5f;

        [Header("Health")]
        public float maxHealth = 100f;

        [Header("Visual Variants")]
        public List<CitizenVisualVariant> prefabs = new();
    }
}