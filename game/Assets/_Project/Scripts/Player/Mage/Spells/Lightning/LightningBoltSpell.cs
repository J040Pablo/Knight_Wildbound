using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Lightning
{
    public class LightningBoltSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 travelDir = GetTargetDirection(spawnPos);

            Vector3 hitPos = spawnPos + travelDir * 40f;

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            RaycastHit[] hits = Physics.RaycastAll(spawnPos, travelDir, 40f, mask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == playerCombat.gameObject || hit.collider.transform.IsChildOf(playerCombat.transform)) continue;

                hitPos = hit.point;
                EnemyBase primaryEnemy = hit.collider.GetComponent<EnemyBase>();
                if (primaryEnemy == null) primaryEnemy = hit.collider.GetComponentInParent<EnemyBase>();

                if (primaryEnemy != null && !primaryEnemy.IsDead)
                {
                    float dmg = CalculateDamage(chargeRatio);
                    DamageInfo info = new DamageInfo(dmg, travelDir, 4.0f, false, playerCombat.gameObject);
                    primaryEnemy.TakeDamage(info);

                    if (Random.value <= 0.35f)
                    {
                        ChainToNearbyEnemy(primaryEnemy, hitPos, dmg * 0.6f);
                    }
                    break;
                }
            }

            MageVFXHelper.CreateLightningStreak(spawnPos, hitPos, Definition.primaryColor, 0.2f);
            MageVFXHelper.CreateImpactExplosion(hitPos, 0.8f, Definition.primaryColor, 0.25f);
        }

        private void ChainToNearbyEnemy(EnemyBase sourceEnemy, Vector3 origin, float chainDamage)
        {
            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            Collider[] nearby = Physics.OverlapSphere(origin, 6.0f, mask);
            foreach (var col in nearby)
            {
                if (col == null || col.gameObject == playerCombat.gameObject || col.transform.IsChildOf(playerCombat.transform) || col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) continue;

                EnemyBase other = col.GetComponent<EnemyBase>();
                if (other == null) other = col.GetComponentInParent<EnemyBase>();

                if (other != null && other != sourceEnemy && !other.IsDead)
                {
                    DamageInfo info = new DamageInfo(chainDamage, (other.transform.position - origin).normalized, 2.0f, false, playerCombat.gameObject);
                    other.TakeDamage(info);

                    MageVFXHelper.CreateLightningStreak(origin, other.transform.position + Vector3.up * 1.0f, new Color(0.9f, 0.95f, 0.3f), 0.25f);
                    break;
                }
            }
        }
    }
}
