using UnityEngine;

namespace Roguelite.Core
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(GameObject player);
        void Interact(GameObject player);
    }
}
