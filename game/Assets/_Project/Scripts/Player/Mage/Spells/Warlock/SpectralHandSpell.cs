using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Warlock
{
    public class SpectralHandSpell : MageSpell
    {
        // Per-cast noise seed so multiple hands (re-casts, or future upgrade interactions)
        // don't wobble in lockstep with each other.
        private float noiseSeed;

        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            if (playerCombat != null)
            {
                noiseSeed = Random.Range(0f, 100f);
                playerCombat.StartCoroutine(TrackAndExecuteSpectralHand(chargeRatio));
            }
        }

        private IEnumerator TrackAndExecuteSpectralHand(float chargeRatio)
        {
            float duration = 1.75f;      // was 1.4f — slightly longer lifetime, per design brief
            float elapsed = 0f;
            float tickInterval = 0.2f;   // unchanged — damage/balance untouched
            float tickTimer = 0f;
            float radius = 3.5f;         // unchanged — damage/balance untouched
            float damage = CalculateDamage(chargeRatio) * 0.35f; // unchanged — damage/balance untouched

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

                // Continuously track the player's aim/reticle position, but pursue it like a
                // hunting entity rather than snapping straight to it: a slower convergence
                // rate (was factor 10 — near-instant) makes the hand visibly lag and chase,
                // and a small Perlin-noise-driven lateral drift gives it a "searching" wobble
                // instead of a perfectly smooth curve. It still reliably reaches and stays
                // near the reticle, so aiming stays readable and skill-based — it just doesn't
                // teleport there.
                Vector3 currentTargetPos = playerCombat.GetReticleTargetWorldPosition();
                if (handVFX != null)
                {
                    Vector3 pursued = Vector3.Lerp(handVFX.transform.position, currentTargetPos, Time.deltaTime * 3.2f);

                    Vector3 toTarget = currentTargetPos - handVFX.transform.position;
                    if (toTarget.sqrMagnitude > 0.0001f)
                    {
                        Vector3 lateral = Vector3.Cross(toTarget.normalized, Vector3.up);
                        float wobble = (Mathf.PerlinNoise(elapsed * 1.6f, noiseSeed) - 0.5f) * 1.2f;
                        pursued += lateral * wobble * Time.deltaTime * 4f;
                    }

                    handVFX.transform.position = pursued;
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
