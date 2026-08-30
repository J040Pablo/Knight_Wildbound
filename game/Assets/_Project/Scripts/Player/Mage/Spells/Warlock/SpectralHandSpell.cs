using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Warlock
{
    public class SpectralHandSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            if (playerCombat != null)
            {
                playerCombat.StartCoroutine(TrackAndExecuteSpectralHand(chargeRatio));
            }
        }

        private IEnumerator TrackAndExecuteSpectralHand(float chargeRatio)
        {
            float duration = 1.4f;
            float elapsed = 0f;
            float tickInterval = 0.2f;
            float tickTimer = 0f;
            float radius = 3.5f;
            float damage = CalculateDamage(chargeRatio) * 0.35f;

            if (playerCombat == null) yield break;

            Vector3 initialPos = playerCombat.GetReticleTargetWorldPosition();
            GameObject handVFX = MageVFXHelper.CreateSpectralHandVisual(initialPos, Definition.primaryColor, duration + 0.2f);

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            while (elapsed < duration)
            {
                if (playerCombat == null || playerStats.IsDead)
                {
                    if (handVFX != null) Object.Destroy(handVFX);
                    yield break;
                }

                elapsed += Time.deltaTime;
                tickTimer += Time.deltaTime;

                // Continuously track mouse reticle position
                Vector3 currentTargetPos = playerCombat.GetReticleTargetWorldPosition();
                if (handVFX != null)
                {
                    handVFX.transform.position = Vector3.Lerp(handVFX.transform.position, currentTargetPos, Time.deltaTime * 10f);
                }

                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    Vector3 handCenter = handVFX != null ? handVFX.transform.position : currentTargetPos;

                    Collider[] hits = Physics.OverlapSphere(handCenter, radius, mask);
                    foreach (var col in hits)
                    {
                        if (col == null || col.gameObject == playerCombat.gameObject || col.transform.IsChildOf(playerCombat.transform) || col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) continue;

                        EnemyBase enemy = col.GetComponent<EnemyBase>();
                        if (enemy == null) enemy = col.GetComponentInParent<EnemyBase>();

                        if (enemy != null && !enemy.IsDead)
                        {
                            Vector3 pullDir = (handCenter - enemy.transform.position);
                            pullDir.y = 0;

                            DamageInfo info = new DamageInfo(damage, pullDir.normalized, 4.0f, false, playerCombat.gameObject);
                            enemy.TakeDamage(info);
                        }
                    }
                }

                yield return null;
            }
        }
    }
}
