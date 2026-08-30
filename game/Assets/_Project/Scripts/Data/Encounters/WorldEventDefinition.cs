using System.Collections.Generic;
using UnityEngine;
using Roguelite.Loot;

namespace Roguelite.Data
{
    [CreateAssetMenu(fileName = "NewWorldEventDefinition", menuName = "Roguelite/Data/World Event Definition")]
    public class WorldEventDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string eventId = "event_goblin_scavengers";
        public string eventName = "Goblin Scavengers";
        [TextArea(2, 4)]
        public string description = "A band of goblin scavengers guarding stolen loot.";

        [Header("Prefabs & Setup")]
        public GameObject eventPrefab;
        public List<EnemyDefinition> enemiesToSpawn = new List<EnemyDefinition>();
        public ChestRarity rewardRarity = ChestRarity.Rare;
    }
}
