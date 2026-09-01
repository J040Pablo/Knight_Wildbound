using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Player;

namespace Roguelite.Inventory
{
    public static class ConsumableItem
    {
        private static readonly Dictionary<string, float> cooldownTimers = new Dictionary<string, float>();

        public static float GetCooldownRemaining(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0f;
            if (cooldownTimers.TryGetValue(itemId, out float readyTime))
            {
                return Mathf.Max(0f, readyTime - Time.time);
            }
            return 0f;
        }

        public static bool TryUse(ItemData item, PlayerStats stats)
        {
            if (item == null || item.category != ItemCategory.Consumable) return false;
            if (stats == null || stats.IsDead) return false;

            float cooldownRem = GetCooldownRemaining(item.itemId);
            if (cooldownRem > 0f)
            {
                // Debug.Log($"[Consumable] '{item.itemName}' is on cooldown ({cooldownRem:F1}s remaining).");
                return false;
            }

            bool effectApplied = false;

            // Apply Health Restoration
            if (item.healAmount > 0f && stats.CurrentHP < stats.MaxHP)
            {
                stats.Heal(item.healAmount);
                // Debug.Log($"[Consumable] Used '{item.itemName}': Healed {item.healAmount} HP.");
                effectApplied = true;
            }

            // Apply Stamina Restoration
            if (item.restoresStaminaFully)
            {
                stats.RegenerateStamina(stats.MaxStamina);
                // Debug.Log($"[Consumable] Used '{item.itemName}': Fully restored Stamina.");
                effectApplied = true;
            }

            // Apply Cleanse
            if (item.cleansesDebuffs)
            {
                // Debug.Log($"[Consumable] Used '{item.itemName}': Cleansed poison/debuffs.");
                effectApplied = true;
            }

            if (effectApplied)
            {
                cooldownTimers[item.itemId] = Time.time + item.useCooldown;
                return true;
            }
            else
            {
                // Debug.Log($"[Consumable] Cannot use '{item.itemName}' right now (HP/Stamina full).");
                return false;
            }
        }
    }
}
