using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Core.Utilities;
using Roguelite.Items;
using Roguelite.Loot;
using Roguelite.Player;
using Roguelite.Progression;

namespace Roguelite.Enemy
{
    public class FairyQueenAI : EnemyBase
    {
        [Header("Fairy Queen Stats")]
        [SerializeField] private float hoverHeight = 2.8f;
        [SerializeField] private float attackCooldown = 3.0f;
        [SerializeField] private float pulseRadius = 8.0f;
        [SerializeField] private float pulseDamage = 28.0f;

        [Header("Detection & Leash Settings")]
        [SerializeField] private float detectionRange = 8.0f;  // 8m detection radius
        [SerializeField] private float combatRange = 6.0f;     // 6m attack start radius
        [SerializeField] private float chaseLimit = 18.0f;     // 18m maximum chase distance
        [SerializeField] private float returnRadius = 20.0f;    // 20m return radius threshold

        private float summonCooldownTimer = 0f;
        private float hoverTime = 0f;
        private Vector3 arenaCenterPos;
        private bool isAggroed = false;
        private readonly List<GameObject> activeMinions = new List<GameObject>();
        private const int MAX_MINIONS = 2;

        public override bool IsBossEnemy => true;
        public override string DisplayName => "Fairy Queen";
        private const float SUMMON_COOLDOWN = 20.0f;

        protected override void Awake()
        {
            base.Awake();
            MaxHP = 300f;
            CurrentHP = MaxHP;
        }

        protected override void Start()
        {
            base.Start();
            arenaCenterPos = transform.position;
            BuildFairyQueenVisuals();
        }

        private void BuildFairyQueenVisuals()
        {
            if (transform.Find("QueenBody_Visual") != null) return;

            // Ethereal Dress & Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "QueenBody_Visual";
            body.transform.parent = transform;
            body.transform.localPosition = new Vector3(0, 1.2f, 0);
            body.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);

            Collider col = body.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer r = body.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.85f, 0.25f, 0.95f); // Ethereal Bright Magenta

