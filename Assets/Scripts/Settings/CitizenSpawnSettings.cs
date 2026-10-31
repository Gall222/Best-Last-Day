using System;
using System.Collections.Generic;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(
        fileName = "CitizenSpawnSettings",
        menuName = "Game/Settings/Spawn Settings"
    )]
    
    [Serializable]
    public class CitizenPrefabEntry : ScriptableObject
    {
        public GameObject prefab;

        [Min(1)]
        public int weight = 1;
    }
    
    public class CitizenSpawnSettings : ScriptableObject
    {
        [Min(0)]
        public int count = 100;

        public List<CitizenPrefabEntry> prefabs = new();
    }
}