using UnityEngine;
using Roguelite.Core;

namespace Roguelite.Environment
{
    public class ForestExitPortal : MonoBehaviour, IInteractable
    {
        public bool IsUnlocked { get; private set; } = false;

        public string InteractionPrompt => IsUnlocked ? "E — Enter Hollow Tree Lair" : "Portal Locked (Clear Hard Encounter)";

        public bool CanInteract(GameObject player)
        {
            return IsUnlocked;
        }

        public void Interact(GameObject player)
        {
            if (IsUnlocked && SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadScene(SceneTransitionManager.SCENE_BOSS);
            }
        }

        public void UnlockPortal()
        {
            IsUnlocked = true;
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.8f, 0.2f, 0.9f, 0.7f); // Ominous purple boss portal
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
