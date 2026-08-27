using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Data;
using Roguelite.Player;

namespace Roguelite.Progression
{
    public class UpgradeManager : MonoBehaviour
    {
        [Header("Available Upgrades List")]
        [SerializeField] private List<UpgradeData> upgradePool = new List<UpgradeData>();

        private PlayerStats playerStats;

        public event Action<List<UpgradeData>> OnLevelUpUpgradeChoices;

        private void Start()
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.OnLevelUp += HandlePlayerLevelUp;
            }

            InitializeDefaultPoolIfEmpty();
        }

        private void OnDestroy()
        {
            if (playerStats != null)
            {
                playerStats.OnLevelUp -= HandlePlayerLevelUp;
            }
        }

        private void InitializeDefaultPoolIfEmpty()
        {
            if (upgradePool.Count == 0)
            {
                // Universal Upgrades
                upgradePool.Add(CreateUpgrade("+20% Damage", "Increases attack damage by 20%", UpgradeType.AttackDamagePercent, 0.20f, UpgradeCategory.Universal));
                upgradePool.Add(CreateUpgrade("+15% Movement Speed", "Increases movement speed by 15%", UpgradeType.MoveSpeedPercent, 0.15f, UpgradeCategory.Universal));
                upgradePool.Add(CreateUpgrade("+20 Max Health", "Adds +20 to maximum health and heals", UpgradeType.MaxHealthFlat, 20f, UpgradeCategory.Universal));
                upgradePool.Add(CreateUpgrade("+15% Max Stamina", "Increases maximum stamina pool by 15%", UpgradeType.MaxStaminaPercent, 0.15f, UpgradeCategory.Universal));
                upgradePool.Add(CreateUpgrade("+10% Critical Chance", "Increases chance to deal double damage by 10%", UpgradeType.CritChancePercent, 0.10f, UpgradeCategory.Universal));

                // Knight Specific
                upgradePool.Add(CreateUpgrade("+15% Attack Speed", "Heavy melee slashes are 15% faster", UpgradeType.AttackSpeedPercent, 0.15f, UpgradeCategory.Knight));

                // Mage Specific
                upgradePool.Add(CreateUpgrade("+20% Magic Damage", "Increases Magic Bolt & Fireball damage by 20%", UpgradeType.MagicDamagePercent, 0.20f, UpgradeCategory.Mage));
                upgradePool.Add(CreateUpgrade("+20% Projectile Speed", "Magic projectiles travel 20% faster", UpgradeType.ProjectileSpeedPercent, 0.20f, UpgradeCategory.Mage));
                upgradePool.Add(CreateUpgrade("+25% Spell Area", "Increases Fireball explosion radius by 25%", UpgradeType.SpellAreaPercent, 0.25f, UpgradeCategory.Mage));

                // Druid Specific
                upgradePool.Add(CreateUpgrade("+25% Nature Recovery", "Increases healing and regeneration efficiency by 25%", UpgradeType.NatureRecoveryPercent, 0.25f, UpgradeCategory.Druid));
                upgradePool.Add(CreateUpgrade("+20% Spell Area", "Increases Nature Burst radius by 20%", UpgradeType.SpellAreaPercent, 0.20f, UpgradeCategory.Druid));
            }
        }

        private UpgradeData CreateUpgrade(string title, string desc, UpgradeType type, float val, UpgradeCategory cat)
        {
            UpgradeData u = ScriptableObject.CreateInstance<UpgradeData>();
            u.upgradeTitle = title;
            u.description = desc;
            u.type = type;
            u.statValue = val;
            u.category = cat;
            return u;
        }

        private void HandlePlayerLevelUp()
        {
            // Pause Game
            Time.timeScale = 0f;

            // Pick 3 random unique upgrades from pool
            List<UpgradeData> choices = GetRandomUpgradeChoices(3);
            OnLevelUpUpgradeChoices?.Invoke(choices);
        }

        public List<UpgradeData> GetRandomUpgradeChoices(int count)
        {
            Roguelite.Core.CharacterType currentChar = Roguelite.Core.GameSettings.Instance.SelectedCharacter;
            UpgradeCategory classCategory = UpgradeCategory.Knight;

            if (currentChar == Roguelite.Core.CharacterType.Mage) classCategory = UpgradeCategory.Mage;
            else if (currentChar == Roguelite.Core.CharacterType.Druid) classCategory = UpgradeCategory.Druid;

            // Filter pool for Universal OR active character class
            List<UpgradeData> validPool = upgradePool.FindAll(u => u.category == UpgradeCategory.Universal || u.category == classCategory);
            List<UpgradeData> copy = new List<UpgradeData>(validPool);
            List<UpgradeData> selected = new List<UpgradeData>();

            for (int i = 0; i < count && copy.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, copy.Count);
                selected.Add(copy[index]);
                copy.RemoveAt(index);
            }

            return selected;
        }

        public void SelectUpgrade(UpgradeData upgrade)
        {
            if (playerStats != null && upgrade != null)
            {
                playerStats.ApplyUpgrade(upgrade);
                if (Roguelite.Core.GameSessionManager.Instance != null && !Roguelite.Core.GameSessionManager.Instance.UnlockedUpgradeIDs.Contains(upgrade.upgradeTitle))
                {
                    Roguelite.Core.GameSessionManager.Instance.UnlockedUpgradeIDs.Add(upgrade.upgradeTitle);
                }
            }

            // Resume Game
            Time.timeScale = 1.0f;
        }

        public void ReapplySavedUpgrades(List<string> upgradeTitles)
        {
            InitializeDefaultPoolIfEmpty();
            if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats == null || upgradeTitles == null) return;

            foreach (var title in upgradeTitles)
            {
                UpgradeData match = upgradePool.Find(u => u.upgradeTitle == title);
                if (match != null)
                {
                    playerStats.ApplyUpgrade(match);
                }
            }
        }
    }
}
