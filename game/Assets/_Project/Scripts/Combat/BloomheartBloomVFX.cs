using System.Collections;
using UnityEngine;
using Roguelite.Combat.StatusEffects;
using Roguelite.Enemy;
using Roguelite.Player;

namespace Roguelite.Combat
{
    /// <summary>
    /// Self-contained "fire and forget" object spawned by OnHitRelicEffects.TryTriggerBloomheart.
    /// Grows a small mushroom at the kill site, pops it into a spore shockwave, applies the
    /// gameplay effects (damage + Slow to nearby enemies, brief Haste to the player), then
    /// cleans itself up. Deliberately self-hosted (rather than routed through a manager
    /// singleton) so the trigger call site stays a single line.
    /// </summary>
    public class BloomheartBloomVFX : MonoBehaviour
    {
        private GameObject caster;
        private float burstRadius;
        private float burstDamage;
        private float slowPercent;
        private float slowDuration;
        private float hasteAmount;
        private float hasteDuration;

        private static readonly Color CapColorLow = new Color(0.85f, 0.25f, 0.55f);
        private static readonly Color CapColorHigh = new Color(1.0f, 0.55f, 0.85f);
        private static readonly Color StalkColor = new Color(0.55f, 0.85f, 0.35f);
        private static readonly Color ShockwaveColor = new Color(0.95f, 0.5f, 0.85f, 0.55f);
        private static readonly Color SporePuffColor = new Color(1.0f, 0.7f, 0.9f, 0.85f);

        public void Initialize(GameObject casterObj, float radius, float damage, float slowPercent, float slowDuration, float hasteAmount, float hasteDuration)
        {
            caster = casterObj;
            burstRadius = radius;
            burstDamage = damage;
            this.slowPercent = slowPercent;
            this.slowDuration = slowDuration;
            this.hasteAmount = hasteAmount;
            this.hasteDuration = hasteDuration;

            StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            // ── Stage 1: Growth ──────────────────────────────────
            GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stalk.name = "BloomheartStalk";
            stalk.transform.SetParent(transform, false);
            StripCollider(stalk);
            Renderer stalkRend = stalk.GetComponent<Renderer>();
            if (stalkRend != null) stalkRend.material.color = StalkColor;

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "BloomheartCap";
            cap.transform.SetParent(transform, false);
            StripCollider(cap);
            Renderer capRend = cap.GetComponent<Renderer>();
            if (capRend != null) capRend.material.color = CapColorLow;

            float growDuration = 0.5f;
            float t = 0f;
            while (t < growDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / growDuration);
                float eased = p * p * (3f - 2f * p); // smoothstep

                stalk.transform.localScale = new Vector3(0.25f, eased * 0.7f, 0.25f);
                stalk.transform.localPosition = new Vector3(0, eased * 0.35f, 0);

                cap.transform.localScale = Vector3.one * (eased * 0.9f);
                cap.transform.localPosition = new Vector3(0, eased * 0.75f, 0);
                if (capRend != null) capRend.material.color = Color.Lerp(CapColorLow, CapColorHigh, Mathf.PingPong(t * 3f, 1f));

                yield return null;
            }

            yield return new WaitForSeconds(0.1f);

            // ── Stage 2: Pop ─────────────────────────────────────
            cap.transform.localScale = Vector3.one * 1.3f; // squash-stretch pop beat

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "BloomheartShockwaveVFX";
            ring.transform.position = transform.position + Vector3.up * 0.1f;
            StripCollider(ring);
            Renderer ringRend = ring.GetComponent<Renderer>();
            if (ringRend != null) ringRend.material.color = ShockwaveColor;

            const int puffCount = 8;
            for (int i = 0; i < puffCount; i++)
            {
                float angle = (360f / puffCount) * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                SpawnSporePuff(transform.position + Vector3.up * 0.8f, dir);
            }

            // ── Stage 3: Gameplay effects ────────────────────────
            ApplyBurstEffects();

