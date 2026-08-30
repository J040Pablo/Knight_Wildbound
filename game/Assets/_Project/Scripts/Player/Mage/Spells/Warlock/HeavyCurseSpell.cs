using UnityEngine;
using Roguelite.Combat;
using Roguelite.Combat.StatusEffects;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Warlock
{
    public class HeavyCurseSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 targetPos = playerCombat.GetGroundReticleTargetWorldPosition();
            float radius = 5.0f;
            float duration = 6.0f;

            // Ground Rune Sigil VFX
            MageVFXHelper.CreateGroundRune(targetPos, radius, Definition.primaryColor, duration);

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
                    DamageInfo info = new DamageInfo(CalculateDamage(chargeRatio), Vector3.zero, 0f, true, playerCombat.gameObject);
                    enemy.TakeDamage(info);

                    var receiver = GetStatusReceiver(enemy);
                    if (receiver != null)
                    {
                        receiver.ApplyEffect(new CurseStatusEffect(8.0f, 0.35f, 0.35f), playerCombat.gameObject, duration);
                    }
                }
            }
        }
    }
}
