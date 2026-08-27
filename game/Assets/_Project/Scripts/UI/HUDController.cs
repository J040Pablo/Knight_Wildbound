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

        private void Start()
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
            playerCombat = FindFirstObjectByType<PlayerCombat>();
            interactionSystem = FindFirstObjectByType<InteractionSystem>();
            runManager = FindFirstObjectByType<RunManager>();
        }

        private void Update()
        {
            if (activeBoss == null)
            {
                activeBoss = FindFirstObjectByType<HollowTreeBossAI>();
            }
        }

        private void OnGUI()
        {
            if (runManager != null && runManager.State != RunState.InRun) return;

            DrawHUD();
            DrawDialogueBox();
            DrawInteractionPrompt();
        }

        private void DrawHUD()
        {
            GUI.skin.box.fontSize = 14;
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.fontStyle = FontStyle.Bold;

            // Top-Left: Player Stats Box
            GUI.Box(new Rect(15, 15, 270, 130), "");

            if (playerStats != null)
            {
                // HP Bar
                float hpRatio = playerStats.MaxHP > 0 ? playerStats.CurrentHP / playerStats.MaxHP : 0;
                DrawBar(new Rect(25, 25, 250, 22), hpRatio, new Color(0.85f, 0.2f, 0.2f), $"HP: {Mathf.CeilToInt(playerStats.CurrentHP)} / {Mathf.CeilToInt(playerStats.MaxHP)}");

                // Stamina Bar
                float stamRatio = playerStats.MaxStamina > 0 ? playerStats.CurrentStamina / playerStats.MaxStamina : 0;
                DrawBar(new Rect(25, 52, 250, 18), stamRatio, new Color(0.2f, 0.7f, 0.9f), $"Stamina: {Mathf.CeilToInt(playerStats.CurrentStamina)} / {Mathf.CeilToInt(playerStats.MaxStamina)}");

                // XP Bar & Level
                float xpRatio = playerStats.XPToNextLevel > 0 ? (float)playerStats.CurrentXP / playerStats.XPToNextLevel : 0;
                DrawBar(new Rect(25, 75, 250, 18), xpRatio, new Color(0.9f, 0.75f, 0.1f), $"Level {playerStats.Level} (XP: {playerStats.CurrentXP}/{playerStats.XPToNextLevel})");

                // Attacks Indicator
                GUI.color = Color.white;
                CharacterType cType = GameSessionManager.Instance != null ? GameSessionManager.Instance.SelectedCharacter : CharacterType.Knight;
                string lightName = cType == CharacterType.Mage ? "Magic Bolt" : (cType == CharacterType.Druid ? "Nature Bolt" : "Slash");
                string heavyName = cType == CharacterType.Mage ? "Fireball" : (cType == CharacterType.Druid ? "Nature Burst" : "Heavy Sweep");
                string chargeStatus = playerCombat != null && playerCombat.IsCharging ? $" ({Mathf.RoundToInt(playerCombat.ChargeRatio * 100)}%)" : "";
                GUI.Label(new Rect(25, 98, 260, 24), $"L-Click: {lightName} | Hold: {heavyName}{chargeStatus}");
            }

            // Top-Right: Run Time & Encounter Info Box
            GUI.Box(new Rect(Screen.width - 230, 15, 215, 80), "");
            if (runManager != null)
            {
                int mins = (int)(runManager.RunTimeSeconds / 60);
                int secs = (int)(runManager.RunTimeSeconds % 60);
                GUI.Label(new Rect(Screen.width - 220, 22, 205, 24), $"⏱️ Time: {mins:D2}:{secs:D2}");
            }

            // Active Encounter Status
            if (EncounterManager.Instance != null && EncounterManager.Instance.ActiveZone != null)
            {
                var zone = EncounterManager.Instance.ActiveZone;
                GUI.Label(new Rect(Screen.width - 220, 48, 205, 24), $"⚔️ {zone.Difficulty} Encounter | Left: {zone.EnemiesRemaining}");
            }
            else
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                GUI.Label(new Rect(Screen.width - 220, 48, 205, 24), $"📍 Area: {sceneName}");
            }

            // Top-Center: Hollow Tree Boss Health Bar
            if (activeBoss != null && !activeBoss.IsDead)
            {
                float bossHpRatio = activeBoss.MaxHP > 0 ? activeBoss.CurrentHP / activeBoss.MaxHP : 0;
                float barWidth = Mathf.Min(550, Screen.width - 100);
                Rect bossRect = new Rect((Screen.width - barWidth) / 2, 20, barWidth, 34);

                Color bossColor = activeBoss.IsPhase2 ? new Color(0.95f, 0.15f, 0.25f) : new Color(0.85f, 0.45f, 0.1f);
                string phaseText = activeBoss.IsPhase2 ? "PHASE 2 (ENRAGED)" : "PHASE 1";
                DrawBar(bossRect, bossHpRatio, bossColor, $"👑 THE HOLLOW TREE [{phaseText}] — HP: {Mathf.CeilToInt(activeBoss.CurrentHP)} / {Mathf.CeilToInt(activeBoss.MaxHP)}");
            }

            // Center Banner Notification (Encounter events / Zone cleared)
            if (EncounterManager.Instance != null && EncounterManager.Instance.StatusBannerTimer > 0)
            {
                GUI.color = Color.yellow;
                GUI.skin.label.fontSize = 22;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(0, Screen.height * 0.26f, Screen.width, 45), EncounterManager.Instance.StatusBannerText);
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                GUI.skin.label.fontSize = 14;
                GUI.color = Color.white;
            }
        }

        private void DrawInteractionPrompt()
        {
            if (interactionSystem == null) interactionSystem = FindFirstObjectByType<InteractionSystem>();
            if (interactionSystem == null || string.IsNullOrEmpty(interactionSystem.CurrentPrompt)) return;

            float w = 320f;
            float h = 42f;
            Rect rect = new Rect((Screen.width - w) / 2f, Screen.height * 0.76f, w, h);

            GUI.color = new Color(0.1f, 0.2f, 0.35f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.cyan;
            GUI.Box(rect, "");

            GUI.skin.label.fontSize = 16;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            GUI.Label(rect, interactionSystem.CurrentPrompt);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.label.fontSize = 14;
        }

        private void DrawDialogueBox()
        {
            if (DialogueSystem.Instance == null || !DialogueSystem.Instance.IsDialogueActive) return;

            float w = Mathf.Min(650f, Screen.width - 60);
            float h = 130f;
            Rect rect = new Rect((Screen.width - w) / 2f, Screen.height - h - 30f, w, h);

            GUI.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.9f, 0.75f, 0.1f);
            GUI.Box(rect, "");

            // Speaker Name
            GUI.skin.label.fontSize = 18;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.color = new Color(1.0f, 0.85f, 0.2f);
            GUI.Label(new Rect(rect.x + 20, rect.y + 15, w - 40, 28), DialogueSystem.Instance.CurrentSpeaker);

            // Dialogue Line Text
            GUI.skin.label.fontSize = 15;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 20, rect.y + 45, w - 40, 50), DialogueSystem.Instance.CurrentText);

            // Continue Hint
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.fontStyle = FontStyle.Italic;
            GUI.skin.label.alignment = TextAnchor.LowerRight;
            GUI.color = Color.gray;
            GUI.Label(new Rect(rect.x + 20, rect.y + h - 25, w - 40, 20), "Press [E / Space / Left Click] to continue...");
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void DrawBar(Rect rect, float fillRatio, Color fillColor, string labelText)
        {
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = fillColor;
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fillRatio), rect.height);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(rect, labelText);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }
    }
}
