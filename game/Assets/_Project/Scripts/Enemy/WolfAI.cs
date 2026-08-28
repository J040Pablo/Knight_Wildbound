using System.Collections;
using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Enemy
{
    public class WolfAI : EnemyBase
    {
        private float attackTimer = 0f;
        private bool isCharging = false;

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
            if (IsDead || playerTransform == null || playerStats.IsDead || isCharging) return;

            attackTimer -= Time.deltaTime;
            float distToPlayer = GetFlatDistanceToPlayer();

            if (distToPlayer > enemyData.attackRange + 3.0f)
            {
                // Fast approach
                Vector3 moveDir = (playerTransform.position - transform.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.Normalize();
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir, Vector3.up), Time.deltaTime * 12f);
                }
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
            Vector3 chargeDir = (playerTransform.position - transform.position);
            chargeDir.y = 0;
            if (chargeDir.sqrMagnitude > 0.0001f)
            {
                chargeDir.Normalize();
                transform.rotation = Quaternion.LookRotation(chargeDir, Vector3.up);
            }
            else
            {
                chargeDir = transform.forward;
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
                    if (GetFlatDistanceToPlayer() < 2.2f)
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
