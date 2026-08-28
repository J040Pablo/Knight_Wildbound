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

            if (characterController != null) characterController.enabled = false;

            if (playerController != null)
            {
                playerController.ResetVelocity();
            }

            if (PlayerSpawnManager.Instance != null)
            {
                PlayerSpawnManager.Instance.SpawnPlayer(gameObject);
            }
            else
            {
                transform.position = new Vector3(0, 0.5f, 2.0f);
            }

            if (characterController != null) characterController.enabled = true;

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
