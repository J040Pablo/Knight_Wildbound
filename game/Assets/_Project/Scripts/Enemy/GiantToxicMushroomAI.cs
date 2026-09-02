using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Data;
using Roguelite.Loot;
using Roguelite.Player;

namespace Roguelite.Enemy
{
    /// <summary>
    /// Deep Forest mini-boss — introduces poison mechanics well before the Fairy Queen court
    /// climax. Follows the same "regular enemy escalated to mini-boss" shape already
    /// established by StoneGiantAI: a boosted EnemyData config, a boss health bar via
    /// IsBossEnemy, a 30%-HP enrage-style intensification, and telegraphed primitive-VFX
    /// attacks. Sits between the Elite tier (Stone Giant, 600 HP) and the Fairy Queen
    /// (900 HP) in both HP and mechanical complexity — three rotating abilities, no phase
    /// transition.
    /// </summary>
    public class GiantToxicMushroomAI : EnemyBase
    {
        [Header("Giant Toxic Mushroom Settings")]
        [SerializeField] private float slamRadius = 6.5f;
        [SerializeField] private float slamDamage = 23.0f; // +25% attack damage (18 -> 23)
        [SerializeField] private float poisonCloudRadius = 5.5f;
        [SerializeField] private float sporeBurstRange = 14.0f;
        [SerializeField] private float moveSpeed = 2.4f;

        [Header("Sprout Summoning Settings")]
        [SerializeField] private int maxSprouts = 4; // Up to 4 sprout adds concurrently
        [SerializeField] private float sproutChance = 0.65f; // 65% spawn chance per spore

        public override bool IsBossEnemy => true;
        public override string DisplayName => "Giant Mushroom Colossus";

        private bool isBloomEnraged = false;
        private Vector3 spawnPos;
        private Transform capTransform;
        private Vector3 capBaseScale;
        private Renderer capRenderer;
        private readonly List<PoisonMushroomAI> summonedSprouts = new List<PoisonMushroomAI>();

        private static readonly Color CapColorNormal = new Color(0.75f, 0.15f, 0.35f);
        private static readonly Color CapColorEnraged = new Color(1.0f, 0.15f, 0.05f);
        private static readonly Color StalkColor = new Color(0.85f, 0.8f, 0.65f);

        protected override void Awake()
        {
            if (enemyData == null)
            {
                EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
                data.enemyName = "Giant Mushroom Colossus";
                data.maxHealth = 980f; // +40% HP buff (700 -> 980)
                data.xpReward = 400;
                data.modelScale = new Vector3(2.6f, 2.4f, 2.6f);
                data.enemyColor = CapColorNormal;
                enemyData = data;
            }

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            spawnPos = transform.position;
            BuildMushroomVisuals();
        }

        private void BuildMushroomVisuals()
        {
            if (transform.Find("MushroomStalk_Visual") != null) return;

            GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stalk.name = "MushroomStalk_Visual";
            stalk.transform.SetParent(transform, false);
            stalk.transform.localPosition = new Vector3(0, 0.55f, 0);
            stalk.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            StripCollider(stalk);
            SetMaterialColor(stalk, StalkColor);

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "MushroomCap_Visual";
            cap.transform.SetParent(transform, false);
            cap.transform.localPosition = new Vector3(0, 1.15f, 0);
            cap.transform.localScale = new Vector3(1.3f, 0.85f, 1.3f);
            StripCollider(cap);
            capTransform = cap.transform;
            capBaseScale = cap.transform.localScale;
            capRenderer = cap.GetComponent<Renderer>();
            if (capRenderer != null) capRenderer.material.color = CapColorNormal;

            // A few small spore-gill dots under the cap for silhouette read.
            for (int i = 0; i < 5; i++)
            {
                float angle = 72f * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward * 0.6f;
                GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spot.name = "MushroomSpot_Visual";
                spot.transform.SetParent(cap.transform, false);
                spot.transform.localPosition = dir + Vector3.up * 0.3f;
                spot.transform.localScale = Vector3.one * 0.18f;
                StripCollider(spot);
                SetMaterialColor(spot, new Color(0.95f, 0.9f, 0.8f));
            }

            meshRenderer = capRenderer;
            originalColor = CapColorNormal;
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || isAttacking || playerTransform == null || playerStats.IsDead || !SafeCanMove()) return;

            // 50% Enrage Threshold
            if (!isBloomEnraged && CurrentHP / MaxHP <= 0.50f)
            {
                TriggerToxicBloom();
            }

