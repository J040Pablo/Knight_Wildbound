using System.Collections;
using UnityEngine;

namespace Roguelite.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] private ThirdPersonCamera cam;

        private CharacterController characterController;
        private PlayerStats playerStats;

        // Gravity & Velocity
        private Vector3 verticalVelocity;
        private Vector3 externalKnockback;
        private const float GRAVITY = -19.6f;

        // State Flags
        public bool IsSprinting { get; private set; }
        public bool IsDodging { get; private set; }
        public bool IsGrounded => characterController.isGrounded;

        private float dodgeTimer = 0f;

        private void Awake()
        {
            Quaternion rot = transform.rotation;
            float sqrMag = rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w;
            if (sqrMag < 0.001f)
            {
                transform.rotation = Quaternion.identity;
            }
            characterController = GetComponent<CharacterController>();
            playerStats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            if (cam == null)
            {
                cam = FindFirstObjectByType<ThirdPersonCamera>();
            }
        }

        [Header("Debug Controls")]
        [SerializeField] private bool enableDebugLogs = false;

        private void Update()
        {
            if (playerStats.IsDead) return;

#if UNITY_EDITOR
            if (enableDebugLogs) Debug.Log($"[PLAYER_UPDATE] START pos: {transform.position}");
#endif

            // Ignore gameplay input while menu is open or in UI mode
            if (UI.MasteryScreenUI.IsAnyMenuOpen || (Roguelite.Core.InputStateManager.Instance != null && Roguelite.Core.InputStateManager.Instance.CurrentMode == Roguelite.Core.InputMode.UI))
            {
                return;
            }

            dodgeTimer -= Time.deltaTime;

            HandleGroundedState();
            HandleDodgeInput();

            if (!IsDodging)
            {
                HandleMovement();
                HandleJumpInput();
            }

            ApplyKnockbackDecay();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleDebugClassSelectShortcuts();
            if (enableDebugLogs) Debug.Log($"[PLAYER_UPDATE] END pos: {transform.position}");
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleDebugClassSelectShortcuts()
        {
            if (Roguelite.Progression.ProgressionManager.Instance == null) return;
            if (Roguelite.Progression.ProgressionManager.Instance.CurrentClass != Roguelite.Progression.ClassType.None) return;

            Roguelite.Core.CharacterType selectedType = Roguelite.Core.CharacterType.Knight;
            bool trigger = false;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                selectedType = Roguelite.Core.CharacterType.Knight;
                trigger = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                selectedType = Roguelite.Core.CharacterType.Mage;
                trigger = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                selectedType = Roguelite.Core.CharacterType.Druid;
                trigger = true;
            }

            if (trigger)
            {
                Debug.Log($"[DEBUG_FALLBACK] Class selected via key shortcut: {selectedType}");
                Roguelite.Progression.ClassType pClass = selectedType switch
                {
                    Roguelite.Core.CharacterType.Mage => Roguelite.Progression.ClassType.Mage,
                    Roguelite.Core.CharacterType.Druid => Roguelite.Progression.ClassType.Druid,
                    _ => Roguelite.Progression.ClassType.Knight
                };

                Roguelite.Progression.ProgressionManager.Instance.SetClass(pClass);
                if (Roguelite.Core.GameSessionManager.Instance != null)
                {
                    Roguelite.Core.GameSessionManager.Instance.SelectedCharacter = selectedType;
                    Roguelite.Core.GameSessionManager.Instance.HasSelectedCharacter = true;
                }

                Roguelite.Environment.WeaponInteractable.SetupPlayerClassVisualsAndBehavior(gameObject, selectedType, GetComponent<PlayerCombat>(), GetComponent<PlayerStats>());

                var allWeapons = FindObjectsByType<Roguelite.Environment.WeaponInteractable>(FindObjectsSortMode.None);
                foreach (var weapon in allWeapons)
                {
                    weapon.gameObject.SetActive(false);
                }

                var gate = FindFirstObjectByType<Roguelite.Environment.RuinsExitGate>();
                if (gate != null) gate.UnlockGate();
            }
        }
#endif

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit == null || hit.collider == null) return;