            // Crown
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "QueenCrown_Visual";
            crown.transform.parent = transform;
            crown.transform.localPosition = new Vector3(0, 2.3f, 0);
            crown.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);

            Collider cCol = crown.GetComponent<Collider>();
            if (cCol != null) Destroy(cCol);

            Renderer cR = crown.GetComponent<Renderer>();
            if (cR != null) cR.material.color = new Color(0.95f, 0.85f, 0.2f); // Gold

            // Wings
            GameObject wingL = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wingL.name = "WingL";
            wingL.transform.parent = transform;
            wingL.transform.localPosition = new Vector3(-0.6f, 1.5f, -0.1f);
            wingL.transform.localRotation = Quaternion.Euler(0, 30f, 0);
            wingL.transform.localScale = new Vector3(0.8f, 1.4f, 1.0f);
            Collider wLCol = wingL.GetComponent<Collider>();
            if (wLCol != null) Destroy(wLCol);
            Renderer wLR = wingL.GetComponent<Renderer>();
            if (wLR != null) wLR.material.color = new Color(0.9f, 0.5f, 1.0f);

            GameObject wingR = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wingR.name = "WingR";
            wingR.transform.parent = transform;
            wingR.transform.localPosition = new Vector3(0.6f, 1.5f, -0.1f);
            wingR.transform.localRotation = Quaternion.Euler(0, -30f, 0);
            wingR.transform.localScale = new Vector3(0.8f, 1.4f, 1.0f);
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

            // Idle state when player is not present or dead
            if (playerTransform == null || playerStats == null || playerStats.IsDead)
            {
                ReturnToArenaCenter(hoverY);
                return;
            }

            float distFromCenter = Vector3.Distance(transform.position, arenaCenterPos);
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            float playerDistFromCenter = Vector3.Distance(playerTransform.position, arenaCenterPos);

            // Arena Leash Check: If player or Fairy Queen moves beyond returnRadius (20m), leash & reset
            if (playerDistFromCenter > returnRadius || distFromCenter > returnRadius)
            {
                if (isAggroed)
                {
                    ResetBossState();
                }
                ReturnToArenaCenter(hoverY);
                return;
            }

            // Line-of-sight wall obstruction check (No detecting or attacking through walls)
            bool hasLineOfSight = true;
            Vector3 eyePos = transform.position + Vector3.up * 1.5f;
            Vector3 targetEyePos = playerTransform.position + Vector3.up * 1.0f;
            Vector3 eyeDir = targetEyePos - eyePos;

            if (Physics.Raycast(eyePos, eyeDir.normalized, out RaycastHit wallHit, distToPlayer))
            {
                if (!wallHit.collider.isTrigger && !wallHit.collider.CompareTag("Player") && wallHit.distance < distToPlayer - 0.5f)
                {
                    hasLineOfSight = false;
                }
            }

            // Detection check: Only activate when player enters 8m detection range WITH line-of-sight inside arena (18m limit)
            if (!isAggroed)
            {
                if (distToPlayer <= detectionRange && hasLineOfSight && playerDistFromCenter <= chaseLimit)
                {
                    isAggroed = true;
                    Debug.Log("👑 [FAIRY QUEEN] — Boss Battle Activated in Ruins Arena (8m Detection)!");
                }
                else
                {
                    ReturnToArenaCenter(hoverY);
                    return;
                }
            }

            // If line-of-sight lost or player leaves chase limit (18m), disengage and return to center
            if (!hasLineOfSight || playerDistFromCenter > chaseLimit)
            {
                ReturnToArenaCenter(hoverY);
                return;
            }

            attackTimer -= Time.deltaTime;
            if (summonCooldownTimer > 0f) summonCooldownTimer -= Time.deltaTime;

            // Hover & combat positioning (target combat range 9m)
            Vector3 targetPos = playerTransform.position + (transform.position - playerTransform.position).normalized * combatRange;
            targetPos.y = playerTransform.position.y + hoverY;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * 6.5f);

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
                else if (distToPlayer <= pulseRadius && rand < 0.35f)
                {
                    StartCoroutine(PerformEnchantedPulse());
                }
                else if (rand < 0.65f)
                {
                    PerformFairyBlessing();
                }
                else
                {
                    StartCoroutine(PerformArcaneBoltBarrage());
                }
            }
        }

        private void BlinkAway()
        {
            Vector3 blinkOffset = Random.insideUnitSphere * 8.0f;
            blinkOffset.y = Mathf.Abs(blinkOffset.y) + 2.0f;
            transform.position += blinkOffset;

            Quaternion rot = transform.rotation;
            float sqrMag = rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w;
            transform.rotation = (sqrMag < 0.001f) ? Quaternion.identity : Quaternion.Normalize(rot);

            Debug.Log("✨ [FAIRY QUEEN BLINK] — Teleported to safety!");
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

            Debug.Log("🧚 [FAIRY QUEEN] — Summoning Fairy Guards!");

            for (int i = 0; i < MAX_MINIONS; i++)
            {
                GameObject minion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                minion.name = "FairyGuardMinion";
                minion.transform.position = transform.position + (i == 0 ? transform.right : -transform.right) * 2.5f;
                minion.transform.localScale = Vector3.one * 0.6f;

                FairyEnemyAI fairyAI = minion.AddComponent<FairyEnemyAI>();
                activeMinions.Add(minion);
            }

            yield return new WaitForSeconds(0.8f);
            isAttacking = false;
        }

        private IEnumerator PerformEnchantedPulse()
        {
            isAttacking = true;
            attackTimer = attackCooldown + 1.0f;

            // Telegraph ring
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
            Collider[] hits = Physics.OverlapSphere(transform.position, 12.0f);
            int healedCount = 0;

            foreach (var hit in hits)
            {
                EnemyBase ally = hit.GetComponent<EnemyBase>();
                if (ally != null && ally != this && !ally.IsDead && ally.CurrentHP < ally.MaxHP)
                {
                    ally.Heal(30f);
                    healedCount++;
                }
            }

            if (healedCount > 0)
            {
                Debug.Log($"💖 [FAIRY QUEEN BLESSING] — Healed {healedCount} fairy allies!");
            }
        }

        private IEnumerator PerformArcaneBoltBarrage()
        {
            isAttacking = true;
            attackTimer = attackCooldown;

            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < 5; i++)
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

                Vector3 spreadOffset = Random.insideUnitSphere * 0.5f;
                Vector3 dir = (playerTransform.position + Vector3.up * 1f + spreadOffset - transform.position).normalized;

                StartCoroutine(MoveArcaneBolt(bolt, dir));
                yield return new WaitForSeconds(0.15f);
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private IEnumerator MoveArcaneBolt(GameObject bolt, Vector3 dir)
        {
            float elapsed = 0f;
            while (elapsed < 1.8f && bolt != null && bolt.activeInHierarchy)
            {
                bolt.transform.position += dir * 18.0f * Time.deltaTime;
                if (playerStats != null && Vector3.Distance(bolt.transform.position, playerTransform.position + Vector3.up * 1f) < 0.9f)
                {
                    DamageInfo info = new DamageInfo(14f, dir, 3f, false, gameObject);
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

            foreach (var m in activeMinions)
            {
                if (m != null) Destroy(m);
            }
            activeMinions.Clear();

            // 150 XP Reward
            if (playerStats != null) playerStats.AddXP(150);
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.AddXP(150);

            // Spawn Fairy Queen Crown accessory
            ItemData crown = ItemDatabase.Get("accessory_fairy_queen_crown");
            if (crown != null)
            {
                LootDrop.SpawnSingle(transform.position + Vector3.right * 1f, crown, 1);
            }

            // Guaranteed Rare/Epic Chest
            var chest = Environment.SceneEnvironmentBuilder.SpawnInteractiveTreasureChest(transform.position + Vector3.forward * 2f, Quaternion.identity, ChestRarity.Epic);
            if (chest != null) chest.name = "FairyQueenRewardChest";

            Debug.Log("👑 [FAIRY QUEEN DEFEATED] — 150 XP + Fairy Queen Crown + Epic Chest Awarded!");
            base.Die();
        }

        private void ReturnToArenaCenter(float hoverY)
        {
            Vector3 target = arenaCenterPos + Vector3.up * hoverY;
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 4f);
        }

        private void ResetBossState()
        {
            isAggroed = false;
            CurrentHP = MaxHP;
            Debug.Log("👑 [FAIRY QUEEN] — Player left arena! Boss returned to center & health reset to 100%.");
        }
    }
}
