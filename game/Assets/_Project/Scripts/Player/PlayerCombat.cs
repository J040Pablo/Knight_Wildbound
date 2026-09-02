using System.Collections;
using UnityEngine;
using Roguelite.Data;
using Roguelite.Combat;
using Roguelite.Enemy;
using Roguelite.Player.Mage;

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

        // Charge State
        private bool isCharging = false;
        private float currentChargeTime = 0f;

        // Class Stat Multipliers (from Upgrades)
        public float MagicDamageMultiplier { get; set; } = 1.0f;
        public float ProjectileSpeedMultiplier { get; set; } = 1.0f;
        public float SpellAreaMultiplier { get; set; } = 1.0f;
        public float HealingEfficiencyMultiplier { get; set; } = 1.0f;

        public bool IsCharging => isCharging;
        public float ChargeRatio => weaponData != null ? Mathf.Clamp01(currentChargeTime / weaponData.chargeTimeRequired) : 0f;
        public float BaseDamage => playerStats != null ? (weaponData.lightDamage + playerStats.CharacterData.baseAttackDamage) * playerStats.DamageMultiplier : 15.0f;

        private MageAimController aimController;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            playerController = GetComponent<PlayerController>();
            aimController = GetComponent<MageAimController>();
            if (aimController == null)
            {
                aimController = gameObject.AddComponent<MageAimController>();
            }

            if (weaponData == null)
            {
                weaponData = ScriptableObject.CreateInstance<WeaponData>();
            }

            InitializeEnemyLayerMask();

            if (GetComponent<SpecialAbilitySystem>() == null)
            {
                gameObject.AddComponent<SpecialAbilitySystem>();
            }

            if (GetComponent<ClassVisuals>() == null)
            {
                gameObject.AddComponent<ClassVisuals>();
            }

            // Default to Knight behavior if none active
            if (activeBehavior == null)
            {
                SetBehavior(new KnightCombatBehavior());
            }
        }

        private void InitializeEnemyLayerMask()
        {
            if (enemyLayerMask == 0 || enemyLayerMask == ~0)
            {
                int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
                if (mask == 0)
                {
                    // Fall back cleanly to all non-player layers without warning spam
                    mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");
                }
                enemyLayerMask = mask;
            }
        }

        public void SetBehavior(ICombatBehavior behavior)
        {
            activeBehavior = behavior;
            if (activeBehavior != null)
            {
                activeBehavior.Initialize(this, playerStats);
            }
        }

        public void SetCombatBehavior(ICombatBehavior behavior)
        {
            SetBehavior(behavior);
        }

        public Vector3 GetReticleTargetWorldPosition()
        {
            if (aimController != null)
            {
                return aimController.GetAimPoint();
            }

            Vector3 originPos = transform.position + Vector3.up * 1.5f;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Ray ray = new Ray(originPos, mainCam.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 25.0f, enemyLayerMask))
                {
                    if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform) && !hit.collider.CompareTag("Player") && hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                    {
                        return hit.point;
                    }
                }
                Vector3 fallbackTarget = originPos + mainCam.transform.forward * 25.0f;
                return fallbackTarget;
            }
            return originPos + transform.forward * 25.0f;
        }

        public Vector3 GetGroundReticleTargetWorldPosition()
        {
            if (aimController != null)
            {
                return aimController.GetGroundAimPoint();
            }

            Vector3 target = GetReticleTargetWorldPosition();
            if (Physics.Raycast(target + Vector3.up * 10.0f, Vector3.down, out RaycastHit hit, 25.0f, enemyLayerMask))
            {
                return hit.point;
            }
            return target;
        }

        public Vector3 GetReticleAimDirection()
        {
            if (aimController != null)
            {
                return aimController.GetAimDirection();
            }

            Vector3 targetPt = GetReticleTargetWorldPosition();
            Vector3 originPos = transform.position + Vector3.up * 1.5f;
            Vector3 dir = (targetPt - originPos);
            if (dir.sqrMagnitude < 0.0001f)
            {
                return transform.forward;
            }
            return dir.normalized;
        }

        private void Update()
        {
            if (playerStats == null || playerStats.IsDead)
            {
                CancelCharge();
                return;
            }

            // Ignore input while UI is active
            if (UI.MasteryScreenUI.IsAnyMenuOpen || (Core.InputStateManager.Instance != null && Core.InputStateManager.Instance.CurrentMode == Core.InputMode.UI))
            {
                CancelCharge();
                return;
            }

            // Update Class Behavior
            activeBehavior?.UpdateBehavior();

            // LMB = Basic Attack, RMB = Charged Attack
            if (Input.GetMouseButtonDown(0))
            {
                ExecuteBasicAttack();
            }

            if (Input.GetMouseButtonDown(1))
            {
                StartCharge();
            }

            if (isCharging)
            {
                UpdateCharge();

                if (Input.GetMouseButtonUp(1))
                {
                    ExecuteChargedAttack();
                }
            }
        }

        private void ExecuteBasicAttack()
        {
            if (playerStats.CurrentStamina < weaponData.lightStaminaCost) return;
            playerStats.ConsumeStamina(weaponData.lightStaminaCost);

            Vector3 aimDir = GetReticleAimDirection();
            AlignPlayerWithAim(aimDir);

            activeBehavior?.ExecuteBasicAttack(aimDir);
        }

        private void StartCharge()
        {
            if (playerStats.CurrentStamina < weaponData.chargedStaminaCost) return;
            isCharging = true;
            currentChargeTime = 0f;
        }

        private void UpdateCharge()
        {
            currentChargeTime += Time.deltaTime;
            float ratio = ChargeRatio;

            if (activeBehavior is MageCombatBehavior mageBehavior)
            {
                mageBehavior.UpdateChargeFeedback(ratio);
            }
        }

        private void ExecuteChargedAttack()
        {
            if (!isCharging) return;

            float ratio = ChargeRatio;
            playerStats.ConsumeStamina(weaponData.chargedStaminaCost);

            Vector3 aimDir = GetReticleAimDirection();
            AlignPlayerWithAim(aimDir);

            if (activeBehavior is MageCombatBehavior mageBehavior)
            {
                mageBehavior.StopChargeFeedback();
            }

            activeBehavior?.ExecuteChargedAttack(aimDir, ratio);
            isCharging = false;
            currentChargeTime = 0f;
        }

        public void CancelCharge()
        {
            if (isCharging)
            {
                if (activeBehavior is MageCombatBehavior mageBehavior)
                {
                    mageBehavior.StopChargeFeedback();
                }
                isCharging = false;
                currentChargeTime = 0f;
            }
        }

        private void AlignPlayerWithAim(Vector3 aimDir)
        {
            aimDir.y = 0;
            if (aimDir.sqrMagnitude > 0.001f)
            {
                Vector3 normDir = aimDir.normalized;
                Vector3 safeUp = Mathf.Abs(Vector3.Dot(normDir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
                Quaternion rot = Quaternion.LookRotation(normDir, safeUp);
                rot.Normalize();
                transform.rotation = rot;
            }
        }

        public void PerformSweepAttack(float range, float arcAngle, float damage, float knockbackForce)
        {
            InitializeEnemyLayerMask();

            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Collider[] hits = Physics.OverlapSphere(origin, range, enemyLayerMask);

            foreach (var col in hits)
            {
                if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform) || col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    continue; // Skip self/player colliders
                }

                Vector3 dirToTarget = (col.transform.position - transform.position).normalized;
                dirToTarget.y = 0;

                float angle = Vector3.Angle(transform.forward, dirToTarget);
                if (angle <= arcAngle * 0.5f)
                {
                    EnemyBase enemy = col.GetComponent<EnemyBase>();
                    if (enemy == null) enemy = col.GetComponentInParent<EnemyBase>();

                    if (enemy != null && !enemy.IsDead)
                    {
                        DamageInfo info = new DamageInfo(damage, dirToTarget, knockbackForce, false, gameObject);
                        enemy.TakeDamage(info);
                    }
                }
            }
        }
    }
}
