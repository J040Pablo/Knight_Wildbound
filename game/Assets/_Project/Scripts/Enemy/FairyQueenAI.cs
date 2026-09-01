using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Core.Utilities;
using Roguelite.Items;
using Roguelite.Loot;
using Roguelite.Player;
using Roguelite.Progression;
using Roguelite.Environment;

namespace Roguelite.Enemy
{
    public class FairyQueenAI : EnemyBase
    {
        [Header("Fairy Queen Stats")]
        [SerializeField] private float hoverHeight = 3.2f;
        [SerializeField] private float attackCooldown = 2.8f;
        [SerializeField] private float pulseRadius = 12.0f;
        [SerializeField] private float pulseDamage = 35.0f;

        [Header("Detection & Leash Settings")]
        [SerializeField] private float detectionRange = 16.0f;
        [SerializeField] private float combatRange = 7.5f;
        [SerializeField] private float chaseLimit = 36.0f;
        [SerializeField] private float returnRadius = 40.0f;

        private float summonCooldownTimer = 0f;
        private float hoverTime = 0f;
        private float stuckTimer = 0f;
        private Vector3 lastTrackedPos;
        private Vector3 arenaCenterPos;
        private bool isAggroed = false;
        private bool isPhase2 = false;
        private readonly List<GameObject> activeMinions = new List<GameObject>();
        private const int MAX_MINIONS = 3;

        public override bool IsBossEnemy => true;
        public override string DisplayName => "Rainha das Fadas";
        public bool IsPhase2 => isPhase2;
        public bool IsAggroed => isAggroed;

        private const float SUMMON_COOLDOWN = 18.0f;

        protected override void Awake()
        {
            base.Awake();
            MaxHP = 900f; // Balanced 900 HP for Phase 1 of court climax
            CurrentHP = MaxHP;
        }

        protected override void Start()
        {
            base.Start();
            arenaCenterPos = transform.position;
            lastTrackedPos = transform.position;
            BuildFairyQueenVisuals();
        }

        public void TriggerBossFight()
        {
            if (isAggroed) return;
            isAggroed = true;
            stuckTimer = 0f;
            lastTrackedPos = transform.position;
            // Debug.Log("👑 [FAIRY QUEEN] — Boss Battle Activated in Royal Court Arena (900 HP)!");
        }

        public override void TakeDamage(DamageInfo damageInfo)
        {
            // Immediate Aggro Reaction: If hit from any distance, awaken boss combat instantly!
            if (!isAggroed)
            {
                TriggerBossFight();
            }
            base.TakeDamage(damageInfo);
        }

        private void BuildFairyQueenVisuals()
        {
            if (transform.Find("QueenBody_Visual") != null) return;

            // Ethereal Dress & Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "QueenBody_Visual";
            body.transform.parent = transform;
            body.transform.localPosition = new Vector3(0, 1.4f, 0);
            body.transform.localScale = new Vector3(0.9f, 1.3f, 0.9f);

            Collider col = body.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer r = body.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.85f, 0.25f, 0.95f); // Ethereal Bright Magenta

            // Crown
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "QueenCrown_Visual";
            crown.transform.parent = transform;
            crown.transform.localPosition = new Vector3(0, 2.7f, 0);
            crown.transform.localScale = new Vector3(0.7f, 0.2f, 0.7f);

            Collider cCol = crown.GetComponent<Collider>();
            if (cCol != null) Destroy(cCol);

            Renderer cR = crown.GetComponent<Renderer>();
            if (cR != null) cR.material.color = new Color(0.95f, 0.85f, 0.2f); // Gold

            // Wings
            GameObject wingL = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wingL.name = "WingL";
            wingL.transform.parent = transform;
            wingL.transform.localPosition = new Vector3(-0.8f, 1.8f, -0.1f);
            wingL.transform.localRotation = Quaternion.Euler(0, 30f, 0);
            wingL.transform.localScale = new Vector3(1.1f, 1.8f, 1.0f);
            Collider wLCol = wingL.GetComponent<Collider>();
            if (wLCol != null) Destroy(wLCol);
            Renderer wLR = wingL.GetComponent<Renderer>();
            if (wLR != null) wLR.material.color = new Color(0.9f, 0.5f, 1.0f);

