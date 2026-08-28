using UnityEngine;
using Roguelite.Core;

namespace Roguelite.Environment
{
    public class RuinsExitGate : MonoBehaviour, IInteractable
    {
        public bool IsUnlocked { get; private set; } = false;

        public string InteractionPrompt => IsUnlocked ? "Ruins Gate (Open)" : "Gate Locked (Choose a weapon first)";

        public bool CanInteract(GameObject player)
        {
            return !IsUnlocked;
        }

        public void Interact(GameObject player)
        {
            // Dialogue or hint if player approaches locked gate before weapon pick
            if (!IsUnlocked && DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.PlayDialogue("RUINS GATE", "Choose a weapon from the pedestals to unlock the gate and start your run.");
            }
        }

        public void UnlockGate()
        {
            IsUnlocked = true;

            // Disable gate solid collider or lower/open gate barrier physically
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true; // Allow player to walk straight through
            }

            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.2f, 0.9f, 0.3f, 0.3f); // Translucent green open archway
            }

            // Lower gate barrier position slightly for visual opening feedback
            transform.position += Vector3.down * 3.5f;

            if (Wave.EncounterManager.Instance != null)
            {
                Wave.EncounterManager.Instance.TriggerBanner("🚪 RUINS GATE UNLOCKED! Proceed to Horse Valley & Forest!");
            }
        }
    }
}
