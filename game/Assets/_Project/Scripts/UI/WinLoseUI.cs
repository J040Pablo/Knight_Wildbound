using UnityEngine;
using Roguelite.Core;
using Roguelite.Player;

namespace Roguelite.UI
{
    public class WinLoseUI : MonoBehaviour
    {
        private RunManager runManager;
        private PlayerStats playerStats;

        private void Start()
        {
            runManager = FindFirstObjectByType<RunManager>();
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        private void OnGUI()
        {
            if (runManager == null || runManager.State == RunState.InRun) return;

            // Darken background
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            bool isVictory = runManager.State == RunState.Victory;

            // Container Box
            float boxWidth = 420f;
            float boxHeight = 300f;
            Rect boxRect = new Rect((Screen.width - boxWidth) / 2f, (Screen.height - boxHeight) / 2f, boxWidth, boxHeight);

            GUI.color = isVictory ? new Color(0.1f, 0.25f, 0.15f, 0.95f) : new Color(0.25f, 0.1f, 0.1f, 0.95f);
            GUI.DrawTexture(boxRect, Texture2D.whiteTexture);
            GUI.color = isVictory ? Color.green : Color.red;
            GUI.Box(boxRect, "");

            // Title
            GUI.skin.label.fontSize = 28;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = isVictory ? new Color(0.3f, 1.0f, 0.4f) : new Color(1.0f, 0.3f, 0.3f);
            string titleText = isVictory ? "🏆 VICTORY! 🏆" : "☠️ GAME OVER ☠️";
            GUI.Label(new Rect(boxRect.x, boxRect.y + 20, boxWidth, 40), titleText);

            // Subtitle
            GUI.skin.label.fontSize = 16;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = Color.white;
            string subText = isVictory ? "The Hollow Tree has been defeated!" : "You fell in combat during the adventure run.";
            GUI.Label(new Rect(boxRect.x, boxRect.y + 65, boxWidth, 30), subText);

            // Statistics Summary
            int mins = (int)(runManager.RunTimeSeconds / 60);
            int secs = (int)(runManager.RunTimeSeconds % 60);
            int level = playerStats != null ? playerStats.Level : 1;

            GUI.skin.label.fontSize = 14;
            GUI.Label(new Rect(boxRect.x + 40, boxRect.y + 110, boxWidth - 80, 25), $"⏱️ Run Duration: {mins:D2}:{secs:D2}");
            GUI.Label(new Rect(boxRect.x + 40, boxRect.y + 135, boxWidth - 80, 25), $"⚔️ Total Enemies Killed: {runManager.TotalKills}");
            GUI.Label(new Rect(boxRect.x + 40, boxRect.y + 160, boxWidth - 80, 25), $"⭐ Level Reached: {level}");

            // Action Button: Play Again / Restart
            GUI.color = isVictory ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.2f);
            string btnText = isVictory ? "PLAY AGAIN" : "RESTART RUN";
            if (GUI.Button(new Rect(boxRect.x + 60, boxRect.y + boxHeight - 55, boxWidth - 120, 40), btnText))
            {
                runManager.RestartRun();
            }

            GUI.color = Color.white;
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }
    }
}
