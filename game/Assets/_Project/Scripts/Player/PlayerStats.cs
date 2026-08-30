using System;
using UnityEngine;
using Roguelite.Data;
using Roguelite.Combat;

namespace Roguelite.Player
{
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Character Data Reference")]
        [SerializeField] private CharacterData characterData;

        public CharacterData CharacterData => characterData;

        // Current Stat Values
        public float CurrentHP { get; private set; }
        public float MaxHP { get; private set; }
        public float CurrentStamina { get; private set; }
        public float MaxStamina { get; private set; }
        public int Level { get; private set; } = 1;
        public int CurrentXP { get; private set; }
        public int XPToNextLevel { get; private set; } = 100;

        // Upgrade Stat Modifiers
        public float DamageMultiplier { get; private set; } = 1.0f;
        public float FlatDamageBonus { get; private set; } = 0f;
        public float FlatDamage => (characterData != null ? characterData.baseAttackDamage : 25f) * DamageMultiplier + FlatDamageBonus;
        public float MoveSpeedMultiplier { get; private set; } = 1.0f;
        public float AttackSpeedMultiplier { get; private set; } = 1.0f;
        public float MaxStaminaMultiplier { get; private set; } = 1.0f;
        public float ExtraMaxStamina { get; private set; } = 0f;
        public float CritChanceBonus { get; private set; } = 0.0f;
        public float ExtraMaxHP { get; private set; } = 0f;

        public bool IsDead => CurrentHP <= 0;

        // Invulnerability (e.g., during dodge roll)
        public bool IsInvulnerable { get; set; } = false;

        // Events
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action<int, int, int> OnXPChanged; // current, target, level
        public event Action OnLevelUp;
        public event Action OnDeath;

        private void Awake()
        {
            if (characterData == null)
            {
                // Create fallback runtime CharacterData if missing
                characterData = ScriptableObject.CreateInstance<CharacterData>();
            }

            RecalculateStats();
            CurrentHP = MaxHP;
            CurrentStamina = MaxStamina;
        }

        private void Update()
        {
            // Auto Regenerate Stamina
            if (CurrentStamina < MaxStamina)
            {
                RegenerateStamina(characterData.staminaRegenRate * Time.deltaTime);
            }
        }

        public void RecalculateStats()
        {
            if (characterData == null)
            {
                characterData = ScriptableObject.CreateInstance<CharacterData>();
            }
            float prevMaxHP = MaxHP;
            MaxHP = (characterData.baseMaxHP + ExtraMaxHP);
            MaxStamina = (characterData.baseMaxStamina + ExtraMaxStamina) * MaxStaminaMultiplier;

            // Retain HP percentage if MaxHP changes
            if (prevMaxHP > 0)
            {
                float ratio = CurrentHP / prevMaxHP;
                CurrentHP = MaxHP * ratio;
            }
            else
            {
                CurrentHP = MaxHP;
            }

            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
            OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        }

        public void ModifyFlatDamage(float delta)
        {
            FlatDamageBonus += delta;
        }

        public void ModifyMoveSpeedMultiplier(float delta)
        {
            MoveSpeedMultiplier += delta;
        }

        public void ModifyDamageMultiplier(float delta)
        {
            DamageMultiplier += delta;
        }

        public void ModifyMaxHP(float delta)
        {
            ExtraMaxHP += delta;
            RecalculateStats();
        }

        public void ModifyMaxStamina(float delta)
        {
            ExtraMaxStamina += delta;
            RecalculateStats();
        }

        public bool ConsumeStamina(float amount)
        {
            if (CurrentStamina >= amount)
            {
                CurrentStamina -= amount;
                OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
                return true;
            }
            return false;
        }

