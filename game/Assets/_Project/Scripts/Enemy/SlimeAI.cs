using System.Collections;
using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Enemy
{
    public class SlimeAI : EnemyBase
    {
        private float attackTimer = 0f;
        private bool isAttacking = false;

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

            attackTimer -= Time.deltaTime;
            float distToPlayer = GetFlatDistanceToPlayer();

            if (distToPlayer > enemyData.attackRange)
            {
                // Move towards player
                Vector3 moveDir = (playerTransform.position - transform.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.Normalize();
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir, Vector3.up), Time.deltaTime * 8f);
                }
                characterController.Move(moveDir * enemyData.moveSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);
            }
            else if (attackTimer <= 0)
            {
                StartCoroutine(PerformSlimeLeapAttack());
            }
        }

        private IEnumerator PerformSlimeLeapAttack()
        {
            isAttacking = true;
            attackTimer = enemyData.attackCooldown;

            Vector3 targetPos = playerTransform.position;
            Vector3 leapDir = (targetPos - transform.position);
            leapDir.y = 0;
            if (leapDir.sqrMagnitude > 0.0001f) leapDir.Normalize();
            else leapDir = transform.forward;

            float leapTime = 0.5f;
            float elapsed = 0f;

            while (elapsed < leapTime)
            {
                if (IsDead) yield break;

                // Move forward & arch up
                float height = Mathf.Sin((elapsed / leapTime) * Mathf.PI) * 2.0f;
                Vector3 leapVelocity = leapDir * (enemyData.moveSpeed * 1.8f);
                leapVelocity.y = height * 4.0f;

                characterController.Move(leapVelocity * Time.deltaTime);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Deal Damage if player is in range at landing
            if (playerStats != null && !playerStats.IsDead)
            {
                float distAtLanding = GetFlatDistanceToPlayer();
                if (distAtLanding <= enemyData.attackRange + 0.8f)
                {
                    Vector3 kbDir = (playerTransform.position - transform.position);
                    kbDir.y = 0;
                    if (kbDir.sqrMagnitude > 0.0001f) kbDir.Normalize();
                    else kbDir = transform.forward;

                    DamageInfo damage = new DamageInfo(
                        enemyData.attackDamage,
                        kbDir,
                        4.0f,
                        false,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }
            }

            isAttacking = false;
        }
    }
}
