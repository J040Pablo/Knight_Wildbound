using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Cosmic
{
    public class SpaceFragmentSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 travelDir = GetTargetDirection(spawnPos);
            float dist = 25.0f;

            float damage = CalculateDamage(chargeRatio);

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            RaycastHit[] hits = Physics.RaycastAll(spawnPos, travelDir, dist, mask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            int hitCount = 0;
            Vector3 endPos = spawnPos + travelDir * dist;

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == playerCombat.gameObject || hit.collider.transform.IsChildOf(playerCombat.transform)) continue;

                EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
                if (enemy == null) enemy = hit.collider.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    DamageInfo info = new DamageInfo(damage, travelDir, 4.0f, false, playerCombat.gameObject);
                    enemy.TakeDamage(info);

                    MageVFXHelper.CreateImpactExplosion(hit.point, 0.8f, Definition.primaryColor, 0.2f);
                    hitCount++;

                    if (hitCount >= 2)
                    {
                        endPos = hit.point;
                        break;
                    }
                }
            }

            MageVFXHelper.CreateLightningStreak(spawnPos, endPos, Definition.primaryColor, 0.25f);
        }
    }
}
