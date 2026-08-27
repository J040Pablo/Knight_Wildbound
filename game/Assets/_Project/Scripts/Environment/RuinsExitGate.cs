using UnityEngine;
using Roguelite.Core;

namespace Roguelite.Environment
{
    public class RuinsExitGate : MonoBehaviour, IInteractable
    {
        public bool IsUnlocked { get; private set; } = false;

        public string InteractionPrompt => IsUnlocked ? "E — Enter Forest" : "Locked (Choose a weapon first)";

        public bool CanInteract(GameObject player)
        {
            return IsUnlocked;
        }

        public void Interact(GameObject player)
        {
            if (IsUnlocked && SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadScene(SceneTransitionManager.SCENE_FOREST);
            }
        }

        public void UnlockGate()
        {
            IsUnlocked = true;
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.2f, 0.9f, 0.3f, 0.6f); // Soft glowing green gate
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsUnlocked && other.CompareTag("Player"))
            {
                Interact(other.gameObject);
            }
        }
    }
}
