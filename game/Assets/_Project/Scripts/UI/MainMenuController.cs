using UnityEngine;
using UnityEngine.SceneManagement;
using Roguelite.Core;

namespace Roguelite.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Config")]
        [SerializeField] private string gameSceneName = "GameArena";

        private bool showCharacterSelect = false;

        private void OnGUI()
        {
            // Title & Main Panel Box
            float panelWidth = 460f;
            float panelHeight = 360f;
            Rect panelRect = new Rect((Screen.width - panelWidth) / 2f, (Screen.height - panelHeight) / 2f, panelWidth, panelHeight);

            // Dark background panel
            GUI.color = new Color(0.1f, 0.15f, 0.22f, 0.95f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.6f, 0.9f);
            GUI.Box(panelRect, "");

            if (!showCharacterSelect)
            {
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
                    "• Left Click / J: Light Sword Attack\n" +
                    "• Hold Left Click / J: Heavy Charged Attack Sweep\n" +
                    "• Defeat Slimes, Goblins, Wolves & Level Up to claim Upgrades!");

                // Buttons
                GUI.color = new Color(0.2f, 0.8f, 0.3f);
                GUI.skin.button.fontSize = 16;
                GUI.skin.button.fontStyle = FontStyle.Bold;

                if (GUI.Button(new Rect(panelRect.x + 80, panelRect.y + panelHeight - 110, panelWidth - 160, 42), "CHARACTER SELECT"))
                {
                    showCharacterSelect = true;
                }

                if (GUI.Button(new Rect(panelRect.x + 80, panelRect.y + panelHeight - 55, panelWidth - 160, 42), "START RUN NOW"))
                {
                    StartGameRun();
                }
            }
            else
            {
                // Character Selection Screen with 3 Class Tabs
                GUI.skin.label.fontSize = 22;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.color = new Color(1.0f, 0.85f, 0.2f);
                GUI.Label(new Rect(panelRect.x, panelRect.y + 15, panelWidth, 30), "CHOOSE YOUR HERO");

                CharacterType selected = GameSettings.Instance.SelectedCharacter;

                // 3 Class Tab Buttons
                float tabWidth = 130f;
                float tabY = panelRect.y + 50f;

                GUI.color = selected == CharacterType.Knight ? new Color(0.3f, 0.7f, 1.0f) : Color.gray;
                if (GUI.Button(new Rect(panelRect.x + 35, tabY, tabWidth, 32), "KNIGHT"))
                {
                    GameSettings.Instance.SelectedCharacter = CharacterType.Knight;
                }

                GUI.color = selected == CharacterType.Mage ? new Color(0.8f, 0.4f, 1.0f) : Color.gray;
                if (GUI.Button(new Rect(panelRect.x + 180, tabY, tabWidth, 32), "MAGE"))
                {
                    GameSettings.Instance.SelectedCharacter = CharacterType.Mage;
                }

                GUI.color = selected == CharacterType.Druid ? new Color(0.3f, 0.85f, 0.4f) : Color.gray;
                if (GUI.Button(new Rect(panelRect.x + 325, tabY, tabWidth, 32), "DRUID"))
                {
                    GameSettings.Instance.SelectedCharacter = CharacterType.Druid;
                }

                // Selected Class Stat Card
                GUI.color = Color.white;
                GUI.skin.label.fontSize = 13;
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                string statsText = "";

                switch (selected)
                {
                    case CharacterType.Mage:
                        statsText = "CLASS: MAGE (Ranged & AoE Specialist)\n\n" +
                                    "A master of arcane elementals wielding a staff.\n\n" +
                                    "• Health: 75 HP | Speed: 7.2 m/s\n" +
                                    "• Light Attack: Magic Bolt (Ranged fast projectile)\n" +
                                    "• Charged Attack: Fireball (AoE explosion + knockback)";
                        break;
                    case CharacterType.Druid:
                        statsText = "CLASS: DRUID (Support & Nature Magic)\n\n" +
                                    "A nature warden with healing and crowd control.\n\n" +
                                    "• Health: 90 HP | Speed: 6.8 m/s\n" +
                                    "• Light Attack: Nature Projectile\n" +
                                    "• Charged Attack: Nature Burst (AoE slow + heal)\n" +
                                    "• Passive: Nature's Blessing (Regen when HP < 50%)";
                        break;
                    case CharacterType.Knight:
                    default:
                        statsText = "CLASS: KNIGHT (Melee Tank)\n\n" +
                                    "A brave low-poly warrior with high health.\n\n" +
                                    "• Health: 100 HP | Speed: 7.0 m/s\n" +
                                    "• Light Attack: Greatsword Slash\n" +
                                    "• Charged Attack: Heavy Greatsword Sweep";
                        break;
                }

                GUI.Label(new Rect(panelRect.x + 40, panelRect.y + 95, panelWidth - 80, 160), statsText);

                GUI.color = new Color(0.2f, 0.8f, 0.3f);
                if (GUI.Button(new Rect(panelRect.x + 60, panelRect.y + panelHeight - 55, 160, 40), "CONFIRM & PLAY"))
                {
                    StartGameRun();
                }

                GUI.color = new Color(0.8f, 0.3f, 0.2f);
                if (GUI.Button(new Rect(panelRect.x + panelWidth - 220, panelRect.y + panelHeight - 55, 160, 40), "BACK"))
                {
                    showCharacterSelect = false;
                }
            }

            GUI.color = Color.white;
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void StartGameRun()
        {
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
