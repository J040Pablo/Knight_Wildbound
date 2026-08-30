using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Inventory;
using Roguelite.Player;
using Roguelite.Progression;
using Roguelite.Core;

namespace Roguelite.UI
{
    /// <summary>
    /// Terraria/Minecraft style minimal grid inventory screen opened with [I].
    /// Features character equipment slots on the left, a 6x4 item grid in the center, 
    /// item preview details, and gold balance on the bottom bar.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        private static InventoryUI instance;
        private static bool applicationIsQuitting = false;

        public static InventoryUI Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<InventoryUI>();
                    if (instance == null && !applicationIsQuitting)
                    {
                        GameObject go = new GameObject("InventoryUI");
                        instance = go.AddComponent<InventoryUI>();
                    }
                }
                return instance;
            }
        }

        public bool isOpen = false;
        private InventorySlot selectedSlot = null;
        private Vector2 scrollPosition = Vector2.zero;

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

        private void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                applicationIsQuitting = true;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I) || (isOpen && Input.GetKeyDown(KeyCode.Escape)))
            {
                ToggleInventory();
            }
        }

        public void ToggleInventory()
        {
            isOpen = !isOpen;

            if (isOpen)
            {
                Debug.Log("[Inventory] Toggle Open");
                if (InputStateManager.Instance != null)
                {
                    InputStateManager.Instance.SetUIMode();
                }
            }
            else
            {
                Debug.Log("[Inventory] Toggle Close");
                if (InputStateManager.Instance != null)
                {
                    InputStateManager.Instance.SetGameplayMode();
                }
                selectedSlot = null;
            }

            Time.timeScale = isOpen ? 0f : 1f;
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            GUI.depth = -20;
            GUI.skin.box.alignment = TextAnchor.MiddleCenter;

            // Main Background Panel (Terraria / Minecraft Dark Frame)
            float panelW = 860f;
            float panelH = 500f;
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = (Screen.height - panelH) * 0.5f;

            // Dark semi-transparent background frame
            GUI.color = new Color(0.05f, 0.06f, 0.09f, 0.94f);
            GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), Texture2D.whiteTexture);

            // Sleek border accent
            GUI.color = new Color(0.25f, 0.32f, 0.45f, 0.9f);
            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");

            // Header Title
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerStyle.normal.textColor = new Color(0.95f, 0.85f, 0.35f);
            GUI.Label(new Rect(panelX + 20, panelY + 10, 300, 26), "🎒 INVENTORY & EQUIPMENT", headerStyle);

            GUI.color = Color.gray;
            GUI.Label(new Rect(panelX + panelW - 220, panelY + 12, 200, 22), "Press [I] or [ESC] to Close");

            // -------------------------------------------------------------
            // 1. LEFT PANEL: Character Preview & Equipment Slots (240px wide)
            // -------------------------------------------------------------
            DrawEquipmentPanel(panelX + 15, panelY + 42, 250, panelH - 85);

            // -------------------------------------------------------------
            // 2. CENTER PANEL: 6x4 Grid Storage & Detail Box (575px wide)
            // -------------------------------------------------------------
            DrawGridInventory(panelX + 280, panelY + 42, panelW - 295, panelH - 85);

            // -------------------------------------------------------------
            // 3. BOTTOM BAR: Gold Counter
            // -------------------------------------------------------------
            DrawBottomBar(panelX + 15, panelY + panelH - 38, panelW - 30, 30);
        }

        private void DrawEquipmentPanel(float x, float y, float w, float h)
        {
            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.8f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.25f, 0.35f, 0.6f);
            GUI.Box(new Rect(x, y, w, h), "");

            // Character Class Header
            ClassType playerClass = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentClass : ClassType.Knight;
            string classTitle = playerClass == ClassType.None ? "UNKNOWN ADVENTURER" : playerClass.ToString().ToUpper();

            GUIStyle classStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            classStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 5, y + 8, w - 10, 22), $"👤 {classTitle}", classStyle);

            float slotY = y + 36;
            float slotH = 95f;

            DrawEquipSlotRow(x + 8, slotY, w - 16, slotH - 6, EquipmentSlot.Weapon, "⚔️ Weapon", EquipmentManager.Instance?.weaponSlot);
            slotY += slotH;

            DrawEquipSlotRow(x + 8, slotY, w - 16, slotH - 6, EquipmentSlot.Amulet, "📿 Amulet", EquipmentManager.Instance?.amuletSlot);
            slotY += slotH;

            DrawEquipSlotRow(x + 8, slotY, w - 16, slotH - 6, EquipmentSlot.Ring1, "💍 Ring Slot 1", EquipmentManager.Instance?.ringSlot1);
            slotY += slotH;

            DrawEquipSlotRow(x + 8, slotY, w - 16, slotH - 6, EquipmentSlot.Ring2, "💍 Ring Slot 2", EquipmentManager.Instance?.ringSlot2);
        }

        private void DrawEquipSlotRow(float x, float y, float w, float h, EquipmentSlot slot, string label, ItemData equippedItem)
        {
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

            GUI.color = equippedItem != null ? equippedItem.RarityColor : new Color(0.2f, 0.25f, 0.35f, 0.4f);
            GUI.Box(new Rect(x, y, w, h), "");

            GUI.skin.label.fontSize = 11;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.color = new Color(0.7f, 0.75f, 0.85f);
            GUI.Label(new Rect(x + 8, y + 4, w - 16, 18), label);

            if (equippedItem != null)
            {
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                nameStyle.normal.textColor = equippedItem.RarityColor;

                GUI.Label(new Rect(x + 10, y + 24, w - 85, 20), $"{equippedItem.iconGlyph} {equippedItem.itemName}", nameStyle);
                GUI.Label(new Rect(x + 10, y + 46, w - 85, 36), GetShortStatDesc(equippedItem));

                if (GUI.Button(new Rect(x + w - 75, y + (h - 26) / 2, 68, 26), "Unequip"))
                {
                    EquipmentManager.Instance?.Unequip(slot);
                }
            }
            else
            {
                GUI.color = Color.gray;
                GUI.Label(new Rect(x + 10, y + 32, w - 20, 20), "[Empty Slot]");
            }
        }

        private void DrawGridInventory(float x, float y, float w, float h)
        {
            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.8f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.25f, 0.35f, 0.6f);
            GUI.Box(new Rect(x, y, w, h), "");

            // Section Label
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 12, y + 8, 200, 20), "📦 Storage (Terraria Grid)");

            // 6 Columns x 4 Rows Grid (24 Slots Total)
            int cols = 6;
            int rows = 4;
            float slotSize = 58f;
            float gap = 8f;
            float gridStartX = x + 12;
            float gridStartY = y + 32;

            IReadOnlyList<InventorySlot> inventoryItems = InventoryManager.Instance?.Items;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int index = r * cols + c;
                    float slotX = gridStartX + c * (slotSize + gap);
                    float slotY = gridStartY + r * (slotSize + gap);

                    Rect slotRect = new Rect(slotX, slotY, slotSize, slotSize);

                    InventorySlot invSlot = (inventoryItems != null && index < inventoryItems.Count) ? inventoryItems[index] : null;

                    DrawGridCell(slotRect, invSlot, index);
                }
            }

            // Detail Preview Section below the Grid (y + 300)
            float detailY = y + 298f;
            float detailH = h - 304f;
            Rect detailRect = new Rect(x + 12, detailY, w - 24, detailH);

            DrawSelectedItemDetail(detailRect);
        }

        private void DrawGridCell(Rect rect, InventorySlot slot, int slotIndex)
        {
            bool isSelected = (slot != null && selectedSlot == slot);

            // Dark cell background
            GUI.color = isSelected ? new Color(0.18f, 0.28f, 0.45f, 0.95f) : new Color(0.04f, 0.05f, 0.08f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            // Border color by item rarity
            Color borderColor = (slot != null && slot.item != null) ? slot.item.RarityColor : new Color(0.18f, 0.22f, 0.3f, 0.5f);
            if (isSelected) borderColor = Color.yellow;

            GUI.color = borderColor;
            GUI.Box(rect, "");

            if (slot != null && slot.item != null)
            {
                // Item Icon Glyph
                GUIStyle glyphStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter
                };
                glyphStyle.normal.textColor = slot.item.RarityColor;
                GUI.Label(rect, slot.item.iconGlyph, glyphStyle);

                // Quantity tag if stackable > 1
                if (slot.quantity > 1)
                {
                    GUIStyle countStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 10,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.LowerRight
                    };
                    countStyle.normal.textColor = Color.yellow;
                    GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width - 6, rect.height - 4), $"x{slot.quantity}", countStyle);
                }

                // Click handler
                if (GUI.Button(rect, "", GUIStyle.none))
                {
                    selectedSlot = slot;
                }
            }
        }

        private void DrawSelectedItemDetail(Rect rect)
        {
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.25f, 0.32f, 0.45f, 0.7f);
            GUI.Box(rect, "");

            if (selectedSlot != null && selectedSlot.item != null)
            {
                ItemData item = selectedSlot.item;

                // Name & Rarity Header
                GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                titleStyle.normal.textColor = item.RarityColor;
                GUI.Label(new Rect(rect.x + 12, rect.y + 8, rect.width - 140, 22), $"{item.iconGlyph} {item.itemName} [{item.rarity}]", titleStyle);

                // Stats / Description
                GUI.skin.label.fontSize = 11;
                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x + 12, rect.y + 30, rect.width - 140, 36), item.description);

                string statDesc = GetShortStatDesc(item);
                if (!string.IsNullOrEmpty(statDesc))
                {
                    GUI.color = new Color(0.4f, 0.9f, 0.5f);
                    GUI.Label(new Rect(rect.x + 12, rect.y + 68, rect.width - 140, 20), $"Bonus: {statDesc}");
                }

                // Action Button (Equip / Use)
                float btnW = 110f;
                float btnH = 32f;
                float btnX = rect.x + rect.width - btnW - 12f;
                float btnY = rect.y + (rect.height - btnH) * 0.5f;

                if (item.category == ItemCategory.Weapon || item.category == ItemCategory.Amulet || item.category == ItemCategory.Ring)
                {
                    if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "Equip"))
                    {
                        EquipmentSlot slot = GetTargetEquipSlot(item);
                        EquipmentManager.Instance?.Equip(item, slot);
                        InventoryManager.Instance?.RemoveItem(item, 1);
                        selectedSlot = null;
                    }
                }
                else if (item.category == ItemCategory.Consumable)
                {
                    float cdRem = ConsumableItem.GetCooldownRemaining(item.itemId);
                    string useLabel = cdRem > 0f ? $"Wait ({cdRem:F0}s)" : "Use Potion";

                    if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), useLabel))
                    {
                        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
                        if (ConsumableItem.TryUse(item, stats))
                        {
                            InventoryManager.Instance?.RemoveItem(item, 1);
                            if (InventoryManager.Instance?.GetItemCount(item.itemId) <= 0)
                            {
                                selectedSlot = null;
                            }
                        }
                    }
                }
            }
            else
            {
                GUI.skin.label.fontSize = 11;
                GUI.color = Color.gray;
                GUI.Label(new Rect(rect.x + 12, rect.y + (rect.height - 20) * 0.5f, rect.width - 24, 20), "Select an item in storage grid to view details & actions.");
            }
        }

        private void DrawBottomBar(float x, float y, float w, float h)
        {
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.75f, 0.2f, 0.7f);
            GUI.Box(new Rect(x, y, w, h), "");

            int gold = InventoryManager.Instance != null ? InventoryManager.Instance.Gold : 0;

            GUIStyle goldStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            goldStyle.normal.textColor = new Color(1.0f, 0.85f, 0.25f);
            GUI.Label(new Rect(x + 15, y + 4, 300, 22), $"💰 GOLD BALANCE: {gold}", goldStyle);
        }

        private EquipmentSlot GetTargetEquipSlot(ItemData item)
        {
            if (item.category == ItemCategory.Weapon) return EquipmentSlot.Weapon;
            if (item.category == ItemCategory.Amulet) return EquipmentSlot.Amulet;

            if (EquipmentManager.Instance != null && EquipmentManager.Instance.ringSlot1 == null)
            {
                return EquipmentSlot.Ring1;
            }
            return EquipmentSlot.Ring2;
        }

        private string GetShortStatDesc(ItemData item)
        {
            if (item == null) return "";
            List<string> parts = new List<string>();

            if (item.flatDamageBonus > 0f)      parts.Add($"+{item.flatDamageBonus:F0} Dmg");
            if (item.flatHpBonus > 0f)          parts.Add($"+{item.flatHpBonus:F0} HP");
            if (item.flatStaminaBonus > 0f)     parts.Add($"+{item.flatStaminaBonus:F0} Stam");
            if (item.moveSpeedBonusPercent > 0f) parts.Add($"+{item.moveSpeedBonusPercent * 100f:F0}% Speed");
            if (item.healAmount > 0f)           parts.Add($"Heal {item.healAmount:F0}");
            if (item.restoresStaminaFully)      parts.Add("Full Stamina");

            return parts.Count > 0 ? string.Join(", ", parts) : "";
        }
    }
}
