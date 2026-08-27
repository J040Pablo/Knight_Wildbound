using System.Collections;
using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Enemy
{
    public class GoblinAI : EnemyBase
    {
        private float attackTimer = 0f;
        private bool isAttacking = false;

        protected override void Update()
        {
            base.Update();
            if (IsDead || playerTransform == null || playerStats.IsDead || isAttacking) return;

            attackTimer -= Time.deltaTime;
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distToPlayer > enemyData.attackRange)
            {
                Vector3 moveDir = (playerTransform.position - transform.position).normalized;
                moveDir.y = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
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
                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distToPlayer <= enemyData.attackRange + 0.5f)
                {
                    Vector3 knockbackDir = (playerTransform.position - transform.position).normalized;
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
