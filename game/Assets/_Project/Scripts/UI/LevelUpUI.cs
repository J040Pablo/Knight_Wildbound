using System.Collections.Generic;
using UnityEngine;
using Roguelite.Data;
using Roguelite.Progression;

namespace Roguelite.UI
{
    public class LevelUpUI : MonoBehaviour
    {
        private UpgradeManager upgradeManager;
        private List<UpgradeData> currentChoices;
        private bool isShowing = false;

        private void Start()
        {
            upgradeManager = FindFirstObjectByType<UpgradeManager>();
            if (upgradeManager != null)
            {
                upgradeManager.OnLevelUpUpgradeChoices += ShowLevelUpChoices;
            }
        }

        private void OnDestroy()
        {
            if (upgradeManager != null)
            {
                upgradeManager.OnLevelUpUpgradeChoices -= ShowLevelUpChoices;
            }
        }

        private void ShowLevelUpChoices(List<UpgradeData> choices)
        {
            currentChoices = choices;
            isShowing = true;
        }

        private void OnGUI()
        {
            if (!isShowing || currentChoices == null || currentChoices.Count == 0) return;

            // Modal Background Overlay
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Title
            GUI.skin.label.fontSize = 26;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = new Color(1.0f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 40), "⚡ LEVEL UP! CHOOSE AN UPGRADE ⚡");
            GUI.color = Color.white;

            // Render 3 Choice Cards
            float cardWidth = 260f;
            float cardHeight = 220f;
            float totalWidth = currentChoices.Count * cardWidth + (currentChoices.Count - 1) * 30f;
            float startX = (Screen.width - totalWidth) / 2f;
            float startY = (Screen.height - cardHeight) / 2f;

            for (int i = 0; i < currentChoices.Count; i++)
            {
                UpgradeData upgrade = currentChoices[i];
                Rect cardRect = new Rect(startX + i * (cardWidth + 30f), startY, cardWidth, cardHeight);

                // Draw Card Container Box
                GUI.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
                GUI.DrawTexture(cardRect, Texture2D.whiteTexture);
                GUI.color = new Color(0.3f, 0.6f, 0.9f);
                GUI.Box(cardRect, "");

                // Header
                GUI.skin.label.fontSize = 18;
                GUI.color = Color.cyan;
                GUI.Label(new Rect(cardRect.x + 10, cardRect.y + 15, cardRect.width - 20, 30), upgrade.upgradeTitle);

                // Description
                GUI.skin.label.fontSize = 13;
                GUI.skin.label.fontStyle = FontStyle.Normal;
                GUI.color = Color.white;
                GUI.Label(new Rect(cardRect.x + 15, cardRect.y + 55, cardRect.width - 30, 90), upgrade.description);

                // Select Button
                GUI.color = new Color(0.2f, 0.8f, 0.3f);
                if (GUI.Button(new Rect(cardRect.x + 20, cardRect.y + cardHeight - 45, cardRect.width - 40, 32), "SELECT UPGRADE"))
                {
                    isShowing = false;
                    GUI.color = Color.white;
                    GUI.skin.label.fontSize = 14;
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    upgradeManager.SelectUpgrade(upgrade);
                    break;
                }
            }

            GUI.color = Color.white;
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }
    }
}
