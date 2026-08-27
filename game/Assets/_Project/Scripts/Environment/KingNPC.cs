using UnityEngine;
using Roguelite.Core;

namespace Roguelite.Environment
{
    public class KingNPC : MonoBehaviour, IInteractable
    {
        [Header("King Dialogue Setup")]
        [SerializeField] private string speakerName = "KING";
        [SerializeField] private string dialogueLine = "Take a weapon. Don't you think you've rested enough?";

        public bool HasFinishedDialogue { get; private set; } = false;

        public string InteractionPrompt => HasFinishedDialogue ? "King (Rest well)" : "E — Talk to King";

        public bool CanInteract(GameObject player)
        {
            return !HasFinishedDialogue && DialogueSystem.Instance != null && !DialogueSystem.Instance.IsDialogueActive;
        }

        public void Interact(GameObject player)
        {
            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.PlayDialogue(speakerName, dialogueLine, () =>
                {
                    HasFinishedDialogue = true;
                    // Auto-trigger weapon pickup unlock notification
                });
            }
        }

        private void Start()
        {
            // Auto-trigger initial King dialogue when player spawns in Ruins
            Invoke(nameof(TriggerInitialDialogue), 0.5f);
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