            // ── Stage 4: Expand + fade, then clean up ────────────
            float burstDuration = 0.4f;
            float bt = 0f;
            Vector3 ringStartScale = new Vector3(0.3f, 0.02f, 0.3f);
            Vector3 ringEndScale = new Vector3(burstRadius * 2f, 0.02f, burstRadius * 2f);
            while (bt < burstDuration)
            {
                bt += Time.deltaTime;
                float p = Mathf.Clamp01(bt / burstDuration);
                ring.transform.localScale = Vector3.Lerp(ringStartScale, ringEndScale, p);

                if (ringRend != null)
                {
                    Color c = ringRend.material.color;
                    c.a = Mathf.Lerp(0.55f, 0f, p);
                    ringRend.material.color = c;
                }

                float shrink = Mathf.Lerp(1f, 0f, p);
                cap.transform.localScale = Vector3.one * (0.9f * shrink);
                stalk.transform.localScale = new Vector3(0.25f * shrink, 0.7f * shrink, 0.25f * shrink);

                yield return null;
            }

            Destroy(ring);
            Destroy(gameObject);
        }

        private void ApplyBurstEffects()
        {
            int mask = LayerMask.GetMask("Enemy", "Boss");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            Collider[] hits = Physics.OverlapSphere(transform.position, burstRadius, mask);
            foreach (var col in hits)
            {
                if (col == null) continue;

                EnemyBase enemy = col.GetComponent<EnemyBase>();
                if (enemy == null) enemy = col.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;

                Vector3 dir = enemy.transform.position - transform.position;
                dir.y = 0;

                DamageInfo info = new DamageInfo(burstDamage, dir.normalized, 3f, false, caster);
                enemy.TakeDamage(info);

                StatusEffectReceiver receiver = enemy.GetOrCreateStatusReceiver();
                receiver.ApplyEffect(new SlowStatusEffect(slowPercent), caster, slowDuration);
            }

            // Brief Haste buff for whoever landed the kill. Hosted on the PlayerStats instance
            // itself (not on this short-lived VFX object) so the buff reliably reverts even
            // though this GameObject is destroyed well before the buff duration elapses.
            PlayerStats playerStats = caster != null ? caster.GetComponent<PlayerStats>() : null;
            if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.StartCoroutine(TemporaryHaste(playerStats, hasteAmount, hasteDuration));
            }

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.25f, 0.2f);
        }

        private static IEnumerator TemporaryHaste(PlayerStats stats, float amount, float duration)
        {
            stats.ModifyMoveSpeedMultiplier(amount);
            yield return new WaitForSeconds(duration);
            if (stats != null) stats.ModifyMoveSpeedMultiplier(-amount);
        }

        private void SpawnSporePuff(Vector3 origin, Vector3 direction)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "BloomheartSporePuff";
            puff.transform.position = origin;
            puff.transform.localScale = Vector3.one * 0.18f;
            StripCollider(puff);
            Renderer rend = puff.GetComponent<Renderer>();
            if (rend != null) rend.material.color = SporePuffColor;

            // Safety-net destroy independent of the animation coroutine below, matching the
            // Object.Destroy(obj, duration) convention used throughout the project's other VFX
            // helpers — guarantees no orphaned puff if this object is torn down mid-animation.
            Destroy(puff, 0.6f);

            StartCoroutine(AnimateSporePuff(puff, direction));
        }

        private IEnumerator AnimateSporePuff(GameObject puff, Vector3 direction)
        {
            float duration = 0.45f;
            float t = 0f;
            Vector3 start = puff.transform.position;

            while (t < duration && puff != null)
            {
                t += Time.deltaTime;
                float p = t / duration;
                puff.transform.position = start + direction * (burstRadius * 0.9f * p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * 0.6f;
                puff.transform.localScale = Vector3.one * (0.18f * (1f - p));
                yield return null;
            }
        }

        private static void StripCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }
    }
}