            float distFromSpawn = Vector3.Distance(transform.position, spawnPos);
            if (distFromSpawn > 20f)
            {
                Vector3 returnDir = (spawnPos - transform.position).normalized;
                characterController.Move(returnDir * moveSpeed * Time.deltaTime);
                return;
            }

            attackTimer -= Time.deltaTime;
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            FacePlayer();

            if (attackTimer <= 0f)
            {
                float roll = Random.value;
                if (dist <= slamRadius + 1f && roll < 0.4f)
                {
                    StartCoroutine(PerformSlam());
                }
                else if (dist <= poisonCloudRadius + 4f && roll < 0.7f)
                {
                    StartCoroutine(PerformToxicBreath());
                }
                else if (dist <= sporeBurstRange)
                {
                    StartCoroutine(PerformSporeBurst());
                }
                else
                {
                    MoveTowardPlayer();
                }
            }
            else if (dist > slamRadius)
            {
                MoveTowardPlayer();
            }
        }

        private void FacePlayer()
        {
            Vector3 lookDir = playerTransform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                rot.Normalize();
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);
            }
        }

        private void MoveTowardPlayer()
        {
            Vector3 dir = playerTransform.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                float speed = isBloomEnraged ? moveSpeed * 1.3f : moveSpeed;
                characterController.Move(dir.normalized * speed * Time.deltaTime);
            }
        }

        private void TriggerToxicBloom()
        {
            isBloomEnraged = true;
            if (capRenderer != null)
            {
                capRenderer.material.color = CapColorEnraged;
                originalColor = CapColorEnraged;
            }

            // Spawn glowing red fungal node visuals on the cap
            if (capTransform != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f + 45f;
                    Vector3 pos = Quaternion.Euler(0, angle, 0) * Vector3.forward * 0.5f + Vector3.up * 0.4f;
                    GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    node.name = "MushroomFungalNode_Visual";
                    node.transform.SetParent(capTransform, false);
                    node.transform.localPosition = pos;
                    node.transform.localScale = Vector3.one * 0.35f;
                    StripCollider(node);
                    SetMaterialColor(node, new Color(1.0f, 0.2f, 0.05f));
                }
            }

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.5f, 0.4f);
        }

        // ── Toxic Breath Cone Sweep ───────────────────────────────
        private IEnumerator PerformToxicBreath()
        {
            isAttacking = true;
            attackTimer = isBloomEnraged ? 2.5f : 3.5f;

            // Telegraph cone angle in front of boss (Wider 60-degree cone)
            GameObject coneTelegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coneTelegraph.name = "ToxicBreathTelegraph";
            StripCollider(coneTelegraph);
            coneTelegraph.transform.SetParent(transform, false);
            coneTelegraph.transform.localPosition = new Vector3(0, 0.05f, 5.0f);
            coneTelegraph.transform.localScale = new Vector3(6.5f, 0.02f, 10.0f);
            SetMaterialColor(coneTelegraph, new Color(0.2f, 0.9f, 0.2f, 0.45f));

            yield return new WaitForSeconds(0.5f);
            Destroy(coneTelegraph);

            // Sweep toxic breath emission (3.5s duration)
            float breathDuration = 3.5f;
            float elapsed = 0f;
            while (elapsed < breathDuration)
            {
                if (IsDead) yield break;
                elapsed += Time.deltaTime;

                if (playerStats != null && !playerStats.IsDead)
                {
                    Vector3 toPlayer = playerTransform.position - transform.position;
                    toPlayer.y = 0;
                    float dist = toPlayer.magnitude;
                    float angle = Vector3.Angle(transform.forward, toPlayer);

                    if (dist <= 10.5f && angle <= 60f)
                    {
                        DamageInfo info = new DamageInfo(6f * Time.deltaTime * 6f, transform.forward, 3f, false, gameObject);
                        playerStats.TakeDamage(info);

                        PoisonStatusEffect poison = playerStats.GetComponent<PoisonStatusEffect>();
                        if (poison == null) poison = playerStats.gameObject.AddComponent<PoisonStatusEffect>();
                        poison.ApplyPoison(4f, 5f);
                    }
                }
                yield return null;
            }

            isAttacking = false;
        }

        // ── Slam ──────────────────────────────────────────────────
        private IEnumerator PerformSlam()
        {
            isAttacking = true;
            attackTimer = isBloomEnraged ? 2.0f : 2.6f;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "MushroomSlamTelegraph";
            StripCollider(marker);
            marker.transform.position = transform.position + new Vector3(0, 0.05f, 0);
            marker.transform.localScale = new Vector3(slamRadius * 2f, 0.02f, slamRadius * 2f);
            SetMaterialColor(marker, new Color(0.9f, 0.4f, 0.15f, 0.5f));

            yield return new WaitForSeconds(0.6f);
            Destroy(marker);

            if (capTransform != null) capTransform.localScale = capBaseScale * 1.25f;

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= slamRadius)
                {
                    Vector3 dir = (playerTransform.position - transform.position).normalized;
                    DamageInfo info = new DamageInfo(slamDamage, dir, 12f, false, gameObject);
                    playerStats.TakeDamage(info);
                }

                ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
                if (cam != null) cam.TriggerShake(0.55f, 0.35f);
            }

            // Dual Expanding Shockwaves: Primary inner wave & Secondary outer ring
            GameObject wave1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wave1.name = "MushroomSlamWave_Primary";
            StripCollider(wave1);
            wave1.transform.position = transform.position + new Vector3(0, 0.05f, 0);
            SetMaterialColor(wave1, new Color(0.3f, 0.9f, 0.35f, 0.5f));
            StartCoroutine(ExpandAndFade(wave1, slamRadius));

            GameObject wave2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wave2.name = "MushroomSlamWave_Secondary";
            StripCollider(wave2);
            wave2.transform.position = transform.position + new Vector3(0, 0.05f, 0);
            SetMaterialColor(wave2, new Color(0.9f, 0.2f, 0.2f, 0.4f));
            StartCoroutine(ExpandAndFade(wave2, slamRadius * 1.5f));

            yield return new WaitForSeconds(0.35f);
            if (capTransform != null) capTransform.localScale = capBaseScale;
            isAttacking = false;
        }

        // ── Poison Cloud ──────────────────────────────────────────
        private IEnumerator PerformPoisonCloud()
        {
            isAttacking = true;
            attackTimer = isBloomEnraged ? 3.6f : 4.5f;

            float cloudRadius = isBloomEnraged ? poisonCloudRadius * 1.3f : poisonCloudRadius;
            float cloudDuration = 4.0f;

            GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cloud.name = "MushroomPoisonCloud";
            StripCollider(cloud);
            cloud.transform.position = transform.position + new Vector3(0, 0.1f, 0);
            cloud.transform.localScale = new Vector3(cloudRadius * 2f, 0.1f, cloudRadius * 2f);
            SetMaterialColor(cloud, new Color(0.15f, 0.85f, 0.25f, 0.4f));

            float elapsed = 0f;
            float tickTimer = 0f;
            while (elapsed < cloudDuration)
            {
                if (IsDead)
                {
                    Destroy(cloud);
                    isAttacking = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                tickTimer += Time.deltaTime;

                if (tickTimer >= 1.0f)
                {
                    tickTimer = 0f;
                    if (playerStats != null && !playerStats.IsDead)
                    {
                        float dist = Vector3.Distance(transform.position, playerTransform.position);
                        if (dist <= cloudRadius)
                        {
                            PoisonStatusEffect poison = playerStats.GetComponent<PoisonStatusEffect>();
                            if (poison == null) poison = playerStats.gameObject.AddComponent<PoisonStatusEffect>();
                            poison.ApplyPoison(3f, 5f);
                        }
                    }
                }

                yield return null;
            }

            Destroy(cloud);
            isAttacking = false;
        }

        // ── Spore Burst ───────────────────────────────────────────
        private IEnumerator PerformSporeBurst()
        {
            isAttacking = true;
            attackTimer = isBloomEnraged ? 2.2f : 2.8f;

            yield return new WaitForSeconds(0.3f);

            const int burstCount = 3;
            for (int i = 0; i < burstCount; i++)
            {
                Vector3 dirToPlayer = playerTransform.position - transform.position;
                dirToPlayer.y = 0;
                dirToPlayer.Normalize();

                float spread = (i - (burstCount - 1) / 2f) * 18f;
                Vector3 dir = Quaternion.Euler(0, spread, 0) * dirToPlayer;

                StartCoroutine(LaunchSpore(transform.position + Vector3.up * 1.2f, dir));
            }

            yield return new WaitForSeconds(0.3f);
            isAttacking = false;
        }

        private IEnumerator LaunchSpore(Vector3 start, Vector3 direction)
        {
            GameObject spore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spore.name = "MushroomSporeProjectile";
            StripCollider(spore);
            spore.transform.position = start;
            spore.transform.localScale = Vector3.one * 0.35f;
            SetMaterialColor(spore, new Color(0.6f, 0.95f, 0.3f));

            float travelTime = 0.6f;
            float t = 0f;
            const float travelDist = 6.5f;
            Vector3 targetPos = start + direction * travelDist;

            while (t < travelTime)
            {
                if (spore == null) yield break;
                t += Time.deltaTime;
                float p = t / travelTime;
                spore.transform.position = Vector3.Lerp(start, targetPos, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * 1.2f;
                yield return null;
            }

            Vector3 landPos = spore.transform.position;
            Destroy(spore);

            if (!IsDead && playerStats != null && !playerStats.IsDead && playerTransform != null)
            {
                float distToPlayer = Vector3.Distance(landPos, playerTransform.position);
                if (distToPlayer <= 2.0f)
                {
                    DamageInfo info = new DamageInfo(6f, direction, 3f, false, gameObject);
                    playerStats.TakeDamage(info);

                    PoisonStatusEffect poison = playerStats.GetComponent<PoisonStatusEffect>();
                    if (poison == null) poison = playerStats.gameObject.AddComponent<PoisonStatusEffect>();
                    poison.ApplyPoison(4f, 3f);
                }
            }

            TrySpawnSprout(landPos);
        }

        /// <summary>
        /// Spore Burst can summon a couple of small mushroom sprouts, capped at maxSprouts
        /// concurrently. Reuses the existing regular PoisonMushroomAI enemy as the summon
        /// (rather than a new enemy class) and the same CreatePrimitive+AddComponent spawn
        /// idiom EncounterZone.SpawnEnemy<T>() already uses, plus SpawnManager for a
        /// validated, non-overlapping spawn position.
        /// </summary>
        private void TrySpawnSprout(Vector3 position)
        {
            summonedSprouts.RemoveAll(s => s == null || s.IsDead);
            if (summonedSprouts.Count >= maxSprouts) return;
            if (Random.value > sproutChance) return;

            Vector3 spawnPosition = position;
            Vector3 playerPos = playerTransform != null ? playerTransform.position : transform.position;
            if (Roguelite.Core.SpawnManager.Instance != null)
            {
                spawnPosition = Roguelite.Core.SpawnManager.Instance.GetValidEnemySpawnPosition(position, playerPos, new List<Vector3>(), 0.6f);
            }

            GameObject sproutObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            sproutObj.name = "MushroomSprout_PoisonMushroomAI";
            sproutObj.transform.position = spawnPosition;
            sproutObj.transform.localScale = Vector3.one * 0.65f;

            Collider pCol = sproutObj.GetComponent<Collider>();
            if (pCol != null) Destroy(pCol);

            CharacterController cc = sproutObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0, 0.9f, 0);

            PoisonMushroomAI sprout = sproutObj.AddComponent<PoisonMushroomAI>();
            summonedSprouts.Add(sprout);
        }

        // ── Shared helpers ────────────────────────────────────────
        private IEnumerator ExpandAndFade(GameObject ring, float targetRadius)
        {
            float duration = 0.35f;
            float t = 0f;
            Vector3 startScale = new Vector3(0.3f, 0.02f, 0.3f);
            Vector3 endScale = new Vector3(targetRadius * 2f, 0.02f, targetRadius * 2f);
            Renderer rend = ring != null ? ring.GetComponent<Renderer>() : null;
            Color startColor = rend != null ? rend.material.color : Color.clear;

            while (t < duration)
            {
                if (ring == null) yield break;
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                ring.transform.localScale = Vector3.Lerp(startScale, endScale, p);

                if (rend != null)
                {
                    Color c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, p);
                    rend.material.color = c;
                }

                yield return null;
            }

            if (ring != null) Destroy(ring);
        }

        /// <summary>
        /// Overrides EnemyBase's default name-matching loot lookup with the mini-boss's own
        /// dedicated table (guaranteed Toxic Spore material + a Rune-tier accessory + gold,
        /// with a chance for the Bloomheart relic).
        /// </summary>
        protected override void SpawnEnemyLoot()
        {
            LootResult loot = LootTable.ForGiantToxicMushroom();
            LootDrop.SpawnFromResult(loot, transform.position + Vector3.up * 0.5f);
        }

        private static void StripCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }

        private static void SetMaterialColor(GameObject go, Color color)
        {
            if (go.TryGetComponent<Renderer>(out var r))
            {
                r.material.color = color;
            }
        }
    }
}
