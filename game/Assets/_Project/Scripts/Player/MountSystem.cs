using UnityEngine;
using Roguelite.Core;
using Roguelite.Core.StateMachine;
using Roguelite.Combat;

namespace Roguelite.Player
{
    public class MountSystem : MonoBehaviour, IInteractable, IDamageable
    {
        private HorseController horseController;
        private GameObject mountedPlayer;
        private ThirdPersonCamera tpCam;
        private CapsuleCollider mountedPlayerCollider;
        private float remountBlockUntil = 0f;

        public static MountSystem ActiveMount { get; private set; }
        public GameObject MountedPlayer => mountedPlayer;

        public bool IsPlayerMounted => mountedPlayer != null;

        public string InteractionPrompt => IsPlayerMounted ? "F — Desmontar" : "F — Montar no Cavalo";

        public float CurrentHP => (mountedPlayer != null && mountedPlayer.TryGetComponent<PlayerStats>(out var stats)) ? stats.CurrentHP : 100f;
        public float MaxHP => (mountedPlayer != null && mountedPlayer.TryGetComponent<PlayerStats>(out var stats)) ? stats.MaxHP : 100f;
        public bool IsDead => mountedPlayer != null && mountedPlayer.TryGetComponent<PlayerStats>(out var stats) && stats.IsDead;

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
            if (Time.time < remountBlockUntil)
            {
                return false;
            }
            return true;
        }

        public void Interact(GameObject player)
        {
            if (IsPlayerMounted)
            {
                ForceDismount();
            }
            else if (player != null)
            {
                MountPlayer(player);
            }
        }

