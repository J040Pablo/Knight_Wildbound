using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Core.Events;
using Roguelite.Items;
using Roguelite.Inventory;
using Roguelite.Progression;

namespace Roguelite.Core.Save
{
    [Serializable]
    public class SavedItemData
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class GameSaveData
    {
        public int gold;
        public string selectedClass = "Knight";
        public int level = 1;
        public int currentXP = 0;

        public List<SavedItemData> inventoryItems = new List<SavedItemData>();

        public string weaponSlotId;
        public string amuletSlotId;
        public string ringSlot1Id;
        public string ringSlot2Id;

        public List<string> collectedRelics = new List<string>();

        public int masteryPath1Tier;
        public int masteryPath2Tier;
        public int masteryPath3Tier;
        public int pendingMasteryPoints;

        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 0.8f;
    }

    public class SaveManager : MonoBehaviour
    {
        private static SaveManager instance;
        public static SaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SaveManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SaveManager");
                        instance = go.AddComponent<SaveManager>();
                    }
                }
                return instance;
            }
        }

        [Header("System Toggle")]
        public bool UseNewSaveSystem = true;

        private const string SAVE_KEY = "Roguelite_UnifiedGameSave_v2";
        private bool isSavePending = false;
        private float saveDelayTimer = 0f;
        private const float SAVE_BATCH_DELAY = 0.5f;

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

        private void OnEnable()
        {
            GameEvents.OnGoldChanged += HandleGoldChanged;
            GameEvents.OnItemAdded += HandleItemAdded;
            GameEvents.OnItemRemoved += HandleItemRemoved;
            GameEvents.OnRelicCollected += HandleRelicCollected;
            GameEvents.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            GameEvents.OnGoldChanged -= HandleGoldChanged;
            GameEvents.OnItemAdded -= HandleItemAdded;
            GameEvents.OnItemRemoved -= HandleItemRemoved;
            GameEvents.OnRelicCollected -= HandleRelicCollected;
            GameEvents.OnLevelUp -= HandleLevelUp;
        }

        private void Start()
        {
            // LoadAll must only be invoked explicitly when restoring a saved run session,
            // never automatically on lazy component initialization mid-gameplay.
        }

        private void Update()
        {
            if (isSavePending)
            {
                saveDelayTimer -= Time.unscaledDeltaTime;
                if (saveDelayTimer <= 0f)
                {
                    isSavePending = false;
                    SaveAll();
                }
            }
        }

        public void RequestSave()
        {
            if (!UseNewSaveSystem) return;

            saveDelayTimer = SAVE_BATCH_DELAY;
            isSavePending = true;
        }

        public void SaveAll()
        {
            if (!UseNewSaveSystem) return;

            GameSaveData data = new GameSaveData();

            // 1. Inventory & Gold
            if (InventoryManager.Instance != null)
            {
                data.gold = InventoryManager.Instance.Gold;
                foreach (var slot in InventoryManager.Instance.Items)
                {
                    if (slot != null && slot.item != null)
                    {
                        data.inventoryItems.Add(new SavedItemData { itemId = slot.item.itemId, quantity = slot.quantity });
                    }
                }
            }

            // 2. Equipment
            if (EquipmentManager.Instance != null)
            {
                data.weaponSlotId = EquipmentManager.Instance.weaponSlot?.itemId;
                data.amuletSlotId = EquipmentManager.Instance.amuletSlot?.itemId;
                data.ringSlot1Id  = EquipmentManager.Instance.ringSlot1?.itemId;
                data.ringSlot2Id  = EquipmentManager.Instance.ringSlot2?.itemId;
            }

            // 3. Relics
            if (RelicManager.Instance != null)
            {
                data.collectedRelics = RelicManager.Instance.GetCollectedRelicIds();
            }

            // 4. Progression & Mastery
            if (ProgressionManager.Instance != null)
            {
                data.selectedClass = ProgressionManager.Instance.CurrentClass.ToString();
                data.level = ProgressionManager.Instance.CurrentLevel;
                data.currentXP = ProgressionManager.Instance.CurrentLevelXP;
                data.masteryPath1Tier = (int)ProgressionManager.Instance.GetTier(MasteryPath.Path1);
                data.masteryPath2Tier = (int)ProgressionManager.Instance.GetTier(MasteryPath.Path2);
                data.masteryPath3Tier = (int)ProgressionManager.Instance.GetTier(MasteryPath.Path3);
                data.pendingMasteryPoints = ProgressionManager.Instance.PendingLevelUpCount;
            }

            string json = JsonUtility.ToJson(data, true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();

            Debug.Log("[SaveManager] Game state successfully saved (Batched).");
        }

        public void LoadAll()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null) return;

                // 1. Relics
                if (RelicManager.Instance != null && data.collectedRelics != null)
                {
                    RelicManager.Instance.LoadSavedRelics(data.collectedRelics);
                }

                // 2. Inventory & Gold
                if (InventoryManager.Instance != null)
                {
                    List<InventorySlot> loadedSlots = new List<InventorySlot>();
                    if (data.inventoryItems != null)
                    {
                        foreach (var saved in data.inventoryItems)
                        {
                            ItemData item = ItemDatabase.Get(saved.itemId);
                            if (item != null)
                            {
                                loadedSlots.Add(new InventorySlot(item, saved.quantity));
                            }
                        }
                    }
                    InventoryManager.Instance.LoadInventoryState(loadedSlots, data.gold);
                }

                // 3. Equipment
                if (EquipmentManager.Instance != null)
                {
                    if (!string.IsNullOrEmpty(data.weaponSlotId))
                    {
                        ItemData w = ItemDatabase.Get(data.weaponSlotId);
                        if (w != null) EquipmentManager.Instance.EquipSilent(w, EquipmentSlot.Weapon);
                    }
                    if (!string.IsNullOrEmpty(data.amuletSlotId))
                    {
                        ItemData a = ItemDatabase.Get(data.amuletSlotId);
                        if (a != null) EquipmentManager.Instance.EquipSilent(a, EquipmentSlot.Amulet);
                    }
                    if (!string.IsNullOrEmpty(data.ringSlot1Id))
                    {
                        ItemData r1 = ItemDatabase.Get(data.ringSlot1Id);
                        if (r1 != null) EquipmentManager.Instance.EquipSilent(r1, EquipmentSlot.Ring1);
                    }
                    if (!string.IsNullOrEmpty(data.ringSlot2Id))
                    {
                        ItemData r2 = ItemDatabase.Get(data.ringSlot2Id);
                        if (r2 != null) EquipmentManager.Instance.EquipSilent(r2, EquipmentSlot.Ring2);
                    }
                }

                Debug.Log("[SaveManager] Unified save data successfully loaded.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load error: {ex.Message}");
            }
        }

        public void ClearSaveData()
        {
            isSavePending = false;
            saveDelayTimer = 0f;
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] Unified save data cleared.");
        }

        private void HandleGoldChanged(int newGold) => RequestSave();
        private void HandleItemAdded(ItemData item, int qty) => RequestSave();
        private void HandleItemRemoved(ItemData item, int qty) => RequestSave();
        private void HandleRelicCollected(string relicId) => RequestSave();
        private void HandleLevelUp(int newLevel) => RequestSave();
    }
}