#if UNITY_EDITOR
            if (enableDebugLogs && hit.normal.y < 0.7f)
            {
                Debug.Log($"[COLLISION DEBUG] Name={hit.collider.name} | Tag={hit.collider.tag} | Layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }

            if (enableDebugLogs && hit.gameObject != null)
            {
                Debug.LogWarning($"[PLAYER_HIT] Collided with: '{hit.gameObject.name}', Tag: '{hit.gameObject.tag}', Layer: '{LayerMask.LayerToName(hit.gameObject.layer)}', HitPoint: {hit.point}");
            }
#endif
        }

        private void HandleGroundedState()
        {
            if (characterController.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f; // Snap to ground
            }
        }

        private void HandleMovement()
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(moveX, 0, moveZ).normalized;

            // Handle Sprint
            bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && inputDir.magnitude > 0.1f;
            IsSprinting = wantsSprint && playerStats.ConsumeStamina(15f * Time.deltaTime);

            float speedMultiplier = playerStats.CharacterData.baseMoveSpeed * playerStats.MoveSpeedMultiplier;
            if (IsSprinting)
            {
                speedMultiplier *= playerStats.CharacterData.sprintSpeedMultiplier;
            }

            Vector3 moveDirection = Vector3.zero;

            if (inputDir.magnitude > 0.1f)
            {
                Vector3 forward = cam != null ? cam.GetForwardVector() : transform.forward;
                Vector3 right = cam != null ? cam.GetRightVector() : transform.right;

                moveDirection = (forward * inputDir.z + right * inputDir.x).normalized;

                if (moveDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
                    targetRotation.Normalize();
                    Quaternion slerped = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12f);
                    slerped.Normalize();
                    transform.rotation = slerped;
                }
            }

            // Apply gravity
            verticalVelocity.y += GRAVITY * Time.deltaTime;

            // Combine final displacement
            Vector3 finalMove = (moveDirection * speedMultiplier) + verticalVelocity + externalKnockback;
            if (SafeCanMove())
            {
                characterController.Move(finalMove * Time.deltaTime);
            }
        }

        private void HandleJumpInput()
        {
            if (Input.GetButtonDown("Jump") && SafeCanMove() && characterController.isGrounded)
            {
                if (playerStats.ConsumeStamina(10f))
                {
                    verticalVelocity.y = playerStats.CharacterData.jumpForce;
                }
            }
        }

        private void HandleDodgeInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftAlt))
            {
                if (dodgeTimer <= 0 && !IsDodging && playerStats.ConsumeStamina(playerStats.CharacterData.dodgeStaminaCost))
                {
                    StartCoroutine(PerformDodgeRoll());
                }
            }
        }

        private IEnumerator PerformDodgeRoll()
        {
            IsDodging = true;
            playerStats.IsInvulnerable = true;
            dodgeTimer = playerStats.CharacterData.dodgeCooldown;

            Vector3 dodgeDir = transform.forward;
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f)
            {
                Vector3 forward = cam != null ? cam.GetForwardVector() : transform.forward;
                Vector3 right = cam != null ? cam.GetRightVector() : transform.right;
                dodgeDir = (forward * moveZ + right * moveX).normalized;
                if (dodgeDir.sqrMagnitude > 0.001f)
                {
                    Quaternion rot = Quaternion.LookRotation(dodgeDir.normalized, Vector3.up);
                    rot.Normalize();
                    transform.rotation = rot;
                }
            }

            // Check for Wind Dash override (Helmet N3)
            bool isWindDash = Progression.ProgressionManager.Instance != null &&
                              Progression.ProgressionManager.Instance.GetTier(Progression.MasteryPath.Path1) >= Progression.MasteryTier.N3;

            float duration = isWindDash ? 0.3f : 0.4f;
            float elapsed = 0f;
            float baseDistance = playerStats.CharacterData.dodgeDistance * (isWindDash ? 1.4f : 1.0f);
            float speed = baseDistance / duration;

            while (elapsed < duration)
            {
                if (!SafeCanMove()) yield break;
                characterController.Move(dodgeDir * speed * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            playerStats.IsInvulnerable = false;
            IsDodging = false;
        }

        public void ResetVelocity()
        {
            verticalVelocity = Vector3.zero;
            externalKnockback = Vector3.zero;
        }

        public void ApplyKnockback(Vector3 force)
        {
            externalKnockback += force;
        }

        private bool SafeCanMove()
        {
            return characterController != null && characterController.enabled && characterController.gameObject.activeInHierarchy;
        }

        private void ApplyKnockbackDecay()
        {
            if (externalKnockback.magnitude > 0.1f)
            {
                if (SafeCanMove())
                {
                    characterController.Move(externalKnockback * Time.deltaTime);
                }
                externalKnockback = Vector3.Lerp(externalKnockback, Vector3.zero, Time.deltaTime * 8f);
            }
            else
            {
                externalKnockback = Vector3.zero;
            }
        }
    }
}
