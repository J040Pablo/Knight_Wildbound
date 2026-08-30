using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;

namespace Roguelite.Loot
{
    public enum ChestRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public static class ChestLootTable
    {
        /// <summary>
        /// Rolls procedural chest rarity according to game design specifications:
        /// 70% Common / 20% Rare / 8% Epic / 2% Legendary.
        /// </summary>
        public static ChestRarity RollChestRarity()
        {
            float roll = Random.value;
            if (roll <= 0.02f) return ChestRarity.Legendary; // 2%
            if (roll <= 0.10f) return ChestRarity.Epic;      // 8%
            if (roll <= 0.30f) return ChestRarity.Rare;      // 20%
            return ChestRarity.Common;                       // 70%
        }

        public static LootResult GenerateRewards(ChestRarity rarity)
        {
            LootResult result = new LootResult();

            switch (rarity)
            {
                case ChestRarity.Legendary:
                    result.goldAmount = Random.Range(40, 75);
                    string legId = (Random.value < 0.5f) ? "ring_of_shadows" : "weapon_dawnbringer";
                    result.droppedItems.Add(ItemDatabase.Get(legId));
                    result.droppedItems.Add(ItemDatabase.Get("greater_health_potion"));
                    break;

                case ChestRarity.Epic:
                    result.goldAmount = Random.Range(25, 45);
                    string epId = (Random.value < 0.5f) ? "weapon_ember_edge" : "amulet_vitality";
                    result.droppedItems.Add(ItemDatabase.Get(epId));
                    result.droppedItems.Add(ItemDatabase.Get("health_potion"));
                    break;

                case ChestRarity.Rare:
                    result.goldAmount = Random.Range(15, 30);
                    string rareId = (Random.value < 0.33f) ? "weapon_hunter_sword" : ((Random.value < 0.66f) ? "amulet_strength" : "ring_swiftness");
                    result.droppedItems.Add(ItemDatabase.Get(rareId));
                    result.droppedItems.Add(ItemDatabase.Get("health_potion"));
                    break;

                case ChestRarity.Common:
                default:
                    result.goldAmount = Random.Range(8, 18);
                    string comId = (Random.value < 0.5f) ? "weapon_rusty_blade" : "ring_vigor";
                    result.droppedItems.Add(ItemDatabase.Get(comId));
                    result.droppedItems.Add(ItemDatabase.Get("small_potion"));
                    break;
            }

            return result;
        }
    }
}
