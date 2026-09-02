using UnityEngine;
using Roguelite.Player;

namespace Roguelite.Core
{
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Interaction Config")]
        [SerializeField] private float interactRange = 3.5f;
        [SerializeField] private KeyCode interactKey = KeyCode.F;

        public IInteractable CurrentInteractable { get; private set; }
        public string CurrentPrompt => CurrentInteractable != null ? CurrentInteractable.InteractionPrompt : string.Empty;

        private void Update()
        {
            // 1. If currently mounted, pressing interactKey (F) MUST ALWAYS prioritize dismounting the horse!
            MountSystem activeMount = MountSystem.ActiveMount;
            if (activeMount != null && activeMount.IsPlayerMounted)
            {
                if (Input.GetKeyDown(interactKey))
                {
                    activeMount.ForceDismount();
                    return;
                }
            }

            // 2. On-foot interaction handling
            FindNearestInteractable();

            if (CurrentInteractable != null && Input.GetKeyDown(interactKey))
            {
                if (CurrentInteractable.CanInteract(gameObject))
                {
                    CurrentInteractable.Interact(gameObject);
                }
            }
        }

        private void FindNearestInteractable()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);
            IInteractable bestCandidate = null;
            float closestDistSqr = float.MaxValue;

            foreach (var hit in hits)
            {
                var interactable = hit.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    interactable = hit.GetComponentInParent<IInteractable>();
                }
                if (interactable == null)
                {
                    interactable = hit.GetComponentInChildren<IInteractable>();
                }

                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    float distSqr = (hit.transform.position - transform.position).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        bestCandidate = interactable;
                    }
                }
            }

            CurrentInteractable = bestCandidate;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
