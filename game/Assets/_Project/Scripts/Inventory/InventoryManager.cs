using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Progression;

namespace Roguelite.Inventory
{
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;

        public InventorySlot(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }
    }

    /// <summary>
    /// Core singleton managing inventory storage, stack management, and Gold balance.
    /// Relics are automatically detected and routed to the campaign RelicManager without taking inventory space.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        private static InventoryManager instance;
        private static bool applicationIsQuitting = false;

        public static InventoryManager Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<InventoryManager>();
                    if (instance == null && !applicationIsQuitting)
                    {
                        GameObject go = new GameObject("InventoryManager");
                        instance = go.AddComponent<InventoryManager>();
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

        [Header("Inventory Settings")]
        public int maxInventoryCapacity = 30;

        [Header("Runtime Storage")]
        private readonly List<InventorySlot> items = new List<InventorySlot>();
        [SerializeField] private int gold = 0;

        public int Gold => gold;
        public IReadOnlyList<InventorySlot> Items => items.AsReadOnly();

        public event Action OnInventoryChanged;
        public event Action<int> OnGoldChanged;
        public event Action<ItemData, int, int> OnItemPickedUp; // item, quantity, totalGold

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

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            gold += amount;
            Debug.Log($"[InventoryManager] +{amount} Gold added. Total Gold: {gold}");
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();

            PlayerInventorySave.Instance?.SaveInventory(this);
        }

        public bool RemoveGold(int amount)
        {
            if (amount <= 0) return true;
            if (gold < amount) return false;

            gold -= amount;
            Debug.Log($"[InventoryManager] -{amount} Gold spent. Total Gold: {gold}");
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();

            PlayerInventorySave.Instance?.SaveInventory(this);
            return true;
        }

        /// <summary>
        /// Adds an item to the inventory. If the item is a Relic (e.g. Seed of the Ancient Tree),
        /// it routes directly to RelicManager as permanent campaign progression and takes 0 inventory slots.
        /// </summary>
        public bool AddItem(ItemData item, int count = 1)
        {
            if (item == null || count <= 0) return false;

            // Route Relics directly to RelicManager (Zero inventory slot rule)
            if (item.isRelic || item.category == ItemCategory.Relic || item.rarity == ItemRarity.Relic)
            {
                Debug.Log($"[InventoryManager] Item '{item.itemName}' is a Campaign Relic! Routing to RelicManager.");
                RelicManager.Instance.CollectRelic(item.itemId);
                OnItemPickedUp?.Invoke(item, 1, gold);
                return true;
            }

            // Standard Stackable / Unique Item Processing
            if (item.isStackable)
            {
                InventorySlot existingSlot = items.Find(s => s.item != null && s.item.itemId == item.itemId && s.quantity < item.maxStack);
                if (existingSlot != null)
                {
                    int spaceLeft = item.maxStack - existingSlot.quantity;
                    int toAdd = Mathf.Min(count, spaceLeft);
                    existingSlot.quantity += toAdd;

                    int remaining = count - toAdd;
                    if (remaining > 0)
                    {
                        // Recursively add remaining stack if capacity allows
                        AddItem(item, remaining);
                    }
                    else
                    {
                        NotifyItemAdded(item, count);
                    }
                    return true;
                }
            }

            // Create new inventory slot
            if (items.Count >= maxInventoryCapacity)
            {
                Debug.LogWarning($"[InventoryManager] Inventory full! Could not add '{item.itemName}'.");
                return false;
            }

            items.Add(new InventorySlot(item, count));
            NotifyItemAdded(item, count);
            return true;
        }

        private void NotifyItemAdded(ItemData item, int count)
        {
            Debug.Log($"[InventoryManager] Added x{count} '{item.itemName}' to inventory.");
            OnItemPickedUp?.Invoke(item, count, gold);
            OnInventoryChanged?.Invoke();

            PlayerInventorySave.Instance?.SaveInventory(this);
        }

        public bool RemoveItem(ItemData item, int count = 1)
        {
            if (item == null || count <= 0) return false;

            InventorySlot slot = items.Find(s => s.item != null && s.item.itemId == item.itemId);
            if (slot == null || slot.quantity < count) return false;

            slot.quantity -= count;
            if (slot.quantity <= 0)
            {
                items.Remove(slot);
            }

            OnInventoryChanged?.Invoke();
            PlayerInventorySave.Instance?.SaveInventory(this);
            return true;
        }

        public bool HasItem(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            InventorySlot slot = items.Find(s => s.item != null && s.item.itemId == itemId);
            return slot != null && slot.quantity >= count;
        }

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            InventorySlot slot = items.Find(s => s.item != null && s.item.itemId == itemId);
            return slot != null ? slot.quantity : 0;
        }

        public void SetGoldDirect(int newGold)
        {
            gold = Mathf.Max(0, newGold);
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();
        }

        public void ResetInventory()
        {
            items.Clear();
            gold = 0;
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();
            Debug.Log("[InventoryManager] Inventory reset to empty for fresh run.");
        }

        public void LoadInventoryState(List<InventorySlot> loadedItems, int loadedGold)
        {
            items.Clear();
            if (loadedItems != null)
            {
                items.AddRange(loadedItems);
            }
            gold = Mathf.Max(0, loadedGold);
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();
        }
    }
}
