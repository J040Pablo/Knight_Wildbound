using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Inventory;
using Roguelite.Player;
using Roguelite.Progression;
using Roguelite.Core;
using Roguelite.Core.StateMachine;

namespace Roguelite.UI
{
    public class CharacterWindowUI : MonoBehaviour
    {
        private static CharacterWindowUI instance;
        private static bool applicationIsQuitting = false;

        public static CharacterWindowUI Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<CharacterWindowUI>();
                    if (instance == null && !applicationIsQuitting)
                    {
                        GameObject go = new GameObject("CharacterWindowUI");
                        instance = go.AddComponent<CharacterWindowUI>();
                    }
                }
                return instance;
            }
        }

        public enum WindowTab
        {
            Inventory = 0,
            Mastery = 1
        }

        public bool isOpen = false;
        public WindowTab currentTab = WindowTab.Inventory;

        // Inventory tab states
        private InventorySlot selectedSlot = null;

        // Mastery tab states
        private ClassUpgradeDefinition selectedUpgradeNode = null;

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

        private void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                applicationIsQuitting = true;
            }
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Inventory)
            {
                isOpen = true;
                currentTab = WindowTab.Inventory;
            }
            else if (newState == GameState.Mastery)
            {
                isOpen = true;
                currentTab = WindowTab.Mastery;
                RefreshMastery();
            }
            else if (newState == GameState.Gameplay)
            {
                isOpen = false;
                selectedSlot = null;
            }
        }

        public void OpenTab(WindowTab tab)
        {
            currentTab = tab;
            isOpen = true;
            GameState targetState = tab == WindowTab.Inventory ? GameState.Inventory : GameState.Mastery;
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(targetState);
            }
            if (tab == WindowTab.Mastery)
            {
                RefreshMastery();
            }
        }

        public void CloseWindow()
        {
            isOpen = false;
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
            }
        }

        public void ToggleTab(WindowTab tab)
        {
            if (isOpen && currentTab == tab)
            {
                CloseWindow();
            }
            else
            {
                OpenTab(tab);
            }
        }

        public void SwitchTab(WindowTab newTab)
        {
            currentTab = newTab;
            GameState targetState = newTab == WindowTab.Inventory ? GameState.Inventory : GameState.Mastery;
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(targetState);
            }
            if (newTab == WindowTab.Mastery)
            {
                RefreshMastery();
            }
        }

        private void Update()
        {
            // [E] opens or closes character window
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (isOpen)
                {
                    CloseWindow();
                }
                else
                {
                    OpenTab(currentTab);
                }
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleTab(WindowTab.Inventory);
            }
            else if (isOpen)
            {
                // [Tab] switches between Inventory (0) and Mastery (1)
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    WindowTab nextTab = currentTab == WindowTab.Inventory ? WindowTab.Mastery : WindowTab.Inventory;
                    SwitchTab(nextTab);
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseWindow();
                }
            }
        }

        public void RefreshMastery()
        {
            if (selectedUpgradeNode == null && ProgressionManager.Instance != null)
            {
                ClassDefinition def = ProgressionManager.Instance.GetActiveClassDefinition();
                if (def != null && def.upgrades != null && def.upgrades.Count > 0)
                {
                    selectedUpgradeNode = def.upgrades[0];
                }
            }
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            GUI.depth = -30;
            GUI.skin.box.alignment = TextAnchor.MiddleCenter;

            // Fullscreen Semi-transparent Dark Backdrop
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Main Modal Window (900px wide x 540px high)
            float windowW = 900f;
            float windowH = 540f;
            float windowX = (Screen.width - windowW) * 0.5f;
            float windowY = (Screen.height - windowH) * 0.5f;

            // Window Base Panel
            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            GUI.DrawTexture(new Rect(windowX, windowY, windowW, windowH), Texture2D.whiteTexture);

            // Window Gold Border Frame
            GUI.color = new Color(0.85f, 0.70f, 0.25f, 0.9f);
            GUI.Box(new Rect(windowX, windowY, windowW, windowH), "");

            // -------------------------------------------------------------
            // TOP TAB HEADER BAR (Y = windowY to windowY + 45)
            // -------------------------------------------------------------
            DrawTabHeader(windowX, windowY, windowW, 45f);

            // -------------------------------------------------------------
            // TAB CONTENT AREA (Y = windowY + 46 to windowY + windowH)
            // -------------------------------------------------------------
            float contentX = windowX + 15f;
            float contentY = windowY + 50f;
            float contentW = windowW - 30f;
            float contentH = windowH - 60f;

            if (currentTab == WindowTab.Inventory)
            {
                DrawInventoryTabContent(contentX, contentY, contentW, contentH);
            }
            else
            {
                DrawMasteryTabContent(contentX, contentY, contentW, contentH);
            }

            GUI.color = Color.white;
        }

        private void DrawTabHeader(float x, float y, float w, float h)
        {
            // Header Bar Background
            GUI.color = new Color(0.03f, 0.04f, 0.06f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.25f, 0.35f, 0.5f);
            GUI.Box(new Rect(x, y, w, h), "");

            float tabW = 220f;
            float tabH = 35f;
            float tabY = y + (h - tabH) * 0.5f;

            // Tab 0: Inventory [I]
            bool invActive = currentTab == WindowTab.Inventory;
            GUI.color = invActive ? new Color(0.85f, 0.70f, 0.2f, 0.95f) : new Color(0.12f, 0.16f, 0.24f, 0.8f);
            Rect invTabRect = new Rect(x + 15, tabY, tabW, tabH);
            GUI.DrawTexture(invTabRect, Texture2D.whiteTexture);
            GUI.color = invActive ? Color.yellow : new Color(0.4f, 0.45f, 0.55f);
            GUI.Box(invTabRect, "");

            GUIStyle tabStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            tabStyle.normal.textColor = invActive ? Color.black : Color.white;
            GUI.Label(invTabRect, "🎒 INVENTÁRIO & EQUIP", tabStyle);

            if (GUI.Button(invTabRect, "", GUIStyle.none))
            {
                SwitchTab(WindowTab.Inventory);
            }

            // Tab 1: Mastery
            bool masteryActive = currentTab == WindowTab.Mastery;
            GUI.color = masteryActive ? new Color(0.85f, 0.70f, 0.2f, 0.95f) : new Color(0.12f, 0.16f, 0.24f, 0.8f);
            Rect masteryTabRect = new Rect(x + 245, tabY, tabW, tabH);
            GUI.DrawTexture(masteryTabRect, Texture2D.whiteTexture);
            GUI.color = masteryActive ? Color.yellow : new Color(0.4f, 0.45f, 0.55f);
            GUI.Box(masteryTabRect, "");

            tabStyle.normal.textColor = masteryActive ? Color.black : Color.white;
            GUI.Label(masteryTabRect, "⚔️ MAESTRIA & UPGRADES", tabStyle);

            if (GUI.Button(masteryTabRect, "", GUIStyle.none))
            {
                SwitchTab(WindowTab.Mastery);
            }

            // Tab Toggle Hint
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            hintStyle.normal.textColor = new Color(0.9f, 0.8f, 0.4f);
            GUI.Label(new Rect(x + w - 240, y + 10, 180, 24), "Alternar Abas: [TAB]", hintStyle);

            // Close Button [X] Top Right
            Rect closeBtnRect = new Rect(x + w - 45, y + (h - 30) * 0.5f, 30, 30);
            GUI.color = new Color(0.85f, 0.2f, 0.2f);
            if (GUI.Button(closeBtnRect, "X"))
            {
                CloseWindow();
            }
            GUI.color = Color.white;
        }

        // =========================================================================
        // TAB 0: INVENTORY CONTENT
        // =========================================================================
        private void DrawInventoryTabContent(float x, float y, float w, float h)
        {
            float leftW = 260f;
            float rightX = x + leftW + 15f;
            float rightW = w - leftW - 15f;

            // 1. LEFT PANEL: Character Equipment & Attributes
            DrawEquipmentPanel(x, y, leftW, h - 38f);

            // 2. RIGHT PANEL: 6x4 Item Storage Grid & Details
            DrawGridInventory(rightX, y, rightW, h - 38f);

            // 3. BOTTOM BAR: Gold Balance
            DrawBottomBar(x, y + h - 32f, w, 32f);
        }

        private void DrawEquipmentPanel(float x, float y, float w, float h)
        {
            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.25f, 0.32f, 0.45f, 0.7f);
            GUI.Box(new Rect(x, y, w, h), "");

            ClassType playerClass = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentClass : ClassType.Knight;
            string classTitle = playerClass == ClassType.None ? "SEM CLASSE" : playerClass.ToString().ToUpper();

            GUIStyle classStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            classStyle.normal.textColor = new Color(1.0f, 0.85f, 0.3f);
            GUI.Label(new Rect(x + 5, y + 8, w - 10, 22), $"CLASSE: {classTitle}", classStyle);

            float slotY = y + 36;
            float slotH = 92f;

            DrawEquipSlotRow(x + 8, slotY, w - 16, slotH - 6, EquipmentSlot.Weapon, "Arma Principal", EquipmentManager.Instance?.weaponSlot);
            slotY += slotH;

            DrawEquipSlotRow(x + 8, slotY, w - 16, slotH - 6, EquipmentSlot.Amulet, "Amuleto", EquipmentManager.Instance?.amuletSlot);
            slotY += slotH;

            DrawEquipSlotRow(x + 8, slotY, w - 16, slotH - 6, EquipmentSlot.Ring1, "Anel Slot 1", EquipmentManager.Instance?.ringSlot1);
            slotY += slotH;

            DrawEquipSlotRow(x + 8, slotY, w - 16, slotH - 6, EquipmentSlot.Ring2, "Anel Slot 2", EquipmentManager.Instance?.ringSlot2);
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

                GUI.Label(new Rect(x + 10, y + 24, w - 85, 20), equippedItem.itemName, nameStyle);
                GUI.Label(new Rect(x + 10, y + 46, w - 85, 36), GetShortStatDesc(equippedItem));

                if (GUI.Button(new Rect(x + w - 75, y + (h - 26) / 2, 68, 26), "Desequipar"))
                {
                    EquipmentManager.Instance?.Unequip(slot);
                }
            }
            else
            {
                GUI.color = Color.gray;
                GUI.Label(new Rect(x + 10, y + 32, w - 20, 20), "[Slot Vazio]");
            }
        }

        private void DrawGridInventory(float x, float y, float w, float h)
        {
            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.25f, 0.32f, 0.45f, 0.7f);
            GUI.Box(new Rect(x, y, w, h), "");

            GUI.skin.label.fontSize = 12;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 12, y + 8, 200, 20), "Bolsa de Itens");

            int cols = 6;
            int rows = 4;
            float slotSize = 62f;
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

            float detailY = y + 315f;
            float detailH = h - 322f;
            Rect detailRect = new Rect(x + 12, detailY, w - 24, detailH);

            DrawSelectedItemDetail(detailRect);
        }

        private void DrawGridCell(Rect rect, InventorySlot slot, int slotIndex)
        {
            bool isSelected = (slot != null && selectedSlot == slot);

            GUI.color = isSelected ? new Color(0.18f, 0.28f, 0.45f, 0.95f) : new Color(0.04f, 0.05f, 0.08f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            Color borderColor = (slot != null && slot.item != null) ? slot.item.RarityColor : new Color(0.18f, 0.22f, 0.3f, 0.5f);
            if (isSelected) borderColor = Color.yellow;

            GUI.color = borderColor;
            GUI.Box(rect, "");

            if (slot != null && slot.item != null)
            {
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                nameStyle.normal.textColor = slot.item.RarityColor;
                GUI.Label(rect, slot.item.itemName, nameStyle);

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

                GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold
                };
                titleStyle.normal.textColor = item.RarityColor;
                GUI.Label(new Rect(rect.x + 12, rect.y + 6, rect.width - 140, 20), $"{item.itemName} [{item.rarity}]", titleStyle);

                GUI.skin.label.fontSize = 11;
                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x + 12, rect.y + 26, rect.width - 140, 34), item.description);

                string statDesc = GetShortStatDesc(item);
                if (!string.IsNullOrEmpty(statDesc))
                {
                    GUI.color = new Color(0.4f, 0.9f, 0.5f);
                    GUI.Label(new Rect(rect.x + 12, rect.y + 62, rect.width - 140, 20), $"Bônus: {statDesc}");
                }

                float btnW = 110f;
                float btnH = 32f;
                float btnX = rect.x + rect.width - btnW - 12f;
                float btnY = rect.y + (rect.height - btnH) * 0.5f;

                if (item.category == ItemCategory.Weapon || item.category == ItemCategory.Amulet || item.category == ItemCategory.Ring)
                {
                    if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "Equipar"))
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
                    string useLabel = cdRem > 0f ? $"Aguarde ({cdRem:F0}s)" : "Usar Poção";

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
                GUI.Label(new Rect(rect.x + 12, rect.y + (rect.height - 20) * 0.5f, rect.width - 24, 20), "Selecione um item na grade para ver detalhes e ações.");
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
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            goldStyle.normal.textColor = new Color(1.0f, 0.85f, 0.25f);
            GUI.Label(new Rect(x + 15, y + 5, 300, 22), $"OURO ACUMULADO: {gold}", goldStyle);
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

            if (item.flatDamageBonus > 0f) parts.Add($"+{item.flatDamageBonus:F0} Dano");
            if (item.flatHpBonus > 0f) parts.Add($"+{item.flatHpBonus:F0} HP");
            if (item.flatStaminaBonus > 0f) parts.Add($"+{item.flatStaminaBonus:F0} Stamina");
            if (item.moveSpeedBonusPercent > 0f) parts.Add($"+{item.moveSpeedBonusPercent * 100f:F0}% Vel.");
            if (item.healAmount > 0f) parts.Add($"Cura {item.healAmount:F0}");
            if (item.restoresStaminaFully) parts.Add("Stamina Total");

            return parts.Count > 0 ? string.Join(", ", parts) : "";
        }

        // =========================================================================
        // TAB 1: MASTERY / UPGRADE CONTENT
        // =========================================================================
        private void DrawMasteryTabContent(float x, float y, float w, float h)
        {
            if (ProgressionManager.Instance == null || ProgressionManager.Instance.CurrentClass == ClassType.None)
            {
                DrawNoClassSelectedView(x, y, w, h);
                return;
            }

            // Top Subheader Info
            ClassDefinition def = ProgressionManager.Instance.GetActiveClassDefinition();
            string className = def != null ? def.className.ToUpper() : ProgressionManager.Instance.CurrentClass.ToString().ToUpper();
            int level = ProgressionManager.Instance.CurrentLevel;
            int points = ProgressionManager.Instance.PendingLevelUpCount;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = new Color(1.0f, 0.88f, 0.3f);
            GUI.Label(new Rect(x + 10, y, 350, 24), $"TRILHA DE MAESTRIA: {className}");

            GUIStyle pointsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            pointsStyle.normal.textColor = points > 0 ? Color.cyan : Color.gray;
            GUI.Label(new Rect(x + w - 360, y, 350, 24), $"NÍVEL {level}  •  PONTOS DISPONÍVEIS: {points}", pointsStyle);

            // Columns Container (startY = y + 28)
            DrawMasteryTreeColumns(x, y + 28f, w, 280f);

            // Bottom Inspector (startY = y + 315)
            DrawNodeDetailInspector(x, y + 315f, w, h - 315f);
        }

        private void DrawNoClassSelectedView(float x, float y, float w, float h)
        {
            GUI.skin.label.fontSize = 18;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = new Color(1.0f, 0.85f, 0.2f);
            GUI.Label(new Rect(x, y + h * 0.35f, w, 30), "NENHUMA CLASSE SELECIONADA");

            GUI.skin.label.fontSize = 13;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + h * 0.45f, w, 24), "Interaja com uma arma nas Ruínas para escolher sua classe e desbloquear Maestrias!");
        }

        private void DrawMasteryTreeColumns(float containerX, float containerY, float containerW, float containerH)
        {
            ClassDefinition def = ProgressionManager.Instance.GetActiveClassDefinition();
            if (def == null) return;

            MasteryPath[] paths = new MasteryPath[] { MasteryPath.Path1, MasteryPath.Path2, MasteryPath.Path3 };
            string[] pathTitles = new string[]
            {
                $"{def.path1Name.ToUpper()} (MOBILIDADE)",
                $"{def.path2Name.ToUpper()} (DANO)",
                $"{def.path3Name.ToUpper()} (TANQUE)"
            };

            float columnWidth = 270f;
            float gap = 20f;
            float totalWidth = paths.Length * columnWidth + (paths.Length - 1) * gap;
            float startX = containerX + (containerW - totalWidth) / 2f;
            float startY = containerY;
            float nodeHeight = 72f;
            float nodeGap = 10f;

            for (int p = 0; p < paths.Length; p++)
            {
                MasteryPath path = paths[p];
                MasteryTier currentTier = ProgressionManager.Instance.GetTier(path);
                float colX = startX + p * (columnWidth + gap);

                // Column Header Box
                Rect headerRect = new Rect(colX, startY, columnWidth, 28);
                GUI.color = new Color(0.12f, 0.18f, 0.28f, 0.95f);
                GUI.DrawTexture(headerRect, Texture2D.whiteTexture);
                GUI.color = new Color(0.4f, 0.7f, 1.0f);
                GUI.Box(headerRect, "");

                GUI.skin.label.fontSize = 12;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.color = Color.cyan;
                GUI.Label(headerRect, pathTitles[p]);

                // Render N1, N2, N3 Nodes
                MasteryTier[] tiers = new MasteryTier[] { MasteryTier.N1, MasteryTier.N2, MasteryTier.N3 };
                for (int t = 0; t < tiers.Length; t++)
                {
                    MasteryTier tier = tiers[t];
                    ClassUpgradeDefinition upgrade = def.upgrades != null ? def.upgrades.Find(u => u.path == path && u.tier == tier) : null;
                    if (upgrade == null) continue;

                    float nodeY = startY + 34f + t * (nodeHeight + nodeGap);
                    Rect nodeRect = new Rect(colX, nodeY, columnWidth, nodeHeight);

                    bool isUnlocked = currentTier >= tier;
                    bool isAvailable = (int)tier == ((int)currentTier + 1);
                    bool isSelected = selectedUpgradeNode == upgrade;

                    Color bgColor;
                    Color borderColor;
                    string statusLabel;
                    Color labelColor;

                    if (isUnlocked)
                    {
                        bgColor = new Color(0.28f, 0.22f, 0.05f, 0.95f);
                        borderColor = new Color(1.0f, 0.85f, 0.2f);
                        statusLabel = "DESBLOQUEADO";
                        labelColor = new Color(1.0f, 0.88f, 0.3f);
                    }
                    else if (isAvailable)
                    {
                        bgColor = new Color(0.08f, 0.25f, 0.40f, 0.95f);
                        borderColor = new Color(0.35f, 0.85f, 1.0f);
                        statusLabel = ProgressionManager.Instance.PendingLevelUpCount > 0 ? "DISPONÍVEL" : "PRONTO";
                        labelColor = new Color(0.4f, 0.9f, 1.0f);
                    }
                    else
                    {
                        bgColor = new Color(0.08f, 0.09f, 0.12f, 0.8f);
                        borderColor = new Color(0.25f, 0.28f, 0.35f);
                        statusLabel = "BLOQUEADO";
                        labelColor = new Color(0.6f, 0.6f, 0.65f);
                    }

                    if (isSelected)
                    {
                        borderColor = Color.white;
                    }

                    GUI.color = bgColor;
                    GUI.DrawTexture(nodeRect, Texture2D.whiteTexture);
                    GUI.color = borderColor;
                    GUI.Box(nodeRect, "");

                    GUI.skin.label.fontSize = 13;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                    GUI.Label(new Rect(nodeRect.x + 10, nodeRect.y + 6, nodeRect.width - 20, 20), upgrade.upgradeTitle ?? $"Tier {tier}");

                    GUI.skin.label.fontSize = 10;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUI.color = labelColor;
                    GUI.Label(new Rect(nodeRect.x + 10, nodeRect.y + 26, nodeRect.width - 20, 16), statusLabel);

                    GUI.skin.label.fontSize = 10;
                    GUI.skin.label.fontStyle = FontStyle.Italic;
                    GUI.color = new Color(0.8f, 0.85f, 0.9f);
                    GUI.Label(new Rect(nodeRect.x + 10, nodeRect.y + 44, nodeRect.width - 20, 20), upgrade.visualPreviewText ?? "");

                    if (GUI.Button(nodeRect, "", GUIStyle.none))
                    {
                        selectedUpgradeNode = upgrade;
                    }

                    if (isAvailable && ProgressionManager.Instance.PendingLevelUpCount > 0)
                    {
                        GUI.color = new Color(0.18f, 0.82f, 0.35f);
                        if (GUI.Button(new Rect(nodeRect.x + nodeRect.width - 80, nodeRect.y + nodeRect.height - 28, 72, 22), "LIBERAR"))
                        {
                            selectedUpgradeNode = upgrade;
                            ProgressionManager.Instance.SelectUpgrade(upgrade);
                            RefreshMastery();
                        }
                    }
                }
            }

            GUI.color = Color.white;
        }

        private void DrawNodeDetailInspector(float x, float y, float w, float h)
        {
            Rect panelRect = new Rect(x, y, w, h);

            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.25f, 0.32f, 0.45f, 0.7f);
            GUI.Box(panelRect, "");

            if (selectedUpgradeNode == null)
            {
                GUI.skin.label.fontSize = 12;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                GUI.Label(panelRect, "Clique em uma melhoria acima para ver os detalhes e evoluir.");
                return;
            }

            MasteryPath path = selectedUpgradeNode.path;
            MasteryTier currentTier = ProgressionManager.Instance.GetTier(path);
            bool isUnlocked = currentTier >= selectedUpgradeNode.tier;
            bool isAvailable = (int)selectedUpgradeNode.tier == ((int)currentTier + 1);

            GUI.skin.label.fontSize = 14;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.color = new Color(1.0f, 0.85f, 0.25f);
            GUI.Label(new Rect(panelRect.x + 14, panelRect.y + 8, 480, 22), selectedUpgradeNode.upgradeTitle ?? "");

            GUI.skin.label.fontSize = 11;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = isUnlocked ? Color.gold : (isAvailable ? Color.cyan : Color.gray);
            string stateText = isUnlocked ? "[✓ DESBLOQUEADO]" : (isAvailable ? "[DISPONÍVEL PARA EVOLUÇÃO]" : "[🔒 REQUER TIER ANTERIOR]");
            GUI.Label(new Rect(panelRect.x + 14, panelRect.y + 30, 480, 18), $"{selectedUpgradeNode.path} • Tier {selectedUpgradeNode.tier}   {stateText}");

            GUI.skin.label.fontSize = 11;
            GUI.color = Color.white;
            GUI.Label(new Rect(panelRect.x + 14, panelRect.y + 50, w - 210, 36), selectedUpgradeNode.description ?? "");

            float btnW = 180f;
            float btnH = 40f;
            float btnX = panelRect.x + panelRect.width - btnW - 14f;
            float btnY = panelRect.y + (panelRect.height - btnH) * 0.5f;

            if (isUnlocked)
            {
                GUI.color = new Color(0.2f, 0.6f, 0.3f, 0.8f);
                GUI.Box(new Rect(btnX, btnY, btnW, btnH), "✓ ADQUIRIDO");
            }
            else if (isAvailable)
            {
                if (ProgressionManager.Instance.PendingLevelUpCount > 0)
                {
                    GUI.color = new Color(0.15f, 0.85f, 0.35f);
                    if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "APRENDER (-1 PONTO)"))
                    {
                        ProgressionManager.Instance.SelectUpgrade(selectedUpgradeNode);
                        RefreshMastery();
                    }
                }
                else
                {
                    GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    GUI.Box(new Rect(btnX, btnY, btnW, btnH), "REQUER PONTO\n(Suba de Nível)");
                }
            }
            else
            {
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
                GUI.Box(new Rect(btnX, btnY, btnW, btnH), "🔒 BLOQUEADO");
            }

            GUI.color = Color.white;
        }
    }
}
