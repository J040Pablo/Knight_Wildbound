using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Progression;
using Roguelite.Core;
using Roguelite.Core.StateMachine;

namespace Roguelite.UI
{
    public class MasteryScreenUI : MonoBehaviour
    {
        public static bool IsAnyMenuOpen { get; private set; } = false;

        private bool isOpen = false;
        private ClassUpgradeDefinition selectedUpgradeNode = null;

        public bool IsOpen => isOpen;

        private void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            isOpen = (newState == GameState.Mastery);
            IsAnyMenuOpen = isOpen;
            if (isOpen)
            {
                Refresh();
            }
        }

        public void Open()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.Mastery);
            }
            else
            {
                isOpen = true;
                IsAnyMenuOpen = true;
            }
        }

        public void Close()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
            }
            else
            {
                isOpen = false;
                IsAnyMenuOpen = false;
            }
        }

        public void Toggle()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ToggleState(GameState.Mastery);
            }
            else
            {
                if (isOpen) Close();
                else Open();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Toggle();
            }
            else if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Refresh()
        {
            // Auto-select first available or unlocked node if selection is null
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

            // Fullscreen Semi-transparent Dark Backdrop
            GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (ProgressionManager.Instance == null || ProgressionManager.Instance.CurrentClass == ClassType.None)
            {
                DrawNoClassSelectedView();
                return;
            }

            DrawHeader();
            DrawMasteryTreeColumns();
            DrawNodeDetailInspector();
        }

        private void DrawNoClassSelectedView()
        {
            GUI.skin.label.fontSize = 22;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = new Color(1.0f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 40), "⚠️ NO CLASS EQUIPPED YET");

            GUI.skin.label.fontSize = 14;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.47f, Screen.width, 30), "Interact with a weapon in the Ruins to select your class & unlock Masteries!");

            if (GUI.Button(new Rect((Screen.width - 160) / 2f, Screen.height * 0.56f, 160, 40), "CLOSE [Q]"))
            {
                Close();
            }
        }

        private void DrawHeader()
        {
            ClassDefinition def = ProgressionManager.Instance.GetActiveClassDefinition();
            string className = def != null ? def.className.ToUpper() : ProgressionManager.Instance.CurrentClass.ToString().ToUpper();
            int level = ProgressionManager.Instance.CurrentLevel;
            int points = ProgressionManager.Instance.PendingLevelUpCount;

            // Title
            GUI.skin.label.fontSize = 26;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = new Color(1.0f, 0.88f, 0.3f);
            GUI.Label(new Rect(0, 20, Screen.width, 36), $"⚔️ {className} MASTERY TREE ⚔️");

            // Subheader Pill
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = new Color(0.85f, 0.90f, 0.95f);
            GUI.Label(new Rect(0, 58, Screen.width, 24), $"LEVEL {level}   |   ⭐ Mastery Points: {points}   |   Press [Q], [ESC] or [X] to Close");

            // Close [X] Button Top Right
            GUI.color = new Color(0.85f, 0.25f, 0.25f);
            if (GUI.Button(new Rect(Screen.width - 55, 20, 35, 35), "X"))
            {
                Close();
            }
            GUI.color = Color.white;
        }

        private void DrawMasteryTreeColumns()
        {
            ClassDefinition def = ProgressionManager.Instance.GetActiveClassDefinition();
            if (def == null) return;

            MasteryPath[] paths = new MasteryPath[] { MasteryPath.Path1, MasteryPath.Path2, MasteryPath.Path3 };
            string[] pathTitles = new string[]
            {
                $"{def.path1Name.ToUpper()} (MOBILITY)",
                $"{def.path2Name.ToUpper()} (DAMAGE)",
                $"{def.path3Name.ToUpper()} (TANK)"
            };

            float columnWidth = 270f;
            float gap = 25f;
            float totalWidth = paths.Length * columnWidth + (paths.Length - 1) * gap;
            float startX = (Screen.width - totalWidth) / 2f;
            float startY = 100f;
            float nodeHeight = 78f;
            float nodeGap = 16f;

            for (int p = 0; p < paths.Length; p++)
            {
                MasteryPath path = paths[p];
                MasteryTier currentTier = ProgressionManager.Instance.GetTier(path);
                float colX = startX + p * (columnWidth + gap);

                // Column Header Box
                Rect headerRect = new Rect(colX, startY, columnWidth, 32);
                GUI.color = new Color(0.12f, 0.18f, 0.28f, 0.95f);
                GUI.DrawTexture(headerRect, Texture2D.whiteTexture);
                GUI.color = new Color(0.4f, 0.7f, 1.0f);
                GUI.Box(headerRect, "");

                GUI.skin.label.fontSize = 13;
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

                    float nodeY = startY + 42f + t * (nodeHeight + nodeGap);
                    Rect nodeRect = new Rect(colX, nodeY, columnWidth, nodeHeight);

                    bool isUnlocked = currentTier >= tier;
                    bool isAvailable = (int)tier == ((int)currentTier + 1);
                    bool isSelected = selectedUpgradeNode == upgrade;

                    // Background & Border colors
                    Color bgColor;
                    Color borderColor;
                    string statusLabel;
                    Color labelColor;

                    if (isUnlocked)
                    {
                        bgColor = new Color(0.28f, 0.22f, 0.05f, 0.95f); // Gold Metallic
                        borderColor = new Color(1.0f, 0.85f, 0.2f);
                        statusLabel = "★ UNLOCKED";
                        labelColor = new Color(1.0f, 0.88f, 0.3f);
                    }
                    else if (isAvailable)
                    {
                        bgColor = new Color(0.08f, 0.25f, 0.40f, 0.95f); // Blue Glow
                        borderColor = new Color(0.35f, 0.85f, 1.0f);
                        statusLabel = ProgressionManager.Instance.PendingLevelUpCount > 0 ? "⚡ READY TO UNLOCK" : "AVAILABLE";
                        labelColor = new Color(0.4f, 0.9f, 1.0f);
                    }
                    else
                    {
                        bgColor = new Color(0.08f, 0.09f, 0.12f, 0.8f); // Locked Gray
                        borderColor = new Color(0.25f, 0.28f, 0.35f);
                        statusLabel = "🔒 LOCKED";
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

                    // Node Title
                    GUI.skin.label.fontSize = 14;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                    GUI.Label(new Rect(nodeRect.x + 10, nodeRect.y + 8, nodeRect.width - 20, 22), upgrade.upgradeTitle ?? $"Tier {tier}");

                    // Status Badge
                    GUI.skin.label.fontSize = 11;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUI.color = labelColor;
                    GUI.Label(new Rect(nodeRect.x + 10, nodeRect.y + 30, nodeRect.width - 20, 18), statusLabel);

                    // Short preview text
                    GUI.skin.label.fontSize = 10;
                    GUI.skin.label.fontStyle = FontStyle.Italic;
                    GUI.color = new Color(0.8f, 0.85f, 0.9f);
                    GUI.Label(new Rect(nodeRect.x + 10, nodeRect.y + 50, nodeRect.width - 20, 20), upgrade.visualPreviewText ?? "");

                    // Make node clickable to select
                    if (GUI.Button(nodeRect, "", GUIStyle.none))
                    {
                        selectedUpgradeNode = upgrade;
                    }

                    // Direct Quick Unlock Button on node if available and points > 0
                    if (isAvailable && ProgressionManager.Instance.PendingLevelUpCount > 0)
                    {
                        GUI.color = new Color(0.18f, 0.82f, 0.35f);
                        if (GUI.Button(new Rect(nodeRect.x + nodeRect.width - 85, nodeRect.y + nodeRect.height - 32, 75, 24), "UNLOCK"))
                        {
                            selectedUpgradeNode = upgrade;
                            ProgressionManager.Instance.SelectUpgrade(upgrade);
                            Debug.Log($"[Mastery] Unlock Selected: {upgrade.upgradeTitle}");

                            if (ProgressionManager.Instance.PendingLevelUpCount > 0)
                            {
                                Refresh();
                            }
                            else
                            {
                                Close();
                            }
                        }
                    }
                }
            }

            GUI.color = Color.white;
        }

        private void DrawNodeDetailInspector()
        {
            float panelWidth = 860f;
            float panelHeight = 150f;
            float startX = (Screen.width - panelWidth) / 2f;
            float startY = Screen.height - panelHeight - 25f;

            Rect panelRect = new Rect(startX, startY, panelWidth, panelHeight);

            GUI.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.25f, 0.45f, 0.75f, 0.6f);
            GUI.Box(panelRect, "");

            if (selectedUpgradeNode == null)
            {
                GUI.skin.label.fontSize = 14;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUI.Label(panelRect, "Click any node above to inspect details and unlock upgrades.");
                return;
            }

            MasteryPath path = selectedUpgradeNode.path;
            MasteryTier currentTier = ProgressionManager.Instance.GetTier(path);
            bool isUnlocked = currentTier >= selectedUpgradeNode.tier;
            bool isAvailable = (int)selectedUpgradeNode.tier == ((int)currentTier + 1);

            // Left side: Title & Badge
            GUI.skin.label.fontSize = 17;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.color = new Color(1.0f, 0.85f, 0.25f);
            GUI.Label(new Rect(panelRect.x + 18, panelRect.y + 10, 480, 24), selectedUpgradeNode.upgradeTitle ?? "");

            GUI.skin.label.fontSize = 11;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = isUnlocked ? Color.gold : (isAvailable ? Color.cyan : Color.gray);
            string stateText = isUnlocked ? "[✓ UNLOCKED]" : (isAvailable ? "[⚡ AVAILABLE FOR UNLOCK]" : "[🔒 REQUIRES PREVIOUS TIER]");
            GUI.Label(new Rect(panelRect.x + 18, panelRect.y + 34, 480, 18), $"{selectedUpgradeNode.path} — Tier {selectedUpgradeNode.tier}   •   {stateText}");

            // Middle side: Structured Attack & Stat Breakdown
            GUI.skin.label.fontSize = 11;
            GUI.color = Color.white;

            string bName = !string.IsNullOrEmpty(selectedUpgradeNode.basicAttackName) ? selectedUpgradeNode.basicAttackName : (selectedUpgradeNode.basicAttack != null ? selectedUpgradeNode.basicAttack.attackName : "Standard");
            string cName = !string.IsNullOrEmpty(selectedUpgradeNode.chargedAttackName) ? selectedUpgradeNode.chargedAttackName : (selectedUpgradeNode.chargedAttack != null ? selectedUpgradeNode.chargedAttack.attackName : "Standard");
            string passiveText = !string.IsNullOrEmpty(selectedUpgradeNode.specialPassiveName) ? $"   •   ✨ Special: {selectedUpgradeNode.specialPassiveName}" : "";

            GUI.Label(new Rect(panelRect.x + 18, panelRect.y + 54, 580, 18), $"⚔️ Basic Attack: {bName}   |   💥 Charged Attack: {cName}{passiveText}");

            // Stat & Visual Info
            string speedText = selectedUpgradeNode.moveSpeedBonusPercent > 0 ? $"+{selectedUpgradeNode.moveSpeedBonusPercent * 100:F0}% Speed  " : "";
            string dmgText = selectedUpgradeNode.attackDamageBonusPercent > 0 ? $"+{selectedUpgradeNode.attackDamageBonusPercent * 100:F0}% Damage  " : "";
            string hpText = selectedUpgradeNode.maxHpBonusFlat > 0 ? $"+{selectedUpgradeNode.maxHpBonusFlat:F0} Max HP  " : "";
            string statSummary = $"{speedText}{dmgText}{hpText}".Trim();
            if (string.IsNullOrEmpty(statSummary)) statSummary = "Mastery Specialization";

            GUI.color = new Color(0.85f, 0.92f, 1.0f);
            GUI.Label(new Rect(panelRect.x + 18, panelRect.y + 74, 580, 18), $"📈 Stats: {statSummary}");

            GUI.color = new Color(0.95f, 0.85f, 0.4f);
            GUI.Label(new Rect(panelRect.x + 18, panelRect.y + 94, 580, 18), $"🎨 {selectedUpgradeNode.visualPreviewText ?? ""}");

            GUI.color = new Color(0.75f, 0.8f, 0.85f);
            GUI.skin.label.fontSize = 10;
            GUI.Label(new Rect(panelRect.x + 18, panelRect.y + 114, 580, 30), selectedUpgradeNode.description ?? "");

            // Right side: Unlock Action Button
            float btnX = panelRect.x + panelRect.width - 210f;
            float btnY = panelRect.y + 45f;

            if (isUnlocked)
            {
                GUI.color = new Color(0.2f, 0.6f, 0.3f, 0.8f);
                GUI.Box(new Rect(btnX, btnY, 190, 48), "✓ ALREADY UNLOCKED");
            }
            else if (isAvailable)
            {
                if (ProgressionManager.Instance.PendingLevelUpCount > 0)
                {
                    GUI.color = new Color(0.15f, 0.85f, 0.35f);
                    if (GUI.Button(new Rect(btnX, btnY, 190, 48), "UNLOCK MASTERY (-1 PT)"))
                    {
                        ProgressionManager.Instance.SelectUpgrade(selectedUpgradeNode);
                        Debug.Log($"[Mastery] Unlock Selected: {selectedUpgradeNode.upgradeTitle}");

                        if (ProgressionManager.Instance.PendingLevelUpCount > 0)
                        {
                            Refresh();
                        }
                        else
                        {
                            Close();
                        }
                    }
                }
                else
                {
                    GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    GUI.Box(new Rect(btnX, btnY, 190, 48), "NEED MASTERY POINT\n(Level Up to Earn)");
                }
            }
            else
            {
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
                GUI.Box(new Rect(btnX, btnY, 190, 48), "🔒 LOCKED\n(Unlock Previous Tier)");
            }

            GUI.color = Color.white;
        }
    }
}
