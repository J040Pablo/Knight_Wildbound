using System;
using System.Collections;
using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Enemy
{
    public class BossAI : EnemyBase
    {
        [Header("Boss Specifics")]
        [SerializeField] private float groundSlamRadius = 6.0f;
        [SerializeField] private float groundSlamDamage = 35.0f;
        [SerializeField] private float specialAttackCooldown = 6.0f;

        private float meleeAttackTimer = 0f;
        private float specialTimer = 3.0f;
        private bool isAttacking = false;
        private bool isEnraged = false;

        public bool IsEnraged => isEnraged;
        public event Action<float, float> OnBossHealthChanged;

        protected override void Awake()
        {
            base.Awake();
            if (MaxHP < 200f)
            {
                MaxHP = 500f;
                CurrentHP = MaxHP;
            }
        }

        protected override void Start()
        {
            base.Start();
            OnBossHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        private float GetFlatDistanceToPlayer()
        {
            if (playerTransform == null) return 999f;
            Vector3 pPos = playerTransform.position;
            Vector3 ePos = transform.position;
            return Vector2.Distance(new Vector2(ePos.x, ePos.z), new Vector2(pPos.x, pPos.z));
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || playerTransform == null || playerStats.IsDead || isAttacking) return;

            // Check Phase 2 Enrage
            if (!isEnraged && CurrentHP <= MaxHP * 0.5f)
            {
                EnterPhase2Enrage();
            }

            float currentSpeed = isEnraged ? enemyData.moveSpeed * 1.4f : enemyData.moveSpeed;

            meleeAttackTimer -= Time.deltaTime;
            specialTimer -= Time.deltaTime;

            float distToPlayer = GetFlatDistanceToPlayer();

            // Special AoE Attack Priority
            if (specialTimer <= 0)
            {
                StartCoroutine(PerformGroundSlamAoE());
                return;
            }

            if (distToPlayer > enemyData.attackRange + 0.5f)
            {
                Vector3 moveDir = (playerTransform.position - transform.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.Normalize();
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir, Vector3.up), Time.deltaTime * 6f);
                }
                characterController.Move(moveDir * currentSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);
            }
            else if (meleeAttackTimer <= 0)
            {
                StartCoroutine(PerformHeavyCleave());
            }
        }

        public override void TakeDamage(DamageInfo damageInfo)
        {
            base.TakeDamage(damageInfo);
            OnBossHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        private void EnterPhase2Enrage()
        {
            isEnraged = true;
            if (meshRenderer != null)
            {
                originalColor = new Color(0.85f, 0.1f, 0.1f);
                meshRenderer.material.color = originalColor;
            }
        }

        private IEnumerator PerformHeavyCleave()
        {
            isAttacking = true;
            meleeAttackTimer = isEnraged ? enemyData.attackCooldown * 0.65f : enemyData.attackCooldown;

            // Wind-up pause
            yield return new WaitForSeconds(0.4f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= enemyData.attackRange + 1.2f)
                {
                    DamageInfo damage = new DamageInfo(
                        enemyData.attackDamage * (isEnraged ? 1.3f : 1.0f),
                        (playerTransform.position - transform.position).normalized,
                        10.0f,
                        false,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }
            }

            yield return new WaitForSeconds(0.3f);
            isAttacking = false;
        }

        private IEnumerator PerformGroundSlamAoE()
        {
            isAttacking = true;
            specialTimer = isEnraged ? specialAttackCooldown * 0.7f : specialAttackCooldown;

            // Spawn ground telegraph indicator circle
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "BossTelegraphIndicator";
            Destroy(indicator.GetComponent<Collider>());
            indicator.transform.position = transform.position + new Vector3(0, 0.05f, 0);
            indicator.transform.localScale = new Vector3(groundSlamRadius * 2f, 0.02f, groundSlamRadius * 2f);

            Renderer indRenderer = indicator.GetComponent<Renderer>();
            if (indRenderer != null)
            {
                indRenderer.material.color = new Color(1.0f, 0.1f, 0.1f, 0.5f);
            }

            // Telegraph expansion duration: 1.2 seconds
            float telegraphTime = 1.2f;
            float elapsed = 0f;

            while (elapsed < telegraphTime)
            {
                if (IsDead)
                {
                    Destroy(indicator);
                    yield break;
                }

                float scaleRatio = Mathf.Lerp(0.1f, groundSlamRadius * 2f, elapsed / telegraphTime);
                indicator.transform.localScale = new Vector3(scaleRatio, 0.02f, scaleRatio);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(indicator);

            // Ground Slam Impact!
            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distToPlayer <= groundSlamRadius)
                {
                    Vector3 knockbackDir = (playerTransform.position - transform.position).normalized;
                    knockbackDir.y = 0.5f; // Lift up
                    DamageInfo damage = new DamageInfo(
                        groundSlamDamage * (isEnraged ? 1.3f : 1.0f),
                        knockbackDir,
                        15.0f,
                        true,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, groundSlamRadius);
        }
    }
}
