using UnityEngine;
using Roguelite.Core;

namespace Roguelite.Player
{
    public class PlayerFallRecovery : MonoBehaviour
    {
        [Header("Fall Threshold Settings")]
        [SerializeField] private float fallYThreshold = -25.0f;
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
            Debug.LogWarning($"[PlayerFallRecovery] Player fell below safe Y threshold ({transform.position.y:F1} < {fallYThreshold}). Recovering to valid trail position.");

            if (characterController != null) characterController.enabled = false;

            if (playerController != null)
            {
                playerController.ResetVelocity();
            }

            // Calculate safe recovery position on main trail near current Z progression
            float safeZ = Mathf.Clamp(transform.position.z, 0f, 750f);
            float pathX = Environment.SceneEnvironmentBuilder.GetForestPathXOffset(safeZ);
            float terrainY = Environment.SceneEnvironmentBuilder.GetTerrainHeightY(pathX, safeZ);
            Vector3 safePos = new Vector3(pathX, terrainY + 0.8f, safeZ);

            transform.position = safePos;

            Physics.SyncTransforms();

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
