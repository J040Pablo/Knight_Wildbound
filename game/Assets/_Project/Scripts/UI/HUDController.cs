using UnityEngine;
using Roguelite.Player;
using Roguelite.Enemy;
using Roguelite.Wave;
using Roguelite.Core;
using Roguelite.Progression;

namespace Roguelite.UI
{
    public class HUDController : MonoBehaviour
    {
        private PlayerStats playerStats;
        private PlayerCombat playerCombat;
        private InteractionSystem interactionSystem;
        private RunManager runManager;
        private HollowTreeBossAI activeBoss;
        private MountSystem activeMount;

        private Texture2D reticleTexture;

        private void OnEnable()
        {
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnXPChanged += HandleXPChanged;
                ProgressionManager.Instance.OnLevelChanged += HandleLevelChanged;
                ProgressionManager.Instance.OnMasteryUnlocked += HandleMasteryUnlocked;
            }
        }

        private void OnDisable()
        {
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnXPChanged -= HandleXPChanged;
                ProgressionManager.Instance.OnLevelChanged -= HandleLevelChanged;
                ProgressionManager.Instance.OnMasteryUnlocked -= HandleMasteryUnlocked;
            }
        }

        private void HandleXPChanged(int currentXP, int targetXP) { }
        private void HandleLevelChanged(int level) { }
        private void HandleMasteryUnlocked(MasteryPath path, MasteryTier tier) { }

        private void Start()
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
            playerCombat = FindFirstObjectByType<PlayerCombat>();
            interactionSystem = FindFirstObjectByType<InteractionSystem>();
            runManager = FindFirstObjectByType<RunManager>();

            CreateReticleTexture();
        }

        private void CreateReticleTexture()
        {
            int size = 16;
            reticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            reticleTexture.filterMode = FilterMode.Bilinear;
            reticleTexture.wrapMode = TextureWrapMode.Clamp;

            float center = size / 2f;
            float outerRadius = 4f;
            float innerRadius = 2.2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));

                    if (dist <= innerRadius)
                    {
                        float alpha = Mathf.Clamp01((innerRadius - dist) + 0.5f);
                        reticleTexture.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
                    }
                    else if (dist <= outerRadius)
                    {
                        float alpha = Mathf.Clamp01((outerRadius - dist) + 0.5f);
                        reticleTexture.SetPixel(x, y, new Color(0.0f, 0.0f, 0.0f, alpha));
                    }
                    else
                    {
                        reticleTexture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            reticleTexture.Apply();
        }

        private void Update()
        {
            if (activeBoss == null)
            {
                activeBoss = FindFirstObjectByType<HollowTreeBossAI>();
            }

            if (activeMount == null)
            {
                activeMount = FindFirstObjectByType<MountSystem>();
            }

            if (interactionSystem == null)
            {
                interactionSystem = FindFirstObjectByType<InteractionSystem>();
            }
        }

        private void OnGUI()
        {
            if (runManager != null && runManager.State == RunState.GameOver) return;

            DrawHUD();
            DrawInteractionPrompt();
            DrawCenterReticle();
        }

        private void DrawInteractionPrompt()
        {
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;
            if (interactionSystem == null || string.IsNullOrEmpty(interactionSystem.CurrentPrompt)) return;

            float pWidth = 340f;
            float pHeight = 36f;
            float posX = (Screen.width - pWidth) * 0.5f;
            float posY = Screen.height - 180f;

            Rect pRect = new Rect(posX, posY, pWidth, pHeight);

            GUI.color = new Color(0.05f, 0.08f, 0.12f, 0.88f);
            GUI.DrawTexture(pRect, Texture2D.whiteTexture);

            GUI.color = new Color(0.95f, 0.75f, 0.25f, 0.9f);
            GUI.Box(pRect, "");

            GUI.skin.label.fontSize = 12;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            GUI.Label(pRect, interactionSystem.CurrentPrompt);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void DrawCenterReticle()
        {
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;

            if (reticleTexture == null)
            {
                CreateReticleTexture();
            }

            float drawSize = 10f;
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            Rect reticleRect = new Rect(centerX - drawSize / 2f, centerY - drawSize / 2f, drawSize, drawSize);
            GUI.color = Color.white;
            GUI.DrawTexture(reticleRect, reticleTexture);
        }

        private void DrawHUD()
        {
            // ==========================================
            // BOTTOM-LEFT: Minimal Action-RPG HUD
            // ==========================================
            float hudWidth = 240f;
            float hudHeight = 78f;
            float posX = 20f;
            float posY = Screen.height - hudHeight - 20f;

            Rect bgRect = new Rect(posX, posY, hudWidth, hudHeight);

            // Semi-transparent dark sleek background container
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.82f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Thin accent border
            GUI.color = new Color(0.2f, 0.25f, 0.35f, 0.5f);
            GUI.Box(bgRect, "");

            if (playerStats != null)
            {
                int level = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : playerStats.Level;
                ClassType currentClass = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentClass : ClassType.Knight;

                string masteryStatusText = "";
                if (ProgressionManager.Instance != null && currentClass != ClassType.None)
                {
                    ClassDefinition def = ProgressionManager.Instance.GetActiveClassDefinition();
                    string p1 = def != null ? def.GetPathAbbrev(MasteryPath.Path1) : "P1";
                    string p2 = def != null ? def.GetPathAbbrev(MasteryPath.Path2) : "P2";
                    string p3 = def != null ? def.GetPathAbbrev(MasteryPath.Path3) : "P3";

                    string t1 = GetRomanTier(ProgressionManager.Instance.GetTier(MasteryPath.Path1));
                    string t2 = GetRomanTier(ProgressionManager.Instance.GetTier(MasteryPath.Path2));
                    string t3 = GetRomanTier(ProgressionManager.Instance.GetTier(MasteryPath.Path3));

                    masteryStatusText = $"{p1} {t1}  |  {p2} {t2}  |  {p3} {t3}";
                }
                else
                {
                    masteryStatusText = "SELECT WEAPON IN RUINS";
                }

                // Level & Mastery Header
                GUI.skin.label.fontSize = 11;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.color = new Color(0.95f, 0.82f, 0.35f);
                GUI.Label(new Rect(posX + 10, posY + 5, hudWidth - 20, 16), $"LV {level}   •   {masteryStatusText}");

                // 1. Thin HP Bar (Height: 10px)
                float hpRatio = playerStats.MaxHP > 0 ? playerStats.CurrentHP / playerStats.MaxHP : 0;
                DrawThinBar(new Rect(posX + 10, posY + 25, hudWidth - 20, 10), hpRatio, new Color(0.85f, 0.22f, 0.22f), $"{Mathf.CeilToInt(playerStats.CurrentHP)} / {Mathf.CeilToInt(playerStats.MaxHP)}");

                // 2. Thin Stamina Bar (Height: 8px)
                float stamRatio = playerStats.MaxStamina > 0 ? playerStats.CurrentStamina / playerStats.MaxStamina : 0;
                DrawThinBar(new Rect(posX + 10, posY + 40, hudWidth - 20, 8), stamRatio, new Color(0.18f, 0.72f, 0.9f), "");

                // 3. Thin XP Bar (Height: 5px)
                int curXP = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevelXP : playerStats.CurrentXP;
                int reqXP = ProgressionManager.Instance != null ? ProgressionManager.Instance.GetXPRequired(level) : playerStats.XPToNextLevel;
                float xpRatio = reqXP > 0 ? (float)curXP / reqXP : 0;
                DrawThinBar(new Rect(posX + 10, posY + 54, hudWidth - 20, 5), xpRatio, new Color(0.92f, 0.75f, 0.15f), "");

                // 4. Notification Banner when Level Up / Mastery Points are available
                int pendingPoints = ProgressionManager.Instance != null ? ProgressionManager.Instance.PendingLevelUpCount : 0;
                if (pendingPoints > 0)
                {
                    float notifWidth = 240f;
                    float notifHeight = 36f;
                    float notifY = posY - notifHeight - 8f;
                    Rect notifRect = new Rect(posX, notifY, notifWidth, notifHeight);

                    GUI.color = new Color(0.08f, 0.28f, 0.45f, 0.92f);
                    GUI.DrawTexture(notifRect, Texture2D.whiteTexture);
                    GUI.color = new Color(0.4f, 0.85f, 1.0f);
                    GUI.Box(notifRect, "");

                    GUI.skin.label.fontSize = 11;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(notifRect.x, notifRect.y + 2, notifWidth, 16), $"↑ LEVEL {level} READY  •  Points: {pendingPoints}");
                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(notifRect.x, notifRect.y + 18, notifWidth, 15), "Press [Q] to Open Masteries");
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                }
            }

            // ==========================================
            // TOP-RIGHT: Minimal Timer & Location Box
            // ==========================================
            float trWidth = 140f;
            float trHeight = 40f;
            Rect trRect = new Rect(Screen.width - trWidth - 20f, 15f, trWidth, trHeight);

            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.65f);
            GUI.DrawTexture(trRect, Texture2D.whiteTexture);

            GUI.skin.label.fontSize = 11;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = Color.white;

            int mins = runManager != null ? (int)(runManager.RunTimeSeconds / 60) : 0;
            int secs = runManager != null ? (int)(runManager.RunTimeSeconds % 60) : 0;
            string regionName = Environment.BiomeRegionTrigger.CurrentRegionName;
            if (string.IsNullOrEmpty(regionName)) regionName = "Forest Route";

            GUI.Label(new Rect(trRect.x + 8, trRect.y + 4, trWidth - 16, 16), $"⏱️ {mins:D2}:{secs:D2}");
            GUI.Label(new Rect(trRect.x + 8, trRect.y + 20, trWidth - 16, 16), $"📍 {regionName}");

            // ==========================================
            // TOP-CENTER: Boss Health Bar (Only when active)
            // ==========================================
            if (activeBoss != null && !activeBoss.IsDead && Environment.BossActivationTrigger.IsBossActivated)
            {
                float bossHpRatio = activeBoss.MaxHP > 0 ? activeBoss.CurrentHP / activeBoss.MaxHP : 0;
                float barWidth = Mathf.Min(500, Screen.width - 100);
                Rect bossRect = new Rect((Screen.width - barWidth) / 2, 20, barWidth, 24);

                Color bossColor = activeBoss.IsPhase2 ? new Color(0.95f, 0.15f, 0.25f) : new Color(0.85f, 0.45f, 0.1f);
                string phaseText = activeBoss.IsPhase2 ? "PHASE 2" : "PHASE 1";
                DrawThinBar(bossRect, bossHpRatio, bossColor, $"👑 THE HOLLOW TREE [{phaseText}] — {Mathf.CeilToInt(activeBoss.CurrentHP)} / {Mathf.CeilToInt(activeBoss.MaxHP)}");
            }
        }

        private string GetRomanTier(MasteryTier tier)
        {
            switch (tier)
            {
                case MasteryTier.N1: return "I";
                case MasteryTier.N2: return "II";
                case MasteryTier.N3: return "III";
                default: return "0";
            }
        }

        private void DrawThinBar(Rect rect, float fillRatio, Color fillColor, string labelText)
        {
            GUI.color = new Color(0.1f, 0.12f, 0.15f, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = fillColor;
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fillRatio), rect.height);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

            if (!string.IsNullOrEmpty(labelText))
            {
                GUI.color = Color.white;
                GUI.skin.label.fontSize = 9;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(rect, labelText);
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
        }
    }
}
