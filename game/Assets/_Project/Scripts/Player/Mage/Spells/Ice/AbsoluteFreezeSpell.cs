using UnityEngine;
using Roguelite.Combat;
using Roguelite.Combat.StatusEffects;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Ice
{
    public class AbsoluteFreezeSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 targetPos = playerCombat.GetGroundReticleTargetWorldPosition();
            float outerRadius = 5.5f;
            float innerFreezeRadius = 3.0f;
            float damage = CalculateDamage(chargeRatio);

            // Blizzard ground VFX
            MageVFXHelper.CreateGroundRune(targetPos, outerRadius, Definition.primaryColor, 2.5f);
            MageVFXHelper.CreateImpactExplosion(targetPos, outerRadius, Definition.secondaryColor, 0.5f);

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            Collider[] hits = Physics.OverlapSphere(targetPos, outerRadius, mask);
            foreach (var col in hits)
            {
                if (col == null || col.gameObject == playerCombat.gameObject || col.transform.IsChildOf(playerCombat.transform) || col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) continue;

                EnemyBase enemy = col.GetComponent<EnemyBase>();
                if (enemy == null) enemy = col.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    DamageInfo info = new DamageInfo(damage, Vector3.zero, 1.0f, true, playerCombat.gameObject);
                    enemy.TakeDamage(info);

                    var receiver = GetStatusReceiver(enemy);
                    if (receiver != null)
                    {
                        float distToCenter = Vector3.Distance(enemy.transform.position, targetPos);
                        if (distToCenter <= innerFreezeRadius)
                        {
                            receiver.ApplyEffect(new FreezeStatusEffect(), playerCombat.gameObject, 1.8f);
                        }
                        else
                        {
                            receiver.ApplyEffect(new SlowStatusEffect(0.65f), playerCombat.gameObject, 3.5f);
                        }
                    }
                }
            }
        }
    }
}
