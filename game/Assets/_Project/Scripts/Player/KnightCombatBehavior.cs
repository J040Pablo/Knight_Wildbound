using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player
{
    public class KnightCombatBehavior : ICombatBehavior
    {
        private PlayerCombat playerCombat;
        private PlayerStats playerStats;

        public void Initialize(PlayerCombat combat, PlayerStats stats)
        {
            playerCombat = combat;
            playerStats = stats;
        }

        public void UpdateBehavior() { }

        public void ExecuteBasicAttack(Vector3 aimDirection)
        {
            float attackDamage = playerCombat.BaseDamage;
            float attackRange = 2.4f;

            Collider[] hits = Physics.OverlapSphere(playerCombat.transform.position + aimDirection * 1.2f, attackRange);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyBase enemy = hits[i].GetComponent<EnemyBase>();
                if (enemy == null) enemy = hits[i].GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    DamageInfo info = new DamageInfo(attackDamage, aimDirection, 4.0f, false, playerCombat.gameObject);
                    enemy.TakeDamage(info);
                }
            }
        }

        public void ExecuteChargedAttack(Vector3 aimDirection, float chargeRatio)
        {
            float attackDamage = playerCombat.BaseDamage * (1.5f + chargeRatio * 1.0f); // Up to 2.5x damage
            float attackRange = 3.5f;

            Collider[] hits = Physics.OverlapSphere(playerCombat.transform.position, attackRange);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyBase enemy = hits[i].GetComponent<EnemyBase>();
                if (enemy == null) enemy = hits[i].GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    Vector3 knockDir = (enemy.transform.position - playerCombat.transform.position).normalized;
                    DamageInfo info = new DamageInfo(attackDamage, knockDir, 10.0f, true, playerCombat.gameObject);
                    enemy.TakeDamage(info);
                }
            }
        }
    }
}
