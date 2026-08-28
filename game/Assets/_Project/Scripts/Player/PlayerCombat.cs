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

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, mask))
            {
                return hit.point;
            }

            return ray.origin + ray.direction * 50f;
        }

        public Vector3 GetReticleAimDirection()
        {
            Vector3 targetPos = GetReticleTargetWorldPosition();
            Vector3 dir = (targetPos - transform.position);
            dir.y = 0;

            if (dir.sqrMagnitude < 0.001f) return transform.forward;
            return dir.normalized;
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

                // Rotate player character toward reticle aim direction
                if (aimDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(aimDir);
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

            float totalDamage = (baseDamage + playerStats.CharacterData.baseAttackDamage) * playerStats.DamageMultiplier;
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
