using UnityEngine;
using Roguelite.Core;
using Roguelite.Environment.Data;

namespace Roguelite.Environment
{
    public class KingNPC : MonoBehaviour, IInteractable
    {
        [Header("Dialogue Data")]
        [SerializeField] private TutorialDialogueData dialogueData;

        public bool HasFinishedDialogue { get; private set; } = false;

        public string InteractionPrompt => HasFinishedDialogue ? "E — Speak with King" : "E — Talk to King";

        private void Awake()
        {
            if (dialogueData == null)
            {
                dialogueData = ScriptableObject.CreateInstance<TutorialDialogueData>();
            }
        }

        private void Start()
        {
            // Auto-trigger initial King dialogue when player spawns in Ruins
            Invoke(nameof(TriggerInitialDialogue), 0.3f);
        }

        public bool CanInteract(GameObject player)
        {
            return DialogueSystem.Instance != null && !DialogueSystem.Instance.IsDialogueActive;
        }

        public void Interact(GameObject player)
        {
            if (DialogueSystem.Instance == null) return;

            if (!HasFinishedDialogue)
            {
                Debug.Log("[King] Intro dialogue started");
                DialogueSystem.Instance.PlayDialogue(dialogueData.speakerName, dialogueData.introLine, () =>
                {
                    HasFinishedDialogue = true;
                    Debug.Log("[King] Intro dialogue completed");
                });
            }
            else
            {
                bool hasClass = GameSessionManager.Instance != null && GameSessionManager.Instance.HasSelectedCharacter;
                string line = hasClass ? dialogueData.postSelectionLine : dialogueData.weaponPromptLine;
                DialogueSystem.Instance.PlayDialogue(dialogueData.speakerName, line);
            }
        }

        private void TriggerInitialDialogue()
        {
            if (!HasFinishedDialogue && DialogueSystem.Instance != null && !DialogueSystem.Instance.IsDialogueActive)
            {
                Interact(null);
            }
        }
    }
}
