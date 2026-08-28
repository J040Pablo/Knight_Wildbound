using UnityEngine;
using Roguelite.Player;
using Roguelite.Enemy;
using Roguelite.Wave;
using Roguelite.Core;

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
            float outerRadius = 4f;   // Outer edge of black outline ring
            float innerRadius = 2.2f; // Outer edge of white circle center

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));

                    if (dist <= innerRadius)
                    {
                        // Solid white center dot with soft anti-aliased edge
                        float alpha = Mathf.Clamp01((innerRadius - dist) + 0.5f);
                        reticleTexture.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
                    }
                    else if (dist <= outerRadius)
                    {
                        // Thin black outline ring with anti-aliasing
                        float alpha = Mathf.Clamp01((outerRadius - dist) + 0.5f);
                        reticleTexture.SetPixel(x, y, new Color(0.0f, 0.0f, 0.0f, alpha));
                    }
                    else
                    {
                        // Fully transparent background
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
        }

        private void OnGUI()
        {
            if (runManager != null && runManager.State != RunState.InRun) return;

            DrawHUD();
            DrawCenterReticle();
        }

        private void DrawCenterReticle()
        {
            // Do not draw reticle if dialogue is active
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;

            if (reticleTexture == null)
            {
                CreateReticleTexture();
            }

            float drawSize = 10f; // Minimal, crisp 10px reticle diameter on screen
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
            float hudWidth = 230f;
            float hudHeight = 72f;
            float posX = 20f;
            float posY = Screen.height - hudHeight - 20f;

            Rect bgRect = new Rect(posX, posY, hudWidth, hudHeight);

            // Semi-transparent dark sleek background container
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.78f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Thin accent border
            GUI.color = new Color(0.2f, 0.25f, 0.35f, 0.5f);
            GUI.Box(bgRect, "");

            if (playerStats != null)
            {
                // Level & Class Title Header
                GUI.skin.label.fontSize = 12;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.color = new Color(0.95f, 0.82f, 0.35f);
                CharacterType cType = GameSessionManager.Instance != null ? GameSessionManager.Instance.SelectedCharacter : CharacterType.Knight;
                GUI.Label(new Rect(posX + 10, posY + 6, hudWidth - 20, 18), $"LEVEL {playerStats.Level}  •  {cType.ToString().ToUpper()}");

                // 1. Thin HP Bar (Height: 10px)
                float hpRatio = playerStats.MaxHP > 0 ? playerStats.CurrentHP / playerStats.MaxHP : 0;
                DrawThinBar(new Rect(posX + 10, posY + 26, hudWidth - 20, 10), hpRatio, new Color(0.85f, 0.22f, 0.22f), $"{Mathf.CeilToInt(playerStats.CurrentHP)} / {Mathf.CeilToInt(playerStats.MaxHP)}");

                // 2. Thin Stamina Bar (Height: 8px)
                float stamRatio = playerStats.MaxStamina > 0 ? playerStats.CurrentStamina / playerStats.MaxStamina : 0;
                DrawThinBar(new Rect(posX + 10, posY + 40, hudWidth - 20, 8), stamRatio, new Color(0.18f, 0.72f, 0.9f), "");

                // 3. Thin XP Bar (Height: 4px)
                float xpRatio = playerStats.XPToNextLevel > 0 ? (float)playerStats.CurrentXP / playerStats.XPToNextLevel : 0;
                DrawThinBar(new Rect(posX + 10, posY + 53, hudWidth - 20, 4), xpRatio, new Color(0.92f, 0.75f, 0.15f), "");
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

        private void DrawThinBar(Rect rect, float fillRatio, Color fillColor, string labelText)
        {
            // Dark track background
            GUI.color = new Color(0.1f, 0.12f, 0.15f, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            // Fill bar
            GUI.color = fillColor;
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fillRatio), rect.height);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

            // Label (if provided)
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
