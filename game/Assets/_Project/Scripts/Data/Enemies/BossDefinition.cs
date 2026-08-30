using UnityEngine;
using Roguelite.Loot;

namespace Roguelite.Data
{
    [CreateAssetMenu(fileName = "NewBossDefinition", menuName = "Roguelite/Data/Boss Definition")]
    public class BossDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string bossName = "The Hollow Tree";

        [Header("Stats")]
        public float maxHealth = 350f;
        public float damage = 25f;
        public int phases = 2;

        [Header("Environment & Music")]
        public GameObject arenaPrefab;
        public GameObject bossPrefab;
        public AudioClip bossMusic;

        [Header("Rewards & Loot")]
        public ChestRarity dropRarity = ChestRarity.Epic;
        public RelicDefinition rewardRelic;
    }
}
