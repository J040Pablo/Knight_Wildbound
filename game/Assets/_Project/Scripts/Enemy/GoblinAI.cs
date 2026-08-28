using System.Collections;
using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Enemy
{
    public class GoblinAI : EnemyBase
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
                Vector3 moveDir = (playerTransform.position - transform.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.Normalize();
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir, Vector3.up), Time.deltaTime * 10f);
                }
                characterController.Move(moveDir * enemyData.moveSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);
            }
            else if (attackTimer <= 0)
            {
                StartCoroutine(PerformGoblinSlash());
            }
        }

        private IEnumerator PerformGoblinSlash()
        {
            isAttacking = true;
            attackTimer = enemyData.attackCooldown;

            // Telegraph pause (0.3s)
            yield return new WaitForSeconds(0.3f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float distToPlayer = GetFlatDistanceToPlayer();
                if (distToPlayer <= enemyData.attackRange + 0.8f)
                {
                    Vector3 knockbackDir = (playerTransform.position - transform.position);
                    knockbackDir.y = 0;
                    if (knockbackDir.sqrMagnitude > 0.0001f) knockbackDir.Normalize();
                    else knockbackDir = transform.forward;

                    DamageInfo damage = new DamageInfo(
                        enemyData.attackDamage,
                        knockbackDir,
                        5.0f,
                        false,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }
            }

            yield return new WaitForSeconds(0.2f);
            isAttacking = false;
        }
    }
}
