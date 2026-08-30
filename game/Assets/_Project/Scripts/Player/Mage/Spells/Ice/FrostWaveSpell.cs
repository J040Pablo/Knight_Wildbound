using UnityEngine;
using Roguelite.Combat;
using Roguelite.Combat.StatusEffects;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Ice
{
    public class FrostWaveSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 targetPos = playerCombat.GetGroundReticleTargetWorldPosition();
            float damage = CalculateDamage(chargeRatio);
            float radius = 4.2f + chargeRatio * 1.2f;

            // 1. Icy Ground rune & Frost Wave VFX at target location
            MageVFXHelper.CreateGroundRune(targetPos, radius, Definition.primaryColor, 2.5f);
            MageVFXHelper.CreateImpactExplosion(targetPos, radius, Definition.secondaryColor, 0.45f);

            // 2. Area Damage & Frost Slow
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
                    Vector3 knockDir = (enemy.transform.position - targetPos).normalized;
                    if (knockDir.sqrMagnitude < 0.001f) knockDir = aimDirection;

                    DamageInfo info = new DamageInfo(damage, knockDir, 3.0f, false, playerCombat.gameObject);
                    enemy.TakeDamage(info);

                    var receiver = GetStatusReceiver(enemy);
                    if (receiver != null)
                    {
                        receiver.ApplyEffect(new SlowStatusEffect(0.60f), playerCombat.gameObject, 4.0f);
                    }
                }
            }
        }
    }
}
