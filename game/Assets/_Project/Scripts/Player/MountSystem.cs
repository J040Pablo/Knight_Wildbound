using UnityEngine;
using Roguelite.Core;
using Roguelite.Combat;

namespace Roguelite.Player
{
    public class MountSystem : MonoBehaviour, IInteractable, IDamageable
    {
        private HorseController horseController;
        private GameObject mountedPlayer;
        private ThirdPersonCamera tpCam;
        private CapsuleCollider mountedPlayerCollider;

        public bool IsPlayerMounted => mountedPlayer != null;

        public string InteractionPrompt => IsPlayerMounted ? "E — Dismount" : "E — Mount Horse";

        public float CurrentHP => mountedPlayer != null ? mountedPlayer.GetComponent<PlayerStats>().CurrentHP : 100f;
        public float MaxHP => mountedPlayer != null ? mountedPlayer.GetComponent<PlayerStats>().MaxHP : 100f;
        public bool IsDead => mountedPlayer != null && mountedPlayer.GetComponent<PlayerStats>().IsDead;

        private void Awake()
        {
            horseController = GetComponent<HorseController>();
            if (horseController == null)
            {
                horseController = gameObject.AddComponent<HorseController>();
            }
        }

        private void Start()
        {
            tpCam = FindFirstObjectByType<ThirdPersonCamera>();
        }

        public bool CanInteract(GameObject player)
        {
            return true;
        }

        public void Interact(GameObject player)
        {
            if (IsPlayerMounted)
            {
                DismountPlayer();
            }
            else if (player != null)
            {
                MountPlayer(player);
            }
        }

        public void MountPlayer(GameObject player)
        {
            if (IsPlayerMounted) return;

            mountedPlayer = player;
            horseController.SetMountedState(true);

            // 1. Disable player CharacterController but add/enable a CapsuleCollider for hit detection while mounted
            CharacterController pCC = player.GetComponent<CharacterController>();
            if (pCC != null) pCC.enabled = false;

            PlayerController pCtrl = player.GetComponent<PlayerController>();
            if (pCtrl != null) pCtrl.enabled = false;

            mountedPlayerCollider = player.GetComponent<CapsuleCollider>();
            if (mountedPlayerCollider == null)
            {
                mountedPlayerCollider = player.AddComponent<CapsuleCollider>();
            }
            mountedPlayerCollider.height = 1.8f;
            mountedPlayerCollider.radius = 0.5f;
            mountedPlayerCollider.center = new Vector3(0, 0.9f, 0);
            mountedPlayerCollider.enabled = true;
            mountedPlayerCollider.isTrigger = false;

            // 2. Attach player to horse mount socket
            Transform socket = horseController.MountSocket;
            player.transform.SetParent(socket);
            player.transform.localPosition = new Vector3(0, -0.2f, 0); // Sitting socket alignment
            player.transform.localRotation = Quaternion.identity;

            // 3. Set camera target to horse mount and raise aim height
            if (tpCam == null) tpCam = FindFirstObjectByType<ThirdPersonCamera>();
            if (tpCam != null)
            {
                tpCam.target = transform;
                tpCam.IsMounted = true;
            }
        }

        public void DismountPlayer()
        {
            if (!IsPlayerMounted) return;

            GameObject player = mountedPlayer;
            mountedPlayer = null;
            horseController.SetMountedState(false);

            if (mountedPlayerCollider != null)
            {
                Destroy(mountedPlayerCollider);
                mountedPlayerCollider = null;
            }

            // 1. Unparent player
            player.transform.SetParent(null);

            // 2. Validate dismount position safely next to horse
            Vector3 candidatePos = transform.position + transform.right * 1.5f + Vector3.up * 0.2f;
            Vector3 safeDismountPos = candidatePos;

            if (PlayerSpawnManager.Instance != null && PlayerSpawnManager.Instance.ValidatePlayerPosition(candidatePos, 0.5f, out Vector3 validGroundPos))
            {
                safeDismountPos = validGroundPos;
            }
            else if (Physics.Raycast(candidatePos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            {
                safeDismountPos = hit.point + Vector3.up * 0.1f;
            }

            player.transform.position = safeDismountPos;
            player.transform.rotation = transform.rotation;

            // Force PhysX transform sync before re-enabling CharacterController
            Physics.SyncTransforms();

            // 3. Re-enable player controllers
            CharacterController pCC = player.GetComponent<CharacterController>();
            if (pCC != null) pCC.enabled = true;

            PlayerController pCtrl = player.GetComponent<PlayerController>();
            if (pCtrl != null) pCtrl.enabled = true;

            // 4. Restore camera target to player and reset aim height
            if (tpCam == null) tpCam = FindFirstObjectByType<ThirdPersonCamera>();
            if (tpCam != null)
            {
                tpCam.target = player.transform;
                tpCam.IsMounted = false;
            }
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (IsPlayerMounted && mountedPlayer != null)
            {
                PlayerStats pStats = mountedPlayer.GetComponent<PlayerStats>();
                if (pStats != null)
                {
                    pStats.TakeDamage(damageInfo);
                }
            }
        }

        private void Update()
        {
            if (UI.MasteryScreenUI.IsAnyMenuOpen)
            {
                Debug.Log("[Horse] Input Blocked By Menu");
                return;
            }

            if (IsPlayerMounted && mountedPlayer != null)
            {
                // Handle Horse Movement Inputs while mounted
                float moveX = Input.GetAxisRaw("Horizontal");
                float moveZ = Input.GetAxisRaw("Vertical");
                Vector3 inputDir = new Vector3(moveX, 0, moveZ);
                bool sprint = Input.GetKey(KeyCode.LeftShift);

                Camera mainCam = Camera.main;
                horseController.ProcessMovementInput(inputDir, sprint, mainCam);

                // Jump Input Forwarding
                if (Input.GetButtonDown("Jump"))
                {
                    horseController.TryJump();
                }
            }
        }
    }
}
