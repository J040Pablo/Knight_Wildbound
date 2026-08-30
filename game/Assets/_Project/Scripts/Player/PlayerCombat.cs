using System.Collections;
using UnityEngine;
using Roguelite.Data;
using Roguelite.Combat;

namespace Roguelite.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Weapon Data")]
        [SerializeField] private WeaponData weaponData;

        [Header("Hit Detection Layers")]
        [SerializeField] private LayerMask enemyLayerMask;

        private PlayerStats playerStats;
        private PlayerController playerController;
        private ICombatBehavior activeBehavior;

        private float lightAttackTimer = 0f;
        private float currentChargeTime = 0f;
        private bool isCharging = false;

        // Class Stat Multipliers (from Upgrades)
        public float MagicDamageMultiplier { get; set; } = 1.0f;
        public float ProjectileSpeedMultiplier { get; set; } = 1.0f;
        public float SpellAreaMultiplier { get; set; } = 1.0f;
        public float HealingEfficiencyMultiplier { get; set; } = 1.0f;

        public bool IsCharging => isCharging;
        public float ChargeRatio => weaponData != null ? Mathf.Clamp01(currentChargeTime / weaponData.chargeTimeRequired) : 0f;
        public float BaseDamage => playerStats != null ? (weaponData.lightDamage + playerStats.CharacterData.baseAttackDamage) * playerStats.DamageMultiplier : 15.0f;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            playerController = GetComponent<PlayerController>();

            if (weaponData == null)
            {
                weaponData = ScriptableObject.CreateInstance<WeaponData>();
            }

            if (enemyLayerMask == 0)
            {
                enemyLayerMask = ~0; // Default to everything if unassigned
            }

            if (GetComponent<SpecialAbilitySystem>() == null)
            {
                gameObject.AddComponent<SpecialAbilitySystem>();
            }

            if (GetComponent<ClassVisuals>() == null)
            {
                gameObject.AddComponent<ClassVisuals>();
            }
        }

        public void SetCombatBehavior(ICombatBehavior behavior)
        {
            activeBehavior = behavior;
            if (activeBehavior != null)
            {
                activeBehavior.Initialize(this, playerStats);
            }
        }

        private void Update()
        {
            if (playerStats.IsDead || (playerController != null && playerController.IsDodging)) return;
            if (UI.MasteryScreenUI.IsAnyMenuOpen || (Roguelite.Core.InputStateManager.Instance != null && Roguelite.Core.InputStateManager.Instance.CurrentMode == Roguelite.Core.InputMode.UI)) return;

            if (activeBehavior == null)
            {
                // Fallback to Knight if unassigned
                SetCombatBehavior(new KnightCombatBehavior());
            }

            activeBehavior?.UpdateBehavior();

            lightAttackTimer -= Time.deltaTime;

            HandleCombatInputs();
        }

        public Vector3 GetReticleTargetWorldPosition()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return transform.position + transform.forward * 10f;

            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            LayerMask mask = ~LayerMask.GetMask("Ignore Raycast", "UI", "Player", "Water");

            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, mask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Vector3 playerPos = transform.position;
            Vector3 camToPlayer = playerPos - mainCam.transform.position;
            float playerDistAlongRay = Vector3.Dot(camToPlayer, mainCam.transform.forward);

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (IsIgnoredAimCollider(hit.collider)) continue;

                // Ignore hits behind or too close to the player along the camera ray
                if (hit.distance < playerDistAlongRay - 0.2f) continue;

                // Filter out non-enemy close hits (<3m) so nearby props/ground don't skew reticle angle
                float distToPlayer = Vector3.Distance(hit.point, playerPos);
                if (distToPlayer < 3.0f)
                {
                    bool isEnemy = hit.collider.GetComponent<Roguelite.Combat.IDamageable>() != null || hit.collider.GetComponentInParent<Roguelite.Combat.IDamageable>() != null;
                    if (!isEnemy) continue;
                }

                return hit.point;
            }

            return ray.origin + ray.direction * 60f;
        }

        public Vector3 GetReticleAimDirection()
        {
            Camera mainCam = Camera.main;
            Vector3 camForward = mainCam != null ? mainCam.transform.forward : transform.forward;
            if (camForward.sqrMagnitude < 0.0001f) camForward = transform.forward;
            camForward.Normalize();

            Vector3 targetPos = GetReticleTargetWorldPosition();
            MountSystem mount = GetComponent<MountSystem>();
            if (mount == null) mount = GetComponentInParent<MountSystem>();

            Vector3 origin = (mount != null && mount.IsPlayerMounted) ? transform.position + Vector3.up * 2.2f : transform.position + Vector3.up * 1.2f;
            Vector3 dir = (targetPos - origin);

            // Absolute safety check: If direction points backwards relative to camera view (dot < 0), fallback to camera forward
            if (dir.sqrMagnitude < 0.0001f || Vector3.Dot(dir.normalized, camForward) < 0.0f)
            {
                return camForward;
            }

            return dir.normalized;
        }

        private bool IsIgnoredAimCollider(Collider col)
        {
            if (col == null) return true;
            if (col.CompareTag("Player")) return true;
            string n = col.gameObject.name.ToLower();
            if (n.Contains("player") || n.Contains("horse") || n.Contains("saddle")) return true;
            if (col.transform == transform || col.transform.IsChildOf(transform) || transform.IsChildOf(col.transform)) return true;
            return false;
        }

        private void HandleCombatInputs()
        {
            // Attack Button (Mouse0 or Keycode J or K)
            bool attackPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J);
            bool attackHeld = Input.GetMouseButton(0) || Input.GetKey(KeyCode.J);
            bool attackReleased = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.J);

            if (attackPressed && lightAttackTimer <= 0 && !isCharging)
            {
                isCharging = true;
                currentChargeTime = 0f;
            }

            if (isCharging && attackHeld)
            {
                currentChargeTime += Time.deltaTime;
            }

            if (isCharging && attackReleased)
            {
                Vector3 aimDir = GetReticleAimDirection();

                // Rotate player character horizontally toward reticle aim direction
                Vector3 horizontalAimDir = new Vector3(aimDir.x, 0, aimDir.z);
                if (horizontalAimDir.sqrMagnitude > 0.0001f)
                {
                    horizontalAimDir.Normalize();
                    Quaternion rot = Quaternion.LookRotation(horizontalAimDir, Vector3.up);
                    rot.Normalize();
                    transform.rotation = rot;
                }

                if (currentChargeTime >= weaponData.chargeTimeRequired)
                {
                    ExecuteChargedAttack(aimDir);
                }
                else
                {
                    ExecuteLightAttack(aimDir);
                }

                isCharging = false;
                currentChargeTime = 0f;
            }
        }

        private void ExecuteLightAttack(Vector3 aimDir)
        {
            if (!playerStats.ConsumeStamina(weaponData.lightStaminaCost)) return;

            float cooldown = weaponData.lightAttackCooldown / playerStats.AttackSpeedMultiplier;
            lightAttackTimer = cooldown;

            if (activeBehavior != null)
            {
                activeBehavior.ExecuteBasicAttack(aimDir);
            }
            else
            {
                PerformSweepAttack(weaponData.lightAttackRange, weaponData.lightAttackAngle, weaponData.lightDamage, weaponData.lightKnockbackForce, false);
            }
        }

        private void ExecuteChargedAttack(Vector3 aimDir)
        {
            if (!playerStats.ConsumeStamina(weaponData.chargedStaminaCost)) return;

            float cooldown = weaponData.lightAttackCooldown * 1.5f / playerStats.AttackSpeedMultiplier;
            lightAttackTimer = cooldown;

            if (activeBehavior != null)
            {
                activeBehavior.ExecuteChargedAttack(aimDir, ChargeRatio);
            }
            else
            {
                PerformSweepAttack(weaponData.chargedAttackRange, weaponData.chargedAttackAngle, weaponData.chargedDamage, weaponData.chargedKnockbackForce, true);
            }
        }

        private void PerformSweepAttack(float range, float angle, float baseDamage, float knockbackForce, bool isCharged)
        {
            // Find all potential targets in radius
            Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayerMask);

            float totalDamage = (baseDamage + playerStats.CharacterData.baseAttackDamage + playerStats.FlatDamageBonus) * playerStats.DamageMultiplier;
            bool isCrit = Random.value <= (playerStats.CharacterData.baseCritChance + playerStats.CritChanceBonus);

            if (isCrit)
            {
                totalDamage *= playerStats.CharacterData.critDamageMultiplier;
            }

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;

                Vector3 dirToTarget = (col.transform.position - transform.position).normalized;
                dirToTarget.y = 0;

                float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

                if (angleToTarget <= angle * 0.5f)
                {
                    IDamageable damageable = col.GetComponent<IDamageable>();
                    if (damageable == null)
                    {
                        damageable = col.GetComponentInParent<IDamageable>();
                    }

                    if (damageable != null && !damageable.IsDead)
                    {
                        DamageInfo info = new DamageInfo(
                            totalDamage,
                            dirToTarget,
                            knockbackForce,
                            isCrit,
                            gameObject
                        );
                        damageable.TakeDamage(info);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (weaponData == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, weaponData.lightAttackRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, weaponData.chargedAttackRange);
        }
    }
}