        public void MountPlayer(GameObject player)
        {
            if (Time.time < remountBlockUntil)
            {
                return;
            }

            if (IsPlayerMounted || player == null) return;

            ActiveMount = this;
            mountedPlayer = player;
            horseController.SetMountedState(true);

            // 1. Disable player CharacterController & PlayerController while mounted
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

            // 3. Set camera offset for horse mount while keeping target strictly on player
            if (tpCam == null) tpCam = FindFirstObjectByType<ThirdPersonCamera>();
            if (tpCam != null)
            {
                tpCam.target = player.transform;
                tpCam.SetMountedCameraOffset();
            }

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.RequestOwnership(CameraOwnerType.Horse, player.transform, "MountSystem.MountPlayer");
            }

        }

        public void DismountPlayer()
        {

            if (!IsPlayerMounted && ActiveMount != this) return;

            // Set 1.0s remount block cooldown immediately upon dismounting
            remountBlockUntil = Time.time + 1.0f;

            // Cache player reference defensively before clearing
            GameObject player = mountedPlayer;
            if (player == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.gameObject;
                else player = GameObject.FindWithTag("Player");
            }

            if (ActiveMount == this) ActiveMount = null;
            mountedPlayer = null;
            if (horseController != null)
            {
                horseController.SetMountedState(false);
            }

            // 1. Instantly destroy mounted temporary CapsuleCollider
            if (mountedPlayerCollider != null)
            {
                mountedPlayerCollider.enabled = false;
                DestroyImmediate(mountedPlayerCollider);
                mountedPlayerCollider = null;
            }

            // 2. Force transform cleanup on player (ALWAYS UNPARENT)
            if (player != null)
            {
                CapsuleCollider[] extraCapsules = player.GetComponents<CapsuleCollider>();
                foreach (var cap in extraCapsules)
                {
                    if (cap != null) DestroyImmediate(cap);
                }

                // UNPARENT PLAYER FROM HORSE SOCKET ALWAYS
                player.transform.SetParent(null);
                player.transform.localScale = Vector3.one;

                // 3. Calculate safe dismount position (2.5m to the right of horse)
                Vector3 candidatePos = transform.position + transform.right * 2.5f + Vector3.up * 0.5f;
                Vector3 safeDismountPos = candidatePos;

                int mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");
                if (Physics.Raycast(candidatePos + Vector3.up * 3.0f, Vector3.down, out RaycastHit hit, 10.0f, mask))
                {
                    safeDismountPos = hit.point + Vector3.up * 0.15f;
                }

                player.transform.position = safeDismountPos;
                Quaternion safeRot = transform.rotation;
                safeRot.Normalize();
                player.transform.rotation = safeRot;

                Physics.SyncTransforms();
            }

            // 4. Restore Player Control & Camera Offset cleanly
            RestorePlayerControl(player);


            // 5. Validation Logging
            ValidateDismountState(player);
        }

        /// <summary>
        /// Simple, direct input & control restoration method.
        /// Re-enables player controllers and restores camera ownership.
        /// </summary>
        public static void RestorePlayerControl(GameObject player = null)
        {
            ActiveMount = null;

            if (player == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.gameObject;
                else player = GameObject.FindWithTag("Player");
            }

            if (player != null)
            {
                if (player.transform.parent != null)
                {
                    player.transform.SetParent(null);
                }
                player.transform.localScale = Vector3.one;

                CharacterController pCC = player.GetComponent<CharacterController>();
                if (pCC != null)
                {
                    pCC.enabled = true;
                }

                PlayerController pCtrl = player.GetComponent<PlayerController>();
                if (pCtrl != null)
                {
                    pCtrl.enabled = true;
                    pCtrl.ResetVelocity();
                }

            }

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.ForceRestorePlayerCamera("MountSystem.RestorePlayerControl");
            }
            else
            {
                ThirdPersonCamera tpCam = FindFirstObjectByType<ThirdPersonCamera>();
                if (tpCam != null)
                {
                    tpCam.RestorePlayerCameraOffset();
                }
            }
        }

        private void ValidateDismountState(GameObject player)
        {
            Transform parentTransform = player != null ? player.transform.parent : null;
            bool isPlayerMounted = (mountedPlayer != null);
            bool isHorseMounted = (horseController != null && horseController.IsMounted);

            ThirdPersonCamera cam = tpCam != null ? tpCam : FindFirstObjectByType<ThirdPersonCamera>();
            float camHeight = cam != null ? cam.HeightOffset : 0f;
            float camDist = cam != null ? cam.Distance : 0f;


            if (parentTransform != null)
            {
                Debug.LogWarning("[Dismount Validation WARNING] Player transform parent is NOT null! Forcing null parent.");
                player.transform.SetParent(null);
            }

            if (isPlayerMounted)
            {
                Debug.LogWarning("[Dismount Validation WARNING] mountedPlayer is NOT null! Clearing reference.");
                mountedPlayer = null;
            }

            if (ActiveMount != null)
            {
                Debug.LogWarning("[Dismount Validation WARNING] ActiveMount is NOT null! Clearing ActiveMount.");
                ActiveMount = null;
            }

            if (isHorseMounted)
            {
                Debug.LogWarning("[Dismount Validation WARNING] HorseController.IsMounted is STILL true! Forcing false.");
                if (horseController != null) horseController.SetMountedState(false);
            }

            if (cam != null && cam.IsMounted)
            {
                Debug.LogWarning("[Dismount Validation WARNING] ThirdPersonCamera.IsMounted is STILL true! Forcing false.");
                cam.RestorePlayerCameraOffset();
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
                return;
            }

            if (IsPlayerMounted && mountedPlayer != null && ActiveMount == this)
            {
                // Handle Horse Movement Inputs while mounted
                float moveX = Input.GetAxisRaw("Horizontal");
                float moveZ = Input.GetAxisRaw("Vertical");
                Vector3 inputDir = new Vector3(moveX, 0, moveZ);
                bool sprint = Input.GetKey(KeyCode.LeftShift);

                Camera mainCam = Camera.main;
                if (horseController != null)
                {
                    horseController.ProcessMovementInput(inputDir, sprint, mainCam);

                    // Jump Input Forwarding
                    if (Input.GetButtonDown("Jump"))
                    {
                        horseController.TryJump();
                    }
                }

                // Direct Dismount Key (ONLY 'F' key while mounted)
                if (Input.GetKeyDown(KeyCode.F))
                {
                    ForceDismount();
                }
            }
        }

        /// <summary>
        /// Failsafe force dismount that guarantees complete restoration of player transform, movement, and camera ownership.
        /// </summary>
        public void ForceDismount()
        {
            if (IsPlayerMounted)
            {
                DismountPlayer();
            }
            else
            {
                RestorePlayerControl(mountedPlayer);
            }
        }
    }
}
