using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Items
{
    public enum ItemCategory
    {
        Weapon,
        Amulet,
        Ring,
        Belt,
        Consumable,
        QuestItem,
        Relic
    }

    public enum ItemRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Relic
    }

    public static class ItemRarityExtensions
    {
        public static Color GetColor(this ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:      return new Color(0.25f, 0.55f, 0.95f); // Blue
                case ItemRarity.Epic:      return new Color(0.64f, 0.27f, 0.90f); // Purple
                case ItemRarity.Legendary: return new Color(0.95f, 0.56f, 0.10f); // Orange
                case ItemRarity.Relic:     return new Color(0.20f, 0.85f, 0.40f); // Emerald Green
                case ItemRarity.Common:
                default:                   return new Color(0.62f, 0.62f, 0.62f); // Gray
            }
        }
    }

    /// <summary>
    /// Generic runtime item definition. Icons are represented with a short text/emoji glyph
    /// drawn directly by the Inventory UI, or an optional procedural Sprite.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Roguelite/Data/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId = "";
        public string itemName = "Unknown Item";
        [TextArea] public string description = "";
        public string iconGlyph = "❔";

        [Header("Visuals (optional — falls back to procedural glyph icon if null)")]
        public Sprite customIcon;

        [Header("Classification")]
        public ItemCategory category = ItemCategory.Consumable;
        public ItemRarity rarity = ItemRarity.Common;
        public bool isRelic = false;

        [Header("Stacking & Value")]
        public bool isStackable = true;
        public int maxStack = 99;
        public int goldValue = 5;

        [Header("Equipment Passive Bonuses (Weapon, Amulet, Belt, Ring)")]
        public float flatDamageBonus = 0f;
        public float flatHpBonus = 0f;
        public float flatStaminaBonus = 0f;
        public float moveSpeedBonusPercent = 0f;

        [Header("Consumable Effect")]
        public float healAmount = 0f;
        public bool restoresStaminaFully = false;
        public bool cleansesDebuffs = false;
        public float useCooldown = 10f;

        public Color RarityColor => rarity.GetColor();

        /// <summary>
        /// Human-readable one-line summary of this item's passive/consumable bonuses, used by tooltips.
        /// </summary>
        public string GetBonusSummary()
        {
            List<string> parts = new List<string>();
            if (flatDamageBonus > 0f) parts.Add($"+{flatDamageBonus:F0} Dano");
            if (flatHpBonus > 0f) parts.Add($"+{flatHpBonus:F0} HP");
            if (flatStaminaBonus > 0f) parts.Add($"+{flatStaminaBonus:F0} Stamina");
            if (moveSpeedBonusPercent > 0f) parts.Add($"+{moveSpeedBonusPercent * 100f:F0}% Vel.");
            if (healAmount > 0f) parts.Add($"Cura {healAmount:F0}");
            if (restoresStaminaFully) parts.Add("Stamina Total");
            if (cleansesDebuffs) parts.Add("Remove Debuffs");
            return parts.Count > 0 ? string.Join("  ", parts) : "";
        }

        public static ItemData Create(string id, string name, string desc, ItemCategory cat, ItemRarity rar,
            string glyph, int gold = 5, bool stackable = true, int maxStack = 99)
        {
            ItemData data = CreateInstance<ItemData>();
            data.itemId = id;
            data.itemName = name;
            data.description = desc;
            data.category = cat;
            data.rarity = rar;
            data.iconGlyph = glyph;
            data.goldValue = gold;
            data.isStackable = stackable;
            data.maxStack = maxStack;
            data.isRelic = (cat == ItemCategory.Relic || rar == ItemRarity.Relic);
            return data;
        }
    }
}
