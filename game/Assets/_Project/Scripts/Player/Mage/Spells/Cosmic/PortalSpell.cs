using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Cosmic
{
    public class PortalSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 entryPos = GetSpawnPosition(aimDirection);
            Vector3 exitPos = playerCombat.GetReticleTargetWorldPosition();

            // 1. Entry Portal
            MageVFXHelper.CreatePortalRing(entryPos, aimDirection, Definition.primaryColor, 2.2f, 1.0f);

            // 2. Exit Portal near target
            Vector3 exitDir = -aimDirection;
            MageVFXHelper.CreatePortalRing(exitPos + Vector3.up * 1.0f, exitDir, Definition.secondaryColor, 2.8f, 1.2f);

            // 3. Emergence Blast at Exit Portal
            float damage = CalculateDamage(chargeRatio);
            float radius = 3.8f;

            MageVFXHelper.CreateImpactExplosion(exitPos, radius, Definition.primaryColor, 0.4f);

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            Collider[] hits = Physics.OverlapSphere(exitPos, radius, mask);
            foreach (var col in hits)
            {
                if (col == null || col.gameObject == playerCombat.gameObject || col.transform.IsChildOf(playerCombat.transform) || col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) continue;

                EnemyBase enemy = col.GetComponent<EnemyBase>();
                if (enemy == null) enemy = col.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    Vector3 knock = (enemy.transform.position - exitPos).normalized;
                    DamageInfo info = new DamageInfo(damage, knock, 7.0f, true, playerCombat.gameObject);
                    enemy.TakeDamage(info);
                }
            }
        }
    }
}
