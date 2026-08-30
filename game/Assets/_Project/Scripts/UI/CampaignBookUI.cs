using System.Collections.Generic;
using UnityEngine;
using Roguelite.Player;
using Roguelite.Progression;
using Roguelite.Items;
using Roguelite.Core;

namespace Roguelite.UI
{
    /// <summary>
    /// Ancient Journal UI opened with [J].
    /// Features an old fantasy book layout with 2 bookmark tabs:
    /// Tab 1: Character Stats (Stardew Valley style).
    /// Tab 2: Campaign Relics (Shows ONLY discovered campaign relics, no future spoilers).
    /// </summary>
    public class CampaignBookUI : MonoBehaviour
    {
        private static CampaignBookUI instance;
        private static bool applicationIsQuitting = false;

        public static CampaignBookUI Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<CampaignBookUI>();
                    if (instance == null && !applicationIsQuitting)
                    {
                        GameObject go = new GameObject("CampaignBookUI");
                        instance = go.AddComponent<CampaignBookUI>();
                    }
                }
                return instance;
            }
        }

        public bool isOpen = false;
        private int selectedTab = 0; // 0: Character Stats, 1: Campaign Relics
        private readonly string[] tabNames = new string[] { "📜 Character Overview", "🏆 Campaign Relics" };

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
            if (Input.GetKeyDown(KeyCode.J) || (isOpen && Input.GetKeyDown(KeyCode.Escape)))
            {
                ToggleJournal();
            }
        }

        public void ToggleJournal()
        {
            isOpen = !isOpen;

            if (isOpen)
            {
                Debug.Log("[Ancient Journal] Toggle Open");
                if (InputStateManager.Instance != null)
                {
                    InputStateManager.Instance.SetUIMode();
                }
            }
            else
            {
                Debug.Log("[Ancient Journal] Toggle Close");
                if (InputStateManager.Instance != null)
                {
                    InputStateManager.Instance.SetGameplayMode();
                }
            }

            Time.timeScale = isOpen ? 0f : 1f;
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            GUI.depth = -25;
            GUI.skin.box.alignment = TextAnchor.MiddleCenter;

            // Fantasy Leather Book Dimensions
            float bookW = 820f;
            float bookH = 520f;
            float bookX = (Screen.width - bookW) * 0.5f;
            float bookY = (Screen.height - bookH) * 0.5f;

            // 1. Outer Dark Worn Leather Cover Background
            GUI.color = new Color(0.12f, 0.08f, 0.05f, 0.96f);
            GUI.DrawTexture(new Rect(bookX, bookY, bookW, bookH), Texture2D.whiteTexture);

            // Gold Embossed Book Border
            GUI.color = new Color(0.75f, 0.58f, 0.25f, 0.9f);
            GUI.Box(new Rect(bookX, bookY, bookW, bookH), "");

            // 2. Inner Parchment Paper Background
            float parchX = bookX + 12f;
            float parchY = bookY + 12f;
            float parchW = bookW - 24f;
            float parchH = bookH - 24f;

            GUI.color = new Color(0.92f, 0.86f, 0.72f, 0.98f);
            GUI.DrawTexture(new Rect(parchX, parchY, parchW, parchH), Texture2D.whiteTexture);

            // Center Book Binding Line
            float centerX = bookX + bookW * 0.5f;
            GUI.color = new Color(0.35f, 0.25f, 0.15f, 0.4f);
            GUI.DrawTexture(new Rect(centerX - 1, parchY, 2, parchH), Texture2D.whiteTexture);

            // Book Title Header
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = new Color(0.25f, 0.15f, 0.08f);
            GUI.Label(new Rect(bookX, bookY + 18, bookW, 26), "📖 ANCIENT JOURNAL & CAMPAIGN PROGRESS", headerStyle);

            GUI.color = new Color(0.4f, 0.3f, 0.2f);
            GUI.Label(new Rect(bookX + bookW - 210, bookY + 18, 190, 22), "Press [J] or [ESC] to Close");

            // Bookmark Navigation Tabs (Top)
            float tabW = 340f;
            float tabX = (bookW - tabW) * 0.5f + bookX;
            selectedTab = GUI.Toolbar(new Rect(tabX, bookY + 48, tabW, 32), selectedTab, tabNames);

            // Render Active Page Content
            float pageY = bookY + 90f;
            float pageH = bookH - 105f;

            if (selectedTab == 0)
            {
                DrawCharacterStatsPage(parchX + 15, pageY, parchW - 30, pageH);
            }
            else
            {
                DrawCampaignRelicsPage(parchX + 15, pageY, parchW - 30, pageH);
            }
        }

        private void DrawCharacterStatsPage(float x, float y, float w, float h)
        {
            float halfW = (w - 20f) * 0.5f;
            float leftX = x;
            float rightX = x + halfW + 20f;

            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            ClassType pClass = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentClass : ClassType.Knight;
            int level = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : (stats != null ? stats.Level : 1);

            // -------------------------------------------------------------
            // LEFT PAGE: Character Identity & Mastery Overview
            // -------------------------------------------------------------
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.3f, 0.18f, 0.08f);

            GUI.Label(new Rect(leftX, y, halfW, 24), "🛡️ ADVENTURER PROFILE", titleStyle);

            string classTitle = pClass == ClassType.None ? "UNBOUND KNIGHT" : $"{pClass.ToString().ToUpper()} HERO";
            GUI.Label(new Rect(leftX, y + 30, halfW, 22), $"Title: {classTitle}");
            GUI.Label(new Rect(leftX, y + 50, halfW, 22), $"Level: {level}");

            // Level XP Progress
            int curXP = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevelXP : (stats != null ? stats.CurrentXP : 0);
            int reqXP = ProgressionManager.Instance != null ? ProgressionManager.Instance.GetXPRequired(level) : (stats != null ? stats.XPToNextLevel : 100);
            float xpRatio = reqXP > 0 ? (float)curXP / reqXP : 0;

            GUI.Label(new Rect(leftX, y + 75, halfW, 20), $"Experience ({curXP} / {reqXP} XP):");
            DrawParchmentBar(new Rect(leftX, y + 96, halfW - 20, 12), xpRatio, new Color(0.85f, 0.65f, 0.15f), "");

            // Class Mastery Summary
            GUI.Label(new Rect(leftX, y + 130, halfW, 24), "⚡ MASTERY PATHWAYS", titleStyle);

            if (ProgressionManager.Instance != null && pClass != ClassType.None)
            {
                ClassDefinition def = ProgressionManager.Instance.GetActiveClassDefinition();
                string p1 = def != null ? def.path1Name : "Path I";
                string p2 = def != null ? def.path2Name : "Path II";
                string p3 = def != null ? def.path3Name : "Path III";

                string t1 = ProgressionManager.Instance.GetTier(MasteryPath.Path1).ToString();
                string t2 = ProgressionManager.Instance.GetTier(MasteryPath.Path2).ToString();
                string t3 = ProgressionManager.Instance.GetTier(MasteryPath.Path3).ToString();

                GUI.Label(new Rect(leftX, y + 160, halfW, 20), $"• {p1}: Tier {t1}");
                GUI.Label(new Rect(leftX, y + 182, halfW, 20), $"• {p2}: Tier {t2}");
                GUI.Label(new Rect(leftX, y + 204, halfW, 20), $"• {p3}: Tier {t3}");
            }
            else
            {
                GUI.Label(new Rect(leftX, y + 160, halfW, 20), "• Select weapon in Ruins arena to unlock masteries.");
            }

            // -------------------------------------------------------------
            // RIGHT PAGE: Stardew-Style Detailed Combat Stats
            // -------------------------------------------------------------
            GUI.Label(new Rect(rightX, y, halfW, 24), "⚔️ COMBAT ATTRIBUTES", titleStyle);

            if (stats != null)
            {
                bool hasTreeSeed = RelicManager.Instance != null && RelicManager.Instance.HasRelic("relic_tree_seed");
                string hpRelicText = hasTreeSeed ? " (+25 Relic)" : "";

                DrawStatRow(rightX, y + 35, halfW - 20, "Health (Max HP)", $"{stats.MaxHP:F0}{hpRelicText}", "❤️");
                DrawStatRow(rightX, y + 70, halfW - 20, "Attack Damage", $"{stats.FlatDamage:F0}", "⚔️");
                DrawStatRow(rightX, y + 105, halfW - 20, "Stamina", $"{stats.MaxStamina:F0}", "⚡");
                DrawStatRow(rightX, y + 140, halfW - 20, "Move Speed", $"{stats.MoveSpeedMultiplier * 100f:F0}%", "🏃");
            }
        }

        private void DrawStatRow(float x, float y, float w, string label, string value, string glyph)
        {
            GUI.color = new Color(0.82f, 0.76f, 0.62f, 0.6f);
            GUI.DrawTexture(new Rect(x, y, w, 28), Texture2D.whiteTexture);
            GUI.color = new Color(0.4f, 0.3f, 0.2f, 0.5f);
            GUI.Box(new Rect(x, y, w, 28), "");

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = new Color(0.2f, 0.12f, 0.05f);
            GUI.Label(new Rect(x + 10, y + 4, w - 80, 20), $"{glyph} {label}", labelStyle);

            GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            valueStyle.normal.textColor = new Color(0.55f, 0.15f, 0.1f);
            GUI.Label(new Rect(x + w - 110, y + 4, 100, 20), value, valueStyle);
        }

        private void DrawCampaignRelicsPage(float x, float y, float w, float h)
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.3f, 0.18f, 0.08f);

            GUI.Label(new Rect(x, y, w, 24), "🏆 DISCOVERED CAMPAIGN RELICS", titleStyle);

            GUI.skin.label.fontSize = 11;
            GUI.color = new Color(0.35f, 0.25f, 0.15f);
            GUI.Label(new Rect(x + 20, y + 26, w - 40, 22), "Relics are permanent campaign bonuses unlocked by defeating key bosses across biomes.");

            float relicY = y + 58f;
            List<string> collected = RelicManager.Instance != null ? RelicManager.Instance.GetCollectedRelicIds() : new List<string>();

            if (collected.Count == 0 && (RelicManager.Instance == null || !RelicManager.Instance.HasRelic("relic_tree_seed")))
            {
                GUI.color = new Color(0.45f, 0.35f, 0.25f);
                GUI.Label(new Rect(x + 20, relicY + 20, w - 40, 30), "📜 No campaign relics discovered yet.\nExplore the forest and defeat the Hollow Tree Boss to unlock progression relics!");
                return;
            }

            // Render DISCOVERED Relics ONLY (No spoilers for undiscovered relics!)
            if (RelicManager.Instance != null && RelicManager.Instance.HasRelic("relic_tree_seed"))
            {
                DrawRelicJournalCard(x + 20, relicY, w - 40, "Seed of the Ancient Tree", "Forest Biome Guardian Relic", "+25 Permanent Max HP", "🌱", "Forest Boss Defeated");
                relicY += 95f;
            }

            // Note: Future relics appear HERE only AFTER being discovered by player! Zero spoilers.
        }

        private void DrawRelicJournalCard(float x, float y, float w, string title, string sub, string power, string glyph, string badge)
        {
            // Parchment Box
            GUI.color = new Color(0.86f, 0.80f, 0.65f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, w, 80), Texture2D.whiteTexture);

            // Dark Emerald Accent Border
            GUI.color = new Color(0.12f, 0.45f, 0.22f, 0.85f);
            GUI.Box(new Rect(x, y, w, 80), "");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.08f, 0.42f, 0.18f);
            GUI.Label(new Rect(x + 12, y + 8, w - 160, 22), $"{glyph} {title}", titleStyle);

            GUIStyle badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            badgeStyle.normal.textColor = new Color(0.75f, 0.5f, 0.1f);
            GUI.Label(new Rect(x + w - 170, y + 8, 160, 20), badge, badgeStyle);

            GUI.skin.label.fontSize = 12;
            GUI.color = new Color(0.25f, 0.18f, 0.1f);
            GUI.Label(new Rect(x + 12, y + 32, w - 24, 20), $"{sub}");

            GUIStyle powerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            powerStyle.normal.textColor = new Color(0.1f, 0.5f, 0.25f);
            GUI.Label(new Rect(x + 12, y + 52, w - 24, 20), $"Permanent Bonus: {power}");
        }

        private void DrawParchmentBar(Rect rect, float fillRatio, Color fillColor, string labelText)
        {
            GUI.color = new Color(0.35f, 0.28f, 0.18f, 0.5f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = fillColor;
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fillRatio), rect.height);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        }
    }
}
