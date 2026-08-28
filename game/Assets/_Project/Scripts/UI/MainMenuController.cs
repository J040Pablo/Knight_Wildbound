using UnityEngine;
using UnityEngine.SceneManagement;
using Roguelite.Core;

namespace Roguelite.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Config")]
        [SerializeField] private string gameSceneName = "01_Run";

        private void OnGUI()
        {
            // Title & Main Panel Box
            float panelWidth = 460f;
            float panelHeight = 300f;
            Rect panelRect = new Rect((Screen.width - panelWidth) / 2f, (Screen.height - panelHeight) / 2f, panelWidth, panelHeight);

            // Dark background panel
            GUI.color = new Color(0.1f, 0.15f, 0.22f, 0.95f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.6f, 0.9f);
            GUI.Box(panelRect, "");

            // Main Title
            GUI.skin.label.fontSize = 28;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = new Color(1.0f, 0.85f, 0.2f);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 25, panelWidth, 40), "⚔️ KNIGHT'S FOREST ROGUELITE ⚔️");

            GUI.skin.label.fontSize = 15;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.color = Color.cyan;
            GUI.Label(new Rect(panelRect.x, panelRect.y + 70, panelWidth, 30), "Third-Person Action RPG Prototype");

            // Instructions overview
            GUI.color = Color.white;
            GUI.skin.label.fontSize = 13;
            GUI.Label(new Rect(panelRect.x + 30, panelRect.y + 110, panelWidth - 60, 110),
                "• WASD / Mouse: Move & Camera Orbit\n" +
                "• Left Shift: Sprint | Space: Jump | Ctrl/C: Dodge Roll\n" +
                "• Left Click / J: Light Attack | Hold: Charged Attack\n" +
                "• Wake in the Ruins, speak with the King, and choose\n" +
                "  your weapon to begin your journey.");

            // Button
            GUI.color = new Color(0.2f, 0.8f, 0.3f);
            GUI.skin.button.fontSize = 16;
            GUI.skin.button.fontStyle = FontStyle.Bold;

            if (GUI.Button(new Rect(panelRect.x + 80, panelRect.y + panelHeight - 60, panelWidth - 160, 42), "BEGIN YOUR JOURNEY"))
            {
                StartGameRun();
            }

            GUI.color = Color.white;
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void StartGameRun()
        {
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.ResetSession();
            }

            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                // If single scene build, trigger Game Bootstrapper directly
                var bootstrapper = FindFirstObjectByType<GameBootstrapper>();
                if (bootstrapper != null)
                {
                    bootstrapper.StartRunFromMenu();
                }
            }
        }
    }
}