            GameObject wingR = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wingR.name = "WingR";
            wingR.transform.parent = transform;
            wingR.transform.localPosition = new Vector3(0.8f, 1.8f, -0.1f);
            wingR.transform.localRotation = Quaternion.Euler(0, -30f, 0);
            wingR.transform.localScale = new Vector3(1.1f, 1.8f, 1.0f);
            Collider wRCol = wingR.GetComponent<Collider>();
            if (wRCol != null) Destroy(wRCol);
            Renderer wRR = wingR.GetComponent<Renderer>();
            if (wRR != null) wRR.material.color = new Color(0.9f, 0.5f, 1.0f);
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || isAttacking || !SafeCanMove()) return;

            hoverTime += Time.deltaTime;
            float hoverY = Mathf.Sin(hoverTime * 2.5f) * 0.5f + hoverHeight;

            // Pre-combat state / Idle when player is not present
            if (playerTransform == null || playerStats == null || playerStats.IsDead)
            {
                ReturnToArenaCenter(hoverY);
                return;
            }

            // Anti-Stuck System Monitoring
            if (isAggroed)
            {
                float moveDist = Vector3.Distance(transform.position, lastTrackedPos);
                if (moveDist < 0.5f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= 3.0f)
                    {
                        RecoverFromStuck();
                        return;
                    }
                }
                else
                {
                    stuckTimer = 0f;
                    lastTrackedPos = transform.position;
                }
            }

            float distFromCenter = Vector3.Distance(transform.position, arenaCenterPos);
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            float playerDistFromCenter = Vector3.Distance(playerTransform.position, arenaCenterPos);

            // Phase 2 Transformation Check at 50% HP (450 HP)
            if (!isPhase2 && CurrentHP <= MaxHP * 0.5f)
            {
                TriggerPhase2Transformation();
            }

            // Arena Leash Check: Reset state if player leaves court (40m)
            if (playerDistFromCenter > returnRadius || distFromCenter > returnRadius)
            {
                if (isAggroed)
                {
                    ResetBossState();
                }
                ReturnToArenaCenter(hoverY);
                return;
            }

            // Pre-combat throne presence: Remain passive until arena trigger or close approach or taking damage
            if (!isAggroed)
            {
                if (distToPlayer <= detectionRange && playerDistFromCenter <= chaseLimit)
                {
                    TriggerBossFight();
                }
                else
                {
                    ReturnToArenaCenter(hoverY);
                    return;
                }
            }

            attackTimer -= Time.deltaTime;
            if (summonCooldownTimer > 0f) summonCooldownTimer -= Time.deltaTime;

            // Combat positioning
            float speedMultiplier = isPhase2 ? 8.5f : 6.5f;
            Vector3 targetPos = playerTransform.position + (transform.position - playerTransform.position).normalized * combatRange;
            targetPos.y = playerTransform.position.y + hoverY;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * speedMultiplier);

            // Turn to face player
            Vector3 lookDir = (playerTransform.position - transform.position);
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Vector3 normLook = lookDir.normalized;
                Vector3 safeUp = Mathf.Abs(Vector3.Dot(normLook, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
                Quaternion rot = Quaternion.LookRotation(normLook, safeUp);
                rot.Normalize();
                transform.rotation = rot;
            }

            // Blink away if player gets too close (< 4m)
            if (distToPlayer < 4.0f && !isAttacking)
            {
                BlinkAway();
                return;
            }

            // Ability decisions
            if (attackTimer <= 0f)
            {
                float rand = Random.value;
                if (CanSummonMinions())
                {
                    StartCoroutine(PerformFairySummon());
                }
                else if (isPhase2 && rand < 0.30f)
                {
                    StartCoroutine(PerformCrystalOverload());
                }
                else if (distToPlayer <= pulseRadius && rand < 0.50f)
                {
                    StartCoroutine(PerformEnchantedPulse());
                }
                else if (rand < 0.75f)
                {
                    PerformFairyBlessing();
                }
                else
                {
                    StartCoroutine(PerformArcaneBoltBarrage());
                }
            }
        }

        private void RecoverFromStuck()
        {
            stuckTimer = 0f;
            Vector3 safePos = arenaCenterPos + Vector3.up * hoverHeight;
            transform.position = safePos;
            lastTrackedPos = safePos;

            if (playerTransform != null)
            {
                Vector3 lookDir = (playerTransform.position - transform.position);
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            }
            // Debug.Log("⚠️ [FAIRY QUEEN ANTI-STUCK] — Recovered position to arena center!");
        }

        private void TriggerPhase2Transformation()
        {
            isPhase2 = true;
            attackCooldown = 1.8f;
            hoverHeight = 3.8f;

            if (Roguelite.Wave.EncounterManager.Instance != null)
            {
                Roguelite.Wave.EncounterManager.Instance.TriggerBanner("👑 RAINHA DAS FADAS — FASE 2: ENFURECIDA!");
            }

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(1.2f, 0.8f);

            Transform bodyVisual = transform.Find("QueenBody_Visual");
            if (bodyVisual != null && bodyVisual.TryGetComponent<Renderer>(out var r))
            {
                r.material.color = new Color(0.95f, 0.10f, 0.35f);
            }
        }

        private void BlinkAway()
        {
            Vector3 blinkOffset = Random.insideUnitSphere * 10.0f;
            blinkOffset.y = Mathf.Abs(blinkOffset.y) + 2.0f;
            transform.position += blinkOffset;

            Quaternion rot = transform.rotation;
            float sqrMag = rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w;
            transform.rotation = (sqrMag < 0.001f) ? Quaternion.identity : Quaternion.Normalize(rot);
        }

        private bool CanSummonMinions()
        {
            activeMinions.RemoveAll(m => m == null);
            return summonCooldownTimer <= 0f && activeMinions.Count < MAX_MINIONS;
        }

        private IEnumerator PerformFairySummon()
        {
            isAttacking = true;
            summonCooldownTimer = SUMMON_COOLDOWN;
            attackTimer = attackCooldown;

            for (int i = 0; i < MAX_MINIONS; i++)
            {
                GameObject minion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                minion.name = "RoyalFairyGuardMinion";
                minion.transform.position = transform.position + (i == 0 ? transform.right : -transform.right) * 3.0f;
                minion.transform.localScale = Vector3.one * 0.8f;

                FairyEnemyAI fairyAI = minion.AddComponent<FairyEnemyAI>();
                activeMinions.Add(minion);
            }

            yield return new WaitForSeconds(0.8f);
            isAttacking = false;
        }

        private IEnumerator PerformCrystalOverload()
        {
            isAttacking = true;
            attackTimer = attackCooldown + 1.2f;

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.8f, 0.6f);

            yield return new WaitForSeconds(0.8f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= pulseRadius * 1.4f)
                {
                    Vector3 kbDir = (playerTransform.position - transform.position).normalized;
                    DamageInfo info = new DamageInfo(45f, kbDir, 16f, true, gameObject);
                    playerStats.TakeDamage(info);
                }
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private IEnumerator PerformEnchantedPulse()
        {
            isAttacking = true;
            attackTimer = attackCooldown + 1.0f;

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "EnchantedPulseTelegraph";
            Collider rCol = ring.GetComponent<Collider>();
            if (rCol != null) Destroy(rCol);

            ring.transform.position = transform.position + Vector3.down * (hoverHeight - 0.1f);
            ring.transform.localScale = new Vector3(pulseRadius * 2f, 0.05f, pulseRadius * 2f);

            Renderer r = ring.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.9f, 0.2f, 0.9f, 0.5f);

            yield return new WaitForSeconds(1.0f);
            Destroy(ring);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= pulseRadius)
                {
                    Vector3 kbDir = (playerTransform.position - transform.position).normalized;
                    DamageInfo info = new DamageInfo(pulseDamage, kbDir, 14f, true, gameObject);
                    playerStats.TakeDamage(info);
                }

                ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
                if (cam != null) cam.TriggerShake(0.5f, 0.3f);
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private void PerformFairyBlessing()
        {
            attackTimer = attackCooldown;
            Collider[] hits = Physics.OverlapSphere(transform.position, 16.0f);

            foreach (var hit in hits)
            {
                EnemyBase ally = hit.GetComponent<EnemyBase>();
                if (ally != null && ally != this && !ally.IsDead && ally.CurrentHP < ally.MaxHP)
                {
                    ally.Heal(50f);
                }
            }
        }

        private IEnumerator PerformArcaneBoltBarrage()
        {
            isAttacking = true;
            attackTimer = attackCooldown;

            yield return new WaitForSeconds(0.3f);

            int boltCount = isPhase2 ? 7 : 5;
            for (int i = 0; i < boltCount; i++)
            {
                if (IsDead || playerStats == null || playerStats.IsDead) break;

                GameObject bolt = FairyProjectilePool.Instance != null
                    ? FairyProjectilePool.Instance.GetProjectile(transform.position, new Color(0.95f, 0.3f, 1.0f), 0.5f)
                    : GameObject.CreatePrimitive(PrimitiveType.Sphere);

                if (FairyProjectilePool.Instance == null)
                {
                    bolt.transform.position = transform.position;
                    bolt.transform.localScale = Vector3.one * 0.5f;
                }

                Vector3 spreadOffset = Random.insideUnitSphere * 0.6f;
                Vector3 dir = (playerTransform.position + Vector3.up * 1f + spreadOffset - transform.position).normalized;

                StartCoroutine(MoveArcaneBolt(bolt, dir));
                yield return new WaitForSeconds(0.12f);
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private IEnumerator MoveArcaneBolt(GameObject bolt, Vector3 dir)
        {
            float elapsed = 0f;
            while (elapsed < 1.8f && bolt != null && bolt.activeInHierarchy)
            {
                bolt.transform.position += dir * 20.0f * Time.deltaTime;
                if (playerStats != null && Vector3.Distance(bolt.transform.position, playerTransform.position + Vector3.up * 1f) < 0.9f)
                {
                    DamageInfo info = new DamageInfo(16f, dir, 3f, false, gameObject);
                    playerStats.TakeDamage(info);

                    if (FairyProjectilePool.Instance != null) FairyProjectilePool.Instance.ReturnProjectile(bolt);
                    else Destroy(bolt);
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (bolt != null)
            {
                if (FairyProjectilePool.Instance != null) FairyProjectilePool.Instance.ReturnProjectile(bolt);
                else Destroy(bolt);
            }
        }

        protected override void Die()
        {
            if (IsDead) return;
            IsDead = true;

            foreach (var m in activeMinions) if (m != null) Destroy(m);
            activeMinions.Clear();

            // Give XP for Phase 1
            if (playerStats != null) playerStats.AddXP(400);

            // Execute Transition Sequence to awaken AwakenedWorldTreeAI!
            StartCoroutine(ExecuteTransitionToWorldTree());
            base.Die();
        }

        private IEnumerator ExecuteTransitionToWorldTree()
        {
            // 1. Visual burst & sound shake
            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(1.5f, 2.5f);

            if (Roguelite.Wave.EncounterManager.Instance != null)
            {
                Roguelite.Wave.EncounterManager.Instance.TriggerBanner("✨ A ÁRVORE SAGRADA DESPERTA...");
            }

            yield return new WaitForSeconds(1.8f);

            // 2. Awaken the Sacred World Tree Boss behind the throne
            AwakenedWorldTreeAI treeBoss = FindFirstObjectByType<AwakenedWorldTreeAI>();
            if (treeBoss == null)
            {
                GameObject treeObj = GameObject.Find("GreatFaePalaceTree_SacredLocalTree");
                if (treeObj != null)
                {
                    treeBoss = treeObj.AddComponent<AwakenedWorldTreeAI>();
                }
                else
                {
                    treeObj = new GameObject("AwakenedWorldTree_FinalBoss");
                    treeObj.transform.position = new Vector3(0, SceneEnvironmentBuilder.GetTerrainHeightY(0, 700f), 700f);
                    treeBoss = treeObj.AddComponent<AwakenedWorldTreeAI>();
                }
            }

            if (treeBoss != null)
            {
                treeBoss.AwakenBoss();
            }
        }

        private void ReturnToArenaCenter(float hoverY)
        {
            Vector3 target = arenaCenterPos + Vector3.up * hoverY;
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 4f);
        }

        private void ResetBossState()
        {
            isAggroed = false;
            isPhase2 = false;
            CurrentHP = MaxHP;
            attackCooldown = 2.8f;
            hoverHeight = 3.2f;

            FairyKingdomArenaTrigger trigger = FindFirstObjectByType<FairyKingdomArenaTrigger>();
            if (trigger != null) trigger.UnlockArena();
        }
    }
}
