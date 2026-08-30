using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Data
{
    public enum BiomeType
    {
        Forest,
        Desert,
        Snow,
        Swamp,
        Volcano,
        Ruins
    }

    [CreateAssetMenu(fileName = "NewBiomeDefinition", menuName = "Roguelite/Data/Biome Definition")]
    public class BiomeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string biomeName = "Forest Biome";
        public BiomeType biomeType = BiomeType.Forest;

        [Header("Progression Parameters")]
        public float biomeLength = 500f;
        public int minLevel = 1;
        public int maxLevel = 10;
        public int recommendedPower = 10;

        [Header("Visuals & Audio")]
        public GameObject terrainPrefab;
        public AudioClip backgroundMusic;
        public Color fogColor = new Color(0.2f, 0.35f, 0.25f);
        public Color ambientColor = new Color(0.4f, 0.5f, 0.4f);

        [Header("Pools & Boss")]
        public List<EnemyDefinition> enemyPool = new List<EnemyDefinition>();
        public BossDefinition boss;
        public RelicDefinition relic;
        public List<WorldEventDefinition> events = new List<WorldEventDefinition>();
    }
}
