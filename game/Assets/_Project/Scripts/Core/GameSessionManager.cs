using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Player;
using Roguelite.Progression;
using Roguelite.Inventory;
using Roguelite.Core.Save;

namespace Roguelite.Core
{
    public class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        [Header("Persistent Run Session Data")]
        public CharacterType SelectedCharacter = CharacterType.Knight;
        public bool HasSelectedCharacter = false;

        public float CurrentHP = 100f;
        public float MaxHP = 100f;
        public float CurrentStamina = 100f;
        public float MaxStamina = 100f;

        public int Level = 1;
        public int CurrentXP = 0;
        public int XPToNextLevel = 100;

        public float RunTimeSeconds = 0f;
        public int TotalKills = 0;

        public List<string> UnlockedUpgradeIDs = new List<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetSession()
        {
            SelectedCharacter = GameSettings.Instance != null ? GameSettings.Instance.SelectedCharacter : CharacterType.Knight;
            HasSelectedCharacter = false;

            CurrentHP = 100f;
            MaxHP = 100f;
            CurrentStamina = 100f;
            MaxStamina = 100f;

            Level = 1;
            CurrentXP = 0;
            XPToNextLevel = 100;

            RunTimeSeconds = 0f;
            TotalKills = 0;

            UnlockedUpgradeIDs.Clear();

            // Clear saved inventory, equipment & progression state for a clean run reset
            if (SaveManager.Instance != null) SaveManager.Instance.ClearSaveData();
            if (PlayerInventorySave.Instance != null) PlayerInventorySave.Instance.ClearSaveData();
            if (InventoryManager.Instance != null) InventoryManager.Instance.ResetInventory();
            if (EquipmentManager.Instance != null) EquipmentManager.Instance.ResetEquipment();
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.ResetRun();
        }

        public void SavePlayerState(PlayerStats stats)
        {
            if (stats == null) return;

            CurrentHP = stats.CurrentHP;
            MaxHP = stats.MaxHP;
            CurrentStamina = stats.CurrentStamina;
            MaxStamina = stats.MaxStamina;

            Level = stats.Level;
            CurrentXP = stats.CurrentXP;
            XPToNextLevel = stats.XPToNextLevel;
        }

        public void ApplyPlayerState(PlayerStats stats)
        {
            if (stats == null) return;

            stats.SetSessionState(CurrentHP, MaxHP, Level, CurrentXP, XPToNextLevel);

            // Re-apply upgrades if UpgradeManager exists
            UpgradeManager um = FindFirstObjectByType<UpgradeManager>();
            if (um != null && UnlockedUpgradeIDs.Count > 0)
            {
                um.ReapplySavedUpgrades(UnlockedUpgradeIDs);
            }
        }
    }
}
