using UnityEngine;
using Roguelite.Combat;
using Roguelite.Combat.StatusEffects;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Warlock
{
    public class ShadowChainSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 travelDir = GetTargetDirection(spawnPos);

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            RaycastHit[] hits = Physics.RaycastAll(spawnPos, travelDir, 30f, mask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == playerCombat.gameObject || hit.collider.transform.IsChildOf(playerCombat.transform)) continue;

                EnemyBase targetEnemy = hit.collider.GetComponent<EnemyBase>();
                if (targetEnemy == null) targetEnemy = hit.collider.GetComponentInParent<EnemyBase>();

                if (targetEnemy != null && !targetEnemy.IsDead)
                {
                    float damage = CalculateDamage(chargeRatio);
                    DamageInfo info = new DamageInfo(damage, Vector3.zero, 0f, true, playerCombat.gameObject);
                    targetEnemy.TakeDamage(info);

                    var receiver = GetStatusReceiver(targetEnemy);
                    if (receiver != null)
                    {
                        receiver.ApplyEffect(new RootStatusEffect(), playerCombat.gameObject, 2.5f);
                    }

                    if (playerStats != null)
                    {
                        playerStats.Heal(damage * 0.25f);
                    }

                    MageVFXHelper.CreateLightningStreak(spawnPos, targetEnemy.transform.position + Vector3.up * 1.0f, Definition.primaryColor, 1.2f);
                    return;
                }
            }

            Vector3 endPos = spawnPos + travelDir * 25.0f;
            MageVFXHelper.CreateLightningStreak(spawnPos, endPos, Definition.primaryColor, 0.3f);
        }
    }
}
