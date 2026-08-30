using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Cosmic
{
    public class CosmicRaySpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            if (playerCombat != null)
            {
                playerCombat.StartCoroutine(ChannelCosmicRay(aimDirection, chargeRatio));
            }
        }

        private IEnumerator ChannelCosmicRay(Vector3 aimDir, float chargeRatio)
        {
            float duration = 1.2f;
            float elapsed = 0f;
            float tickInterval = 0.15f;
            float tickTimer = 0f;
            float totalDamage = CalculateDamage(chargeRatio);
            float tickDamage = totalDamage / (duration / tickInterval);

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            while (elapsed < duration)
            {
                if (playerCombat == null || playerStats.IsDead) yield break;

                elapsed += Time.deltaTime;
                tickTimer += Time.deltaTime;

                Vector3 currentAim = playerCombat.GetReticleAimDirection();
                Vector3 origin = GetSpawnPosition(currentAim);
                Vector3 endPos = origin + currentAim * 20.0f;

                // Render continuous cosmic beam line
                MageVFXHelper.CreateLightningStreak(origin, endPos, Definition.primaryColor, 0.16f);

                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    RaycastHit[] hits = Physics.SphereCastAll(origin, 1.2f, currentAim, 20.0f, mask);
                    foreach (var hit in hits)
                    {
                        if (hit.collider == null || hit.collider.gameObject == playerCombat.gameObject || hit.collider.transform.IsChildOf(playerCombat.transform)) continue;

                        EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
                        if (enemy == null) enemy = hit.collider.GetComponentInParent<EnemyBase>();

                        if (enemy != null && !enemy.IsDead)
                        {
                            DamageInfo info = new DamageInfo(tickDamage, currentAim, 2.0f, false, playerCombat.gameObject);
                            enemy.TakeDamage(info);
                        }
                    }
                }

                yield return null;
            }
        }
    }
}
