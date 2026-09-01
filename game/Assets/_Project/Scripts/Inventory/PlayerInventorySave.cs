using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Progression;
using Roguelite.Core.Save;

namespace Roguelite.Inventory
{
    [Serializable]
    public class SavedItemData
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class PlayerSavePacket
    {
        public int gold;
        public List<SavedItemData> inventoryItems = new List<SavedItemData>();
        public string weaponSlotId;
        public string amuletSlotId;
        public string beltSlotId;
        public string ringSlot1Id;
        public string ringSlot2Id;
        public List<string> collectedRelics = new List<string>();
    }

    public class PlayerInventorySave : MonoBehaviour
    {
        private static PlayerInventorySave instance;
        public static PlayerInventorySave Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PlayerInventorySave>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("PlayerInventorySave");
                        instance = go.AddComponent<PlayerInventorySave>();
                    }
                }
                return instance;
            }
        }

        private const string SAVE_KEY = "Roguelite_PlayerSaveData_v1";

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

        public void SaveAll()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.UseNewSaveSystem)
            {
                SaveManager.Instance.RequestSave();
                return;
            }

            PlayerSavePacket packet = new PlayerSavePacket();

            // Save Gold & Inventory
            if (InventoryManager.Instance != null)
            {
                packet.gold = InventoryManager.Instance.Gold;
                foreach (var slot in InventoryManager.Instance.Items)
                {
                    if (slot != null && slot.item != null)
                    {
                        packet.inventoryItems.Add(new SavedItemData { itemId = slot.item.itemId, quantity = slot.quantity });
                    }
                }
            }

            // Save Equipment
            if (EquipmentManager.Instance != null)
            {
                packet.weaponSlotId = EquipmentManager.Instance.weaponSlot?.itemId;
                packet.amuletSlotId = EquipmentManager.Instance.amuletSlot?.itemId;
                packet.beltSlotId   = EquipmentManager.Instance.beltSlot?.itemId;
                packet.ringSlot1Id  = EquipmentManager.Instance.ringSlot1?.itemId;
                packet.ringSlot2Id  = EquipmentManager.Instance.ringSlot2?.itemId;
            }

            // Save Relics
            if (RelicManager.Instance != null)
            {
                packet.collectedRelics = RelicManager.Instance.GetCollectedRelicIds();
            }

            string json = JsonUtility.ToJson(packet);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            // Debug.Log("[PlayerInventorySave (Legacy)] Inventory, Equipment, and Relics saved.");
        }

        public void SaveInventory(InventoryManager manager)
        {
            SaveAll();
        }

        public void ClearSaveData()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            // Debug.Log("[PlayerInventorySave] Saved data deleted for fresh run.");
        }

        public void SaveEquipment(EquipmentManager manager)
        {
            SaveAll();
        }

        public void LoadAll()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                PlayerSavePacket packet = JsonUtility.FromJson<PlayerSavePacket>(json);
                if (packet == null) return;

                // Load Relics
                if (RelicManager.Instance != null && packet.collectedRelics != null)
                {
                    RelicManager.Instance.LoadSavedRelics(packet.collectedRelics);
                }

                // Load Inventory & Gold
                if (InventoryManager.Instance != null)
                {
                    List<InventorySlot> loadedSlots = new List<InventorySlot>();
                    if (packet.inventoryItems != null)
                    {
                        foreach (var saved in packet.inventoryItems)
                        {
                            ItemData item = ItemDatabase.Get(saved.itemId);
                            if (item != null)
                            {
                                loadedSlots.Add(new InventorySlot(item, saved.quantity));
                            }
                        }
                    }
                    InventoryManager.Instance.LoadInventoryState(loadedSlots, packet.gold);
                }

                // Load Equipment
                if (EquipmentManager.Instance != null)
                {
                    if (!string.IsNullOrEmpty(packet.weaponSlotId))
                    {
                        ItemData w = ItemDatabase.Get(packet.weaponSlotId);
                        if (w != null) EquipmentManager.Instance.EquipSilent(w, EquipmentSlot.Weapon);
                    }
                    if (!string.IsNullOrEmpty(packet.amuletSlotId))
                    {
                        ItemData a = ItemDatabase.Get(packet.amuletSlotId);
                        if (a != null) EquipmentManager.Instance.EquipSilent(a, EquipmentSlot.Amulet);
                    }
                    if (!string.IsNullOrEmpty(packet.beltSlotId))
                    {
                        ItemData bl = ItemDatabase.Get(packet.beltSlotId);
                        if (bl != null) EquipmentManager.Instance.EquipSilent(bl, EquipmentSlot.Belt);
                    }
                    if (!string.IsNullOrEmpty(packet.ringSlot1Id))
                    {
                        ItemData r1 = ItemDatabase.Get(packet.ringSlot1Id);
                        if (r1 != null) EquipmentManager.Instance.EquipSilent(r1, EquipmentSlot.Ring1);
                    }
                    if (!string.IsNullOrEmpty(packet.ringSlot2Id))
                    {
                        ItemData r2 = ItemDatabase.Get(packet.ringSlot2Id);
                        if (r2 != null) EquipmentManager.Instance.EquipSilent(r2, EquipmentSlot.Ring2);
                    }
                }

                // Debug.Log("[PlayerInventorySave] State successfully loaded.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventorySave] Load error: {ex.Message}");
            }
        }
    }
}
