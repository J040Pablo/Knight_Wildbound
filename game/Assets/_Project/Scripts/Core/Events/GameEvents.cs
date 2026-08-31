using System;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Progression;

namespace Roguelite.Core.Events
{
    public enum CombatTextType
    {
        Normal,
        Critical,
        Heal
    }

    public static class GameEvents
    {
        // Combat Text (floating damage / heal numbers)
        public static event Action<Vector3, float, CombatTextType> OnCombatTextRequested;

        public static void TriggerCombatText(Vector3 worldPosition, float amount, CombatTextType type)
        {
            OnCombatTextRequested?.Invoke(worldPosition, amount, type);
        }

        // Economy & Items
        public static event Action<int> OnGoldChanged;
        public static event Action<ItemData, int> OnItemAdded;
        public static event Action<ItemData, int> OnItemRemoved;
        public static event Action<ItemData> OnPotionConsumed;

        // Progression & Stats
        public static event Action<int> OnLevelUp;
        public static event Action<float, float> OnPlayerHealthChanged;
        public static event Action<float, float> OnPlayerStaminaChanged;
        public static event Action<string> OnRelicCollected;

        // Combat & World
        public static event Action<string, int> OnEnemyKilled;
        public static event Action<string> OnBossDefeated;
        public static event Action<string> OnWorldEventTriggered;

        // Event Dispatchers
        public static void TriggerGoldChanged(int newGold)
        {
            OnGoldChanged?.Invoke(newGold);
        }

        public static void TriggerItemAdded(ItemData item, int qty)
        {
            OnItemAdded?.Invoke(item, qty);
        }

        public static void TriggerItemRemoved(ItemData item, int qty)
        {
            OnItemRemoved?.Invoke(item, qty);
        }

        public static void TriggerPotionConsumed(ItemData item)
        {
            OnPotionConsumed?.Invoke(item);
        }

        public static void TriggerLevelUp(int newLevel)
        {
            OnLevelUp?.Invoke(newLevel);
        }

        public static void TriggerPlayerHealthChanged(float current, float max)
        {
            OnPlayerHealthChanged?.Invoke(current, max);
        }

        public static void TriggerPlayerStaminaChanged(float current, float max)
        {
            OnPlayerStaminaChanged?.Invoke(current, max);
        }

        public static void TriggerRelicCollected(string relicId)
        {
            OnRelicCollected?.Invoke(relicId);
        }

        public static void TriggerEnemyKilled(string enemyType, int xpReward)
        {
            OnEnemyKilled?.Invoke(enemyType, xpReward);
        }

        public static void TriggerBossDefeated(string bossName)
        {
            OnBossDefeated?.Invoke(bossName);
        }

        public static void TriggerWorldEvent(string eventId)
        {
            OnWorldEventTriggered?.Invoke(eventId);
        }
    }
}
