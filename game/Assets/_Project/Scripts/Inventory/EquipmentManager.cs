using System;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Player;

namespace Roguelite.Inventory
{
    public enum EquipmentSlot
    {
        Weapon,
        Amulet,
        Belt,
        Ring1,
        Ring2
    }

    /// <summary>
    /// Global stealth state helper so enemy AI scripts can immediately check player visibility.
    /// </summary>
    public static class StealthState
    {
        public static bool IsPlayerInvisible { get; set; } = false;
        public static float InvisibilityDurationRemaining { get; set; } = 0f;
    }

    /// <summary>
    /// Manages active player equipment slots (Weapon, Amulet, Belt, Ring 1, Ring 2).
    /// Dynamically applies and reverts passive stat bonuses on PlayerStats.
    /// Manages the Legendary Ring of Shadows active ability [R].
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        private static EquipmentManager instance;
        private static bool applicationIsQuitting = false;

        public static EquipmentManager Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<EquipmentManager>();
                    if (instance == null && !applicationIsQuitting)
                    {
                        GameObject go = new GameObject("EquipmentManager");
                        instance = go.AddComponent<EquipmentManager>();
                    }
                }
                return instance;
            }
        }

        private void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        [Header("Equipped Slots")]
        public ItemData weaponSlot;
        public ItemData amuletSlot;
        public ItemData beltSlot;
        public ItemData ringSlot1;
        public ItemData ringSlot2;

        [Header("Ring of Shadows Ability [R]")]
        private float shadowStealthTimer = 0f;
        private float shadowCooldownTimer = 0f;
        private const float SHADOW_STEALTH_DURATION = 10f;
        private const float SHADOW_STEALTH_COOLDOWN = 60f;

        public event Action<EquipmentSlot, ItemData> OnEquipmentChanged;
        public event Action<bool> OnStealthStateChanged;

        public float ShadowCooldownRemaining => Mathf.Max(0f, shadowCooldownTimer);
        public float ShadowCooldownMax => SHADOW_STEALTH_COOLDOWN;
        public bool IsStealthActive => StealthState.IsPlayerInvisible;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Update Ring of Shadows active timers
            if (StealthState.IsPlayerInvisible)
            {
                shadowStealthTimer -= Time.deltaTime;
                StealthState.InvisibilityDurationRemaining = Mathf.Max(0f, shadowStealthTimer);

                if (shadowStealthTimer <= 0f)
                {
                    DeactivateShadowStealth();
                }
            }

            if (shadowCooldownTimer > 0f)
            {
                shadowCooldownTimer -= Time.deltaTime;
            }

            // Key R activation
            if (Input.GetKeyDown(KeyCode.R))
            {
                TryActivateRingOfShadows();
            }
        }

        public ItemData GetEquipped(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: return weaponSlot;
                case EquipmentSlot.Amulet: return amuletSlot;
                case EquipmentSlot.Belt:   return beltSlot;
                case EquipmentSlot.Ring1:  return ringSlot1;
                case EquipmentSlot.Ring2:  return ringSlot2;
                default: return null;
            }
        }

        public bool Equip(ItemData item, EquipmentSlot slot)
        {
            if (item == null) return Unequip(slot);

            // Validate category matching
            if (slot == EquipmentSlot.Weapon && item.category != ItemCategory.Weapon) return false;
            if (slot == EquipmentSlot.Amulet && item.category != ItemCategory.Amulet) return false;
            if (slot == EquipmentSlot.Belt   && item.category != ItemCategory.Belt)   return false;
            if ((slot == EquipmentSlot.Ring1 || slot == EquipmentSlot.Ring2) && item.category != ItemCategory.Ring) return false;

            // Unequip currently equipped item in this slot
            ItemData oldItem = GetEquipped(slot);
            if (oldItem != null)
            {
                RemoveStatBonuses(oldItem);
                InventoryManager.Instance?.AddItem(oldItem, 1);
            }

            // Set new item
            SetSlot(slot, item);
            ApplyStatBonuses(item);

            // Debug.Log($"[EquipmentManager] Equipped '{item.itemName}' into {slot} slot.");
            OnEquipmentChanged?.Invoke(slot, item);

            // Save state
            PlayerInventorySave.Instance?.SaveEquipment(this);
            return true;
        }

        /// <summary>
        /// Moves an already-equipped item from one slot to another (e.g. dragging Ring 1 onto Ring 2).
        /// If the destination slot is occupied, the two items swap places.
        /// </summary>
        public bool MoveEquipped(EquipmentSlot from, EquipmentSlot to)
        {
            if (from == to) return false;

            ItemData movingItem = GetEquipped(from);
            if (movingItem == null) return false;

            // Validate category fits destination slot
            bool categoryFits =
                (to == EquipmentSlot.Weapon && movingItem.category == ItemCategory.Weapon) ||
                (to == EquipmentSlot.Amulet && movingItem.category == ItemCategory.Amulet) ||
                (to == EquipmentSlot.Belt   && movingItem.category == ItemCategory.Belt) ||
                ((to == EquipmentSlot.Ring1 || to == EquipmentSlot.Ring2) && movingItem.category == ItemCategory.Ring);
            if (!categoryFits) return false;

            ItemData destinationItem = GetEquipped(to);

            SetSlot(from, destinationItem);
            SetSlot(to, movingItem);

            OnEquipmentChanged?.Invoke(from, destinationItem);
            OnEquipmentChanged?.Invoke(to, movingItem);

            PlayerInventorySave.Instance?.SaveEquipment(this);
            return true;
        }

        /// <summary>
        /// Silently equips an item during game save load without unequipping previous items or adding items to inventory.
        /// </summary>
        public void EquipSilent(ItemData item, EquipmentSlot slot)
        {
            if (item == null) return;
            ItemData oldItem = GetEquipped(slot);
            if (oldItem != null)
            {
                RemoveStatBonuses(oldItem);
            }

            SetSlot(slot, item);
            ApplyStatBonuses(item);
            OnEquipmentChanged?.Invoke(slot, item);
        }

        public bool Unequip(EquipmentSlot slot)
        {
            ItemData oldItem = GetEquipped(slot);
            if (oldItem == null) return false;

            RemoveStatBonuses(oldItem);
            SetSlot(slot, null);

            InventoryManager.Instance?.AddItem(oldItem, 1);

            // Debug.Log($"[EquipmentManager] Unequipped {slot} slot ('{oldItem.itemName}').");
            OnEquipmentChanged?.Invoke(slot, null);

            PlayerInventorySave.Instance?.SaveEquipment(this);
            return true;
        }

        public void ResetEquipment()
        {
            weaponSlot = null;
            amuletSlot = null;
            beltSlot = null;
            ringSlot1 = null;
            ringSlot2 = null;
            // Debug.Log("[EquipmentManager] Equipment reset to empty for fresh run.");
        }

        private void SetSlot(EquipmentSlot slot, ItemData item)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: weaponSlot = item; break;
                case EquipmentSlot.Amulet: amuletSlot = item; break;
                case EquipmentSlot.Belt:   beltSlot = item; break;
                case EquipmentSlot.Ring1:  ringSlot1 = item; break;
                case EquipmentSlot.Ring2:  ringSlot2 = item; break;
            }
        }

        private void ApplyStatBonuses(ItemData item)
        {
            if (item == null) return;
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            if (stats == null) return;

            if (item.flatDamageBonus > 0f)      stats.ModifyFlatDamage(item.flatDamageBonus);
            if (item.flatHpBonus > 0f)          stats.ModifyMaxHP(item.flatHpBonus);
            if (item.flatStaminaBonus > 0f)     stats.ModifyMaxStamina(item.flatStaminaBonus);
            if (item.moveSpeedBonusPercent > 0f) stats.ModifyMoveSpeedMultiplier(item.moveSpeedBonusPercent);
        }

        private void RemoveStatBonuses(ItemData item)
        {
            if (item == null) return;
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            if (stats == null) return;

            if (item.flatDamageBonus > 0f)      stats.ModifyFlatDamage(-item.flatDamageBonus);
            if (item.flatHpBonus > 0f)          stats.ModifyMaxHP(-item.flatHpBonus);
            if (item.flatStaminaBonus > 0f)     stats.ModifyMaxStamina(-item.flatStaminaBonus);
            if (item.moveSpeedBonusPercent > 0f) stats.ModifyMoveSpeedMultiplier(-item.moveSpeedBonusPercent);
        }

        public bool IsRingOfShadowsEquipped()
        {
            return (ringSlot1 != null && ringSlot1.itemId == "ring_of_shadows") ||
                   (ringSlot2 != null && ringSlot2.itemId == "ring_of_shadows");
        }

        /// <summary>Bloomheart (Giant Toxic Mushroom mini-boss relic) — checked by
        /// OnHitRelicEffects.TryTriggerBloomheart on every kill.</summary>
        public bool IsBloomheartEquipped()
        {
            return amuletSlot != null && amuletSlot.itemId == "relic_bloomheart";
        }

        public void TryActivateRingOfShadows()
        {
            if (!IsRingOfShadowsEquipped())
            {
                return;
            }

            if (shadowCooldownTimer > 0f)
            {
                // Debug.Log($"[Ring of Shadows] Ability on cooldown! {shadowCooldownTimer:F1}s remaining.");
                return;
            }

            shadowStealthTimer = SHADOW_STEALTH_DURATION;
            shadowCooldownTimer = SHADOW_STEALTH_COOLDOWN;
            StealthState.IsPlayerInvisible = true;
            StealthState.InvisibilityDurationRemaining = SHADOW_STEALTH_DURATION;

            // Debug.Log("🌑 [SHADOWS EMBRACE YOU] — Player is now INVISIBLE for 10 seconds!");
            OnStealthStateChanged?.Invoke(true);
        }

        private void DeactivateShadowStealth()
        {
            StealthState.IsPlayerInvisible = false;
            StealthState.InvisibilityDurationRemaining = 0f;
            // Debug.Log("☀️ [SHADOWS FADE] — Player is visible again.");
            OnStealthStateChanged?.Invoke(false);
        }
    }
}
