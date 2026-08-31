using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Roguelite.Core;
using Roguelite.Core.StateMachine;
using Roguelite.UI.Theme;

namespace Roguelite.UI.Widgets
{
    public class HUDInteractionPromptWidget : MonoBehaviour
    {
        private Image reticleImage;
        private GameObject promptContainer;
        private TextMeshProUGUI promptText;

        private InteractionSystem interactionSystem;

        private void Awake()
        {
            BuildUI();
        }

        private void Start()
        {
            interactionSystem = FindFirstObjectByType<InteractionSystem>();
        }

        private void BuildUI()
        {
            // --- CENTER RETICLE ---
            reticleImage = HUDTheme.CreateImage(transform, "Center_Reticle", HUDTheme.Circle(Color.white, 1, Color.black), Color.white);
            reticleImage.rectTransform.SetRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));

            // --- INTERACTION PROMPT BOX ---
            RectTransform pBox = HUDTheme.CreatePanel(transform, "Interaction_Prompt_Box",
                HUDTheme.RoundedRect(8, HUDTheme.WoodDark, 2, HUDTheme.GoldAccent), HUDTheme.WoodDark);
            pBox.SetRect(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(360f, 40f));
            promptContainer = pBox.gameObject;

            promptText = HUDTheme.CreateText(pBox, "Prompt_Text", "[E] INTERAGIR", 13f, HUDTheme.GoldAccent, TextAlignmentOptions.Center, FontStyles.Bold);
            promptText.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            promptContainer.SetActive(false);
        }

        private void Update()
        {
            if (interactionSystem == null)
            {
                interactionSystem = FindFirstObjectByType<InteractionSystem>();
            }

            bool isGameplayActive = GameStateManager.Instance == null || GameStateManager.Instance.IsGameplayActive();
            bool isDialogueActive = DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive;

            // Reticle visibility
            if (reticleImage != null)
            {
                reticleImage.gameObject.SetActive(isGameplayActive && !isDialogueActive);
            }

            // Prompt visibility & string update
            if (isGameplayActive && !isDialogueActive && interactionSystem != null && !string.IsNullOrEmpty(interactionSystem.CurrentPrompt))
            {
                if (!promptContainer.activeSelf) promptContainer.SetActive(true);
                promptText.text = interactionSystem.CurrentPrompt;
            }
            else
            {
                if (promptContainer.activeSelf) promptContainer.SetActive(false);
            }
        }
    }
}
