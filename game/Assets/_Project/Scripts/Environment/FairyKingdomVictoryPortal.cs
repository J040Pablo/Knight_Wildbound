using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Roguelite.Core;
using Roguelite.UI.Theme;
using Roguelite.UI;
using Roguelite.Wave;

namespace Roguelite.Environment
{
    public class FairyKingdomVictoryPortal : MonoBehaviour, IInteractable
    {
        private static GameObject confirmationWindow;
        private static FairyKingdomVictoryPortal currentPortalInstance;

        public string InteractionPrompt => "F — Entrar no Portal Encantado";

        public bool CanInteract(GameObject player)
        {
            return true;
        }

        public void Interact(GameObject player)
        {
            currentPortalInstance = this;
            OpenConfirmationWindow();
        }

        private void OpenConfirmationWindow()
        {
            if (confirmationWindow != null)
            {
                confirmationWindow.SetActive(true);
                return;
            }

            // Create Confirmation UI Window
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas == null) return;

            GameObject winObj = new GameObject("PortalConfirmation_UIWindow");
            winObj.transform.SetParent(mainCanvas.transform, false);

            RectTransform container = HUDTheme.CreatePanel(winObj.transform, "Container",
                HUDTheme.RoundedRect(12, new Color(0.12f, 0.08f, 0.18f, 0.95f), 2, HUDTheme.GoldAccent), Color.white);
            container.SetRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 180f));

            // Title
            TextMeshProUGUI title = HUDTheme.CreateText(container, "Title", "PORTAL DO REINO ENCANTADO", 16f, HUDTheme.GoldAccent, TextAlignmentOptions.Center, FontStyles.Bold);
            title.rectTransform.SetRect(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(0f, 30f));

            // Message
            TextMeshProUGUI msg = HUDTheme.CreateText(container, "Message", "Deseja seguir para a próxima região?\n(Certifique-se de coletar todas as recompensas antes de avançar)", 12f, HUDTheme.TextCream, TextAlignmentOptions.Center, FontStyles.Normal);
            msg.rectTransform.SetRect(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(-30f, 50f));

            // Buttons / Prompt Hints
            TextMeshProUGUI btnHint = HUDTheme.CreateText(container, "BtnHint", "[ E ]  Confirmar     |     [ ESC ]  Cancelar", 13f, HUDTheme.StaminaGreen, TextAlignmentOptions.Center, FontStyles.Bold);
            btnHint.rectTransform.SetRect(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(0f, 30f));

            confirmationWindow = winObj;
        }

        private void Update()
        {
            if (confirmationWindow != null && confirmationWindow.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
                {
                    ConfirmTransition();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseConfirmationWindow();
                }
            }
        }

        public static void ConfirmTransition()
        {
            CloseConfirmationWindow();

            // Debug.Log("🌀 [VICTORY PORTAL] — Player confirmed transition to Biome 2 / Next Region!");
            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.TriggerBanner("✨ AVANÇANDO PARA A PRÓXIMA REGIÃO...");
            }

            // Teleport player to Transition Pass at Z: 730
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                
                float groundY = SceneEnvironmentBuilder.GetTerrainHeightY(0, 730f);
                player.transform.position = new Vector3(0, groundY + 0.5f, 730f);

                if (cc != null) cc.enabled = true;
                Physics.SyncTransforms();
            }
        }

        public static void CloseConfirmationWindow()
        {
            if (confirmationWindow != null)
            {
                confirmationWindow.SetActive(false);
            }
        }
    }
}
