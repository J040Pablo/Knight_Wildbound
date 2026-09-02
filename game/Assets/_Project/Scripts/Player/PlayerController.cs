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
        public bool IsGrounded => characterController != null && characterController.isGrounded;

        private float dodgeTimer = 0f;

        // Shadow Helm (Knight Helmet N3 passive) tuning.
        private const float SHADOW_HELM_COOLDOWN_BONUS = 0.2f; // small cooldown tax vs. a normal dodge
        private const float SHADOW_HELM_SETTLE_TIME = 0.12f;   // brief i-frame window around the blink

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

            if (Roguelite.Core.CameraManager.Instance != null)
            {
                Roguelite.Core.CameraManager.Instance.RegisterPlayer(transform);
            }
        }

        [Header("Debug Controls")]
        [SerializeField] private bool enableDebugLogs = false;

        private void Update()
        {
            if (playerStats == null || playerStats.IsDead) return;

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

        private void LateUpdate()
        {
            // Defensive recovery watchdog: prevent player scale locking or orphaned parent lock
            if (transform.localScale.sqrMagnitude < 0.01f)
            {
                transform.localScale = Vector3.one;
            }

            if (transform.parent != null)
            {
                MountSystem mount = FindFirstObjectByType<MountSystem>();
                Enemy.TitanClimbNode climb = FindFirstObjectByType<Enemy.TitanClimbNode>();

                bool isLegitimatelyMounted = (mount != null && mount.IsPlayerMounted) || (climb != null && climb.IsMounted);
                if (!isLegitimatelyMounted)
                {
                    transform.SetParent(null, true);
                    transform.localScale = Vector3.one;
                    if (characterController != null && !characterController.enabled)
                    {
                        characterController.enabled = true;
                    }
                }
            }
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
                // Debug.Log($"[DEBUG_FALLBACK] Class selected via key shortcut: {selectedType}");
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
                // Debug.Log($"[COLLISION DEBUG] Name={hit.collider.name} | Tag={hit.collider.tag} | Layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }

            if (enableDebugLogs && hit.gameObject != null)
            {
                // Debug.LogWarning($"[PLAYER_HIT] Collided with: '{hit.gameObject.name}', Tag: '{hit.gameObject.tag}', Layer: '{LayerMask.LayerToName(hit.gameObject.layer)}', HitPoint: {hit.point}");
            }
#endif
        }

        private void HandleGroundedState()
        {
            if (characterController != null && characterController.isGrounded && verticalVelocity.y < 0)
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
                    Vector3 normMove = moveDirection.normalized;
                    Vector3 safeUp = Mathf.Abs(Vector3.Dot(normMove, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
                    Quaternion targetRotation = Quaternion.LookRotation(normMove, safeUp);
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
                    Vector3 normDodge = dodgeDir.normalized;
                    Vector3 safeUp = Mathf.Abs(Vector3.Dot(normDodge, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
                    Quaternion rot = Quaternion.LookRotation(normDodge, safeUp);
                    rot.Normalize();
                    transform.rotation = rot;
                }
            }

            // Check for Shadow Helm override (Helmet N3) — replaces the old "Wind Dash" passive.
            // Dodge Roll becomes a short-range teleport instead of a faster/longer slide.
            bool isShadowHelm = Progression.ProgressionManager.Instance != null &&
                                Progression.ProgressionManager.Instance.GetTier(Progression.MasteryPath.Path1) >= Progression.MasteryTier.N3;

            if (isShadowHelm)
            {
                // Shadow Helm applies its own small cooldown tax (see below) — override the
                // base cooldown that was already set above before this branch runs.
                dodgeTimer = playerStats.CharacterData.dodgeCooldown + SHADOW_HELM_COOLDOWN_BONUS;
                yield return PerformShadowHelmTeleport(dodgeDir);
                playerStats.IsInvulnerable = false;
                IsDodging = false;
                yield break;
            }

            float duration = 0.4f;
            float elapsed = 0f;
            float baseDistance = playerStats.CharacterData.dodgeDistance;
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

        /// <summary>
        /// Shadow Helm (Knight Helmet N3 passive): blinks the player directly to a point
        /// "dodgeDistance" away in the chosen direction using a single CharacterController.Move
        /// sweep (so it still can't pass through walls/geometry), with a shadow-puff VFX at both
        /// the origin and landing spot and a short settle window that keeps the existing full
        /// invulnerability coverage around the blink.
        /// </summary>
        private IEnumerator PerformShadowHelmTeleport(Vector3 dodgeDir)
        {
            float distance = playerStats.CharacterData.dodgeDistance; // same distance as a normal dodge

            KnightVFXHelper.CreateShadowPuff(transform.position, 1.0f, 0.35f);

            if (SafeCanMove())
            {
                characterController.Move(dodgeDir * distance);
            }

            // Small settle window so the teleport reads as a deliberate beat rather than an
            // instant jump-cut, and keeps a moment of i-frames on both sides of the blink.
            yield return new WaitForSeconds(SHADOW_HELM_SETTLE_TIME);

            KnightVFXHelper.CreateShadowPuff(transform.position, 1.0f, 0.35f);
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
