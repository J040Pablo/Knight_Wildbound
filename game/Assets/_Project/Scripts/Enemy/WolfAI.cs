using System.Collections;
using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Enemy
{
    public class WolfAI : EnemyBase
    {
        private float attackTimer = 0f;
        private bool isCharging = false;

        protected override void Update()
        {
            base.Update();
            if (IsDead || playerTransform == null || playerStats.IsDead || isCharging) return;

            attackTimer -= Time.deltaTime;
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distToPlayer > enemyData.attackRange + 3.0f)
            {
                // Fast approach
                Vector3 moveDir = (playerTransform.position - transform.position).normalized;
                moveDir.y = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 12f);
                characterController.Move(moveDir * enemyData.moveSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);
            }
            else if (attackTimer <= 0)
            {
                StartCoroutine(PerformWolfCharge());
            }
        }

        private IEnumerator PerformWolfCharge()
        {
            isCharging = true;
            attackTimer = enemyData.attackCooldown;

            // Wind-up: Face player and pause
            Vector3 chargeDir = (playerTransform.position - transform.position).normalized;
            chargeDir.y = 0;
            if (chargeDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(chargeDir);
            }

            // Wind-up telegraph pause
            yield return new WaitForSeconds(0.4f);

            // Fast Charge forward
            float chargeDuration = 0.5f;
            float elapsed = 0f;
            float chargeSpeed = enemyData.moveSpeed * 2.5f;
            bool hitPlayer = false;

            while (elapsed < chargeDuration)
            {
                if (IsDead) yield break;

                characterController.Move(chargeDir * chargeSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);

                if (!hitPlayer && playerStats != null && !playerStats.IsDead)
                {
                    if (Vector3.Distance(transform.position, playerTransform.position) < 1.8f)
                    {
                        hitPlayer = true;
                        DamageInfo damage = new DamageInfo(
                            enemyData.attackDamage,
                            chargeDir,
                            8.0f,
                            false,
                            gameObject
                        );
                        playerStats.TakeDamage(damage);
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            isCharging = false;
        }
    }
}