        public void RegenerateStamina(float amount)
        {
            CurrentStamina = Mathf.Min(CurrentStamina + amount, MaxStamina);
            OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (IsDead || IsInvulnerable) return;

            if (damageInfo.amount > 0f)
            {
                Debug.Log($"[DAMAGE SOURCE] Attacker: '{damageInfo.attacker?.name ?? "NULL"}', Amount: {damageInfo.amount:F1}, Knockback: {damageInfo.knockbackForce:F1}, IsCrit: {damageInfo.isCritical}");
            }

            CurrentHP = Mathf.Max(CurrentHP - damageInfo.amount, 0f);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);

            if (CurrentHP <= 0)
            {
                Die();
            }
        }

        public void AddXP(int amount)
        {
            if (IsDead) return;

            if (Progression.ProgressionManager.Instance != null)
            {
                Progression.ProgressionManager.Instance.AddXP(amount);
                CurrentXP = Progression.ProgressionManager.Instance.CurrentLevelXP;
                Level = Progression.ProgressionManager.Instance.CurrentLevel;
                XPToNextLevel = Progression.ProgressionManager.Instance.GetXPRequired(Level);
            }
            else
            {
                CurrentXP += amount;
                while (CurrentXP >= XPToNextLevel)
                {
                    CurrentXP -= XPToNextLevel;
                    Level++;
                    XPToNextLevel = Mathf.RoundToInt(XPToNextLevel * 1.35f);
                    OnLevelUp?.Invoke();
                }
            }
            OnXPChanged?.Invoke(CurrentXP, XPToNextLevel, Level);
        }

        public void ApplyUpgrade(UpgradeData upgrade)
        {
            switch (upgrade.type)
            {
                case UpgradeType.AttackDamagePercent:
                    DamageMultiplier += upgrade.statValue;
                    break;
                case UpgradeType.MoveSpeedPercent:
                    MoveSpeedMultiplier += upgrade.statValue;
                    break;
                case UpgradeType.MaxHealthFlat:
                    ExtraMaxHP += upgrade.statValue;
                    CurrentHP += upgrade.statValue; // Instantly give the extra health
                    break;
                case UpgradeType.AttackSpeedPercent:
                    AttackSpeedMultiplier += upgrade.statValue;
                    break;
                case UpgradeType.MaxStaminaPercent:
                    MaxStaminaMultiplier += upgrade.statValue;
                    CurrentStamina += (characterData.baseMaxStamina * upgrade.statValue);
                    break;
                case UpgradeType.CritChancePercent:
                    CritChanceBonus += upgrade.statValue;
                    break;
                case UpgradeType.MagicDamagePercent:
                    var pCombatM = GetComponent<PlayerCombat>();
                    if (pCombatM != null) pCombatM.MagicDamageMultiplier += upgrade.statValue;
                    break;
                case UpgradeType.ProjectileSpeedPercent:
                    var pCombatP = GetComponent<PlayerCombat>();
                    if (pCombatP != null) pCombatP.ProjectileSpeedMultiplier += upgrade.statValue;
                    break;
                case UpgradeType.SpellAreaPercent:
                    var pCombatA = GetComponent<PlayerCombat>();
                    if (pCombatA != null) pCombatA.SpellAreaMultiplier += upgrade.statValue;
                    break;
                case UpgradeType.NatureRecoveryPercent:
                    var pCombatN = GetComponent<PlayerCombat>();
                    if (pCombatN != null) pCombatN.HealingEfficiencyMultiplier += upgrade.statValue;
                    break;
            }

            RecalculateStats();
        }

        public void SetSessionState(float hp, float maxHp, int level, int xp, int xpNext)
        {
            MaxHP = maxHp > 0 ? maxHp : MaxHP;
            CurrentHP = Mathf.Clamp(hp, 1f, MaxHP);
            Level = level > 0 ? level : 1;
            CurrentXP = xp;
            XPToNextLevel = xpNext > 0 ? xpNext : 100;

            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
            OnXPChanged?.Invoke(CurrentXP, XPToNextLevel, Level);
        }

        private void Die()
        {
            OnDeath?.Invoke();
        }
    }
}
