using UnityEngine;
using Roguelite.Combat;
using Roguelite.Combat.StatusEffects;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Lightning
{
    public class LightningStrikeSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 targetPos = playerCombat.GetGroundReticleTargetWorldPosition();
            float radius = 3.8f;
            float dmg = CalculateDamage(chargeRatio);

            // 1. Mark target ground area
            MageVFXHelper.CreateGroundRune(targetPos, radius, Definition.primaryColor, 0.4f);

            // 2. Thunder bolt striking from sky
            Vector3 skyPos = targetPos + Vector3.up * 18.0f;
            MageVFXHelper.CreateLightningStreak(skyPos, targetPos, Definition.primaryColor, 0.35f);
            MageVFXHelper.CreateImpactExplosion(targetPos, radius, Definition.secondaryColor, 0.4f);

            // 3. Area damage + Stun
            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            Collider[] hits = Physics.OverlapSphere(targetPos, radius, mask);
            foreach (var col in hits)
            {
                if (col == null || col.gameObject == playerCombat.gameObject || col.transform.IsChildOf(playerCombat.transform) || col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) continue;

                EnemyBase enemy = col.GetComponent<EnemyBase>();
                if (enemy == null) enemy = col.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    Vector3 knock = (enemy.transform.position - targetPos).normalized + Vector3.up * 0.3f;
                    DamageInfo info = new DamageInfo(dmg, knock, 6.0f, true, playerCombat.gameObject);
                    enemy.TakeDamage(info);

                    // Apply Stun
                    var receiver = GetStatusReceiver(enemy);
                    if (receiver != null)
                    {
                        receiver.ApplyEffect(new StunStatusEffect(), playerCombat.gameObject, 1.2f);
                    }
                }
            }
        }
    }
}
