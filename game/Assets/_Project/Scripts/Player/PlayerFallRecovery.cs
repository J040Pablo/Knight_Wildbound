using UnityEngine;
using Roguelite.Core;

namespace Roguelite.Player
{
    public class PlayerFallRecovery : MonoBehaviour
    {
        [Header("Fall Threshold Settings")]
        [SerializeField] private float fallYThreshold = -10.0f;
        [SerializeField] private bool enableRecovery = true;

        public float FallYThreshold
        {
            get => fallYThreshold;
            set => fallYThreshold = value;
        }

        private CharacterController characterController;
        private PlayerController playerController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (!enableRecovery) return;

            if (transform.position.y < fallYThreshold)
            {
                RecoverPlayer();
            }
        }

        public void RecoverPlayer()
        {
            Debug.LogWarning($"[PlayerFallRecovery] Player fell below safe Y threshold ({transform.position.y:F1} < {fallYThreshold}). Teleporting to valid spawn point.");

            // 1. Temporarily disable CharacterController to allow position override
            if (characterController != null) characterController.enabled = false;

            // 2. Clear any accumulated movement/velocity in PlayerController
            if (playerController != null)
            {
                playerController.ResetVelocity();
            }

            // 3. Delegate safe position lookup to SpawnManager
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.SpawnPlayer(gameObject);
            }
            else
            {
                // Fallback position if SpawnManager instance is unavailable
                transform.position = new Vector3(0, 1.0f, 0);
            }

            // 4. Re-enable CharacterController
            if (characterController != null) characterController.enabled = true;

            // 5. Ensure camera target remains aligned
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var tpCam = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpCam != null)
                {
                    tpCam.target = transform;
                }
            }
        }
    }
}
