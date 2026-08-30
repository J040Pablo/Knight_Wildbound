using System;
using UnityEngine;

namespace Roguelite.Data
{
    [Serializable]
    public class BiomeRuntimeData
    {
        public BiomeDefinition Definition { get; private set; }
        public int EnemiesDefeated { get; set; }
        public bool IsBossSpawned { get; set; }
        public bool IsBossDefeated { get; set; }
        public bool IsRelicClaimed { get; set; }

        public BiomeRuntimeData(BiomeDefinition def)
        {
            Definition = def;
            EnemiesDefeated = 0;
            IsBossSpawned = false;
            IsBossDefeated = false;
            IsRelicClaimed = false;
        }
    }
}
