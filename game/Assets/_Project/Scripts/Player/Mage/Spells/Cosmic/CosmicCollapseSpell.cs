using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Cosmic
{
    public class CosmicCollapseSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 targetPos = playerCombat.GetGroundReticleTargetWorldPosition();
            if (playerCombat != null)
            {
                playerCombat.StartCoroutine(ExecuteCosmicCollapseSequence(aimDirection, targetPos, chargeRatio));
            }
        }

        private IEnumerator ExecuteCosmicCollapseSequence(Vector3 aimDir, Vector3 targetPos, float chargeRatio)
        {
            if (playerCombat == null) yield break;

            // 1. Huge portal behind Mage
            Vector3 portalPos = playerCombat.transform.position - aimDir.normalized * 1.5f + Vector3.up * 1.5f;
            MageVFXHelper.CreatePortalRing(portalPos, aimDir, Definition.secondaryColor, 3.5f, 2.2f);

            // 2. Spawn Mini Black Hole at target position
            GameObject blackHole = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blackHole.name = "MiniBlackHoleVFX";
            blackHole.transform.position = targetPos + Vector3.up * 1.0f;
            blackHole.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            Object.Destroy(blackHole.GetComponent<Collider>());
            var rend = blackHole.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.08f, 0.02f, 0.15f);
            }

            float pullDuration = 1.8f;
            float elapsed = 0f;
            float pullRadius = 8.0f;

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            // 3. Gravitational Pull phase
            while (elapsed < pullDuration)
            {
                if (playerCombat == null || playerStats.IsDead)
                {
                    if (blackHole != null) Object.Destroy(blackHole);
                    yield break;
                }

                elapsed += Time.deltaTime;

                // Pulsate Black Hole
                if (blackHole != null)
                {
                    float s = 1.2f + Mathf.Sin(elapsed * 12f) * 0.3f;
                    blackHole.transform.localScale = new Vector3(s, s, s);
                }

                // Pull enemies inward
                Collider[] hits = Physics.OverlapSphere(targetPos, pullRadius, mask);
                foreach (var col in hits)
                {
                    if (col == null || col.gameObject == playerCombat.gameObject || col.transform.IsChildOf(playerCombat.transform) || col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) continue;

                    EnemyBase enemy = col.GetComponent<EnemyBase>();
                    if (enemy == null) enemy = col.GetComponentInParent<EnemyBase>();

                    if (enemy != null && !enemy.IsDead)
                    {
                        Vector3 pullDir = (targetPos - enemy.transform.position);
                        pullDir.y = 0;
                        if (pullDir.sqrMagnitude > 0.01f)
                        {
                            var cc = enemy.GetComponent<CharacterController>();
                            if (cc != null && cc.enabled && cc.gameObject.activeInHierarchy)
                            {
                                cc.Move(pullDir.normalized * 6.5f * Time.deltaTime);
                            }
                        }
                    }
                }

                yield return null;
            }

            // 4. Collapse to tiny point
            if (blackHole != null)
            {
                blackHole.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                yield return new WaitForSeconds(0.1f);
                Object.Destroy(blackHole);
            }

            if (playerCombat == null || playerStats.IsDead) yield break;

            // 5. Massive Cosmic Explosion
            float damage = CalculateDamage(chargeRatio);
            float expRadius = 6.0f;

            MageVFXHelper.CreateImpactExplosion(targetPos, expRadius, Definition.primaryColor, 0.5f);
            MageVFXHelper.CreateGroundRune(targetPos, expRadius, Definition.secondaryColor, 1.0f);

            Collider[] expHits = Physics.OverlapSphere(targetPos, expRadius, mask);
            foreach (var col in expHits)
            {
                if (col == null || col.gameObject == playerCombat.gameObject || col.transform.IsChildOf(playerCombat.transform) || col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) continue;

                EnemyBase enemy = col.GetComponent<EnemyBase>();
                if (enemy == null) enemy = col.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    Vector3 knock = (enemy.transform.position - targetPos).normalized + Vector3.up * 0.4f;
                    DamageInfo info = new DamageInfo(damage, knock, 10.0f, true, playerCombat.gameObject);
                    enemy.TakeDamage(info);
                }
            }
        }
    }
}
