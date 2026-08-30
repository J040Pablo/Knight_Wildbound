using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Progression;

namespace Roguelite.Loot
{
    public class LootResult
    {
        public int goldAmount;
        public List<ItemData> droppedItems = new List<ItemData>();
    }

    /// <summary>
    /// Evaluates enemy loot drop pools. Zero crafting materials — strictly Gold, Potions,
    /// Weapons, Amulets, Rings, and Campaign Relics.
    /// </summary>
    public static class LootTable
    {
        /// <summary>
        /// Hollow Tree Boss (Forest Biome Boss) Drop Evaluation:
        /// 1. 100% Campaign Relic 'relic_tree_seed' (+25 HP). Converts to +50 Gold if already owned.
        /// 2. 100% Boss Equipment (50/50 roll between Rootbreaker Axe & Bark Guardian Amulet).
        /// 3. 100% 30-50 Gold.
        /// 4. 25% Chance for 1 Epic Item.
        /// 5. 5% Chance for 1 Legendary Item.
        /// </summary>
        public static LootResult ForBoss()
        {
            LootResult result = new LootResult();

            // 1. Relic (Single-shot protection: converts to +50 Gold if already owned)
            bool alreadyHasRelic = RelicManager.Instance != null && RelicManager.Instance.HasRelic("relic_tree_seed");
            if (!alreadyHasRelic)
            {
                ItemData relic = ItemDatabase.Get("relic_tree_seed");
                if (relic != null) result.droppedItems.Add(relic);
            }
            else
            {
                result.goldAmount += 50; // Duplicate relic conversion to bonus Gold
                Debug.Log("[LootTable] Boss Relic 'relic_tree_seed' already owned. Converted to +50 Bonus Gold!");
            }

            // 2. Guaranteed Boss Equipment (50/50 random roll)
            string bossEqId = (Random.value < 0.5f) ? "weapon_rootbreaker_axe" : "amulet_bark_guardian";
            ItemData bossEq = ItemDatabase.Get(bossEqId);
            if (bossEq != null) result.droppedItems.Add(bossEq);

            // 3. Guaranteed Gold (30-50)
            result.goldAmount += Random.Range(30, 51);

            // 4. 25% Chance for Epic Item
            if (Random.value <= 0.25f)
            {
                ItemData epicItem = ItemDatabase.Get("weapon_ember_edge");
                if (epicItem != null) result.droppedItems.Add(epicItem);
            }

            // 5. 5% Chance for Legendary Item
            if (Random.value <= 0.05f)
            {
                string legId = (Random.value < 0.5f) ? "ring_of_shadows" : "weapon_dawnbringer";
                ItemData legItem = ItemDatabase.Get(legId);
                if (legItem != null) result.droppedItems.Add(legItem);
            }

            return result;
        }

        public static LootResult ForFairy()
        {
            LootResult result = new LootResult();
            result.goldAmount = Random.Range(1, 4);

            // Potions
            if (Random.value <= 0.60f)
            {
                result.droppedItems.Add(ItemDatabase.Get("small_potion"));
            }
            else if (Random.value <= 0.20f)
            {
                result.droppedItems.Add(ItemDatabase.Get("health_potion"));
            }

            // 10% Chance for Rare Ring
            if (Random.value <= 0.10f)
            {
                string ringId = (Random.value < 0.5f) ? "ring_vigor" : "ring_swiftness";
                result.droppedItems.Add(ItemDatabase.Get(ringId));
            }

            return result;
        }

        public static LootResult ForMushroom()
        {
            LootResult result = new LootResult();
            result.goldAmount = Random.Range(2, 6);

            // Potions (Antidote / Health)
            if (Random.value <= 0.50f)
            {
                result.droppedItems.Add(ItemDatabase.Get("antidote_potion"));
            }
            else if (Random.value <= 0.30f)
            {
                result.droppedItems.Add(ItemDatabase.Get("health_potion"));
            }
            else if (Random.value <= 0.15f)
            {
                result.droppedItems.Add(ItemDatabase.Get("greater_health_potion"));
            }

            // 15% Chance for Rare Amulet
            if (Random.value <= 0.15f)
            {
                string amId = (Random.value < 0.5f) ? "amulet_vitality" : "amulet_endurance";
                result.droppedItems.Add(ItemDatabase.Get(amId));
            }

            return result;
        }

        public static LootResult ForStoneGiant()
        {
            LootResult result = new LootResult();
            result.goldAmount = Random.Range(10, 25);

            // 50% Chance for Rare Ring or Amulet
            if (Random.value <= 0.50f)
            {
                string eqId = (Random.value < 0.5f) ? "amulet_strength" : "ring_vigor";
                result.droppedItems.Add(ItemDatabase.Get(eqId));
            }

            return result;
        }

        /// <summary>
        /// Ancient Colossus Optional Mini-Boss Drops:
        /// - 100% Gold (40-70)
        /// - 100% Unique Equipment (50% Ancient Colossus Ring / 50% Colossus Stone Amulet)
        /// - 50% Chance for Bonus Weapon
        /// </summary>
        public static LootResult ForColossusMiniBoss()
        {
            LootResult result = new LootResult();
            result.goldAmount = Random.Range(40, 71);

            // 100% Unique Colossus Equipment Drop (50/50 roll)
            string colossusEqId = (Random.value < 0.5f) ? "ring_ancient_colossus" : "amulet_colossus_stone";
            ItemData eq = ItemDatabase.Get(colossusEqId);
            if (eq != null) result.droppedItems.Add(eq);

            // 50% Chance for Rare/Epic Weapon
            if (Random.value <= 0.50f)
            {
                string weaponId = (Random.value < 0.5f) ? "weapon_guardian_blade" : "weapon_ember_edge";
                ItemData w = ItemDatabase.Get(weaponId);
                if (w != null) result.droppedItems.Add(w);
            }

            return result;
        }

        /// <summary>
        /// Default Enemy Loot Distribution:
        /// - 66% Gold
        /// - 20% Consumables (Potions)
        /// - 10% Accessories (Rings / Amulets)
        /// - 2% Weapons (Rare drop)
        /// - 2% Empty (No item drop)
        /// TODO FUTURE: Add Crafting Materials in Biome 2 / Blacksmith update.
        /// </summary>
        public static LootResult Default()
        {
            LootResult result = new LootResult();

            float roll = Random.value;
            if (roll <= 0.66f)
            {
                // 66% Gold
                result.goldAmount = Random.Range(2, 7);
            }
            else if (roll <= 0.86f)
            {
                // 20% Consumables
                float pRoll = Random.value;
                if (pRoll <= 0.60f) result.droppedItems.Add(ItemDatabase.Get("small_potion"));
                else if (pRoll <= 0.90f) result.droppedItems.Add(ItemDatabase.Get("health_potion"));
                else result.droppedItems.Add(ItemDatabase.Get("antidote_potion"));
            }
            else if (roll <= 0.96f)
            {
                // 10% Rings / Amulets
                string accId = (Random.value < 0.5f) ? "ring_vigor" : "amulet_vitality";
                result.droppedItems.Add(ItemDatabase.Get(accId));
            }
            else if (roll <= 0.98f)
            {
                // 2% Weapons
                string weaponId = (Random.value < 0.5f) ? "weapon_rusty_blade" : "weapon_hunter_sword";
                result.droppedItems.Add(ItemDatabase.Get(weaponId));
            }
            else
            {
                // 2% Empty (No drop)
            }

            return result;
        }
    }
}
