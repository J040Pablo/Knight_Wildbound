using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Loot;

namespace Roguelite.Enemy
{
    public enum FairyType
    {
        Standard,
        Healer,
        WitchElite
    }

    public class FairyEnemyAI : EnemyBase
    {
        [Header("Fairy Config")]
        [SerializeField] private FairyType fairyType = FairyType.Standard;
        [SerializeField] private float hoverHeight = 2.0f;
        [SerializeField] private float attackCooldown = 3.5f;

        [SerializeField] private float detectionRange = 14f;

        private float attackTimer = 0f;
        private float hoverTime = 0f;
        private Vector3 targetHoverPos;
        private Vector3 spawnPosition;
        private bool isAggroed = false;

        // Forest Witch Summon Limits (User specified caps)
        private int totalSummonsExecuted = 0;
        private const int MAX_TOTAL_SUMMONS = 3;
        private const float SUMMON_COOLDOWN = 20.0f;
        private float summonCooldownTimer = 0f;
        private readonly List<GameObject> activeSummonedMinions = new List<GameObject>();

        public FairyType Type => fairyType;

        protected override void Awake()
        {
            base.Awake();
            MaxHP = fairyType == FairyType.WitchElite ? 120f : (fairyType == FairyType.Healer ? 35f : 40f);
            CurrentHP = MaxHP;
        }

        protected override void Start()
        {
            base.Start();
            spawnPosition = transform.position;
            targetHoverPos = spawnPosition;

            if (meshRenderer != null)
            {
                meshRenderer.material.color = fairyType switch
                {
                    FairyType.WitchElite => new Color(0.6f, 0.1f, 0.8f), // Deep Purple
                    FairyType.Healer => new Color(0.2f, 0.9f, 0.6f),     // Emerald Mint
                    _ => new Color(0.9f, 0.2f, 0.3f)                    // Malignant Red
                };
            }
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || isAttacking || !SafeCanMove()) return;

            // Hover float animation
            hoverTime += Time.deltaTime;
            float hoverY = Mathf.Sin(hoverTime * 3f) * 0.4f + hoverHeight;

            if (playerTransform == null || playerStats == null || playerStats.IsDead)
            {
                // Idle hover around spawn position
                Vector3 idlePos = spawnPosition + Vector3.up * hoverY;
                transform.position = Vector3.MoveTowards(transform.position, idlePos, Time.deltaTime * 2f);
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            // Detection check: Only aggro if player comes within detectionRange (14m)
            if (!isAggroed)
            {
                if (dist <= detectionRange)
                {
                    isAggroed = true;
                }
                else
                {
                    // Idle hover at spawn point when unaggroed
                    Vector3 idlePos = spawnPosition + Vector3.up * hoverY;
                    transform.position = Vector3.MoveTowards(transform.position, idlePos, Time.deltaTime * 2f);
                    return;
                }
            }

            // De-aggro if player runs away > 28m
            if (dist > 28f)
            {
                isAggroed = false;
                return;
            }

            attackTimer -= Time.deltaTime;
            if (summonCooldownTimer > 0f) summonCooldownTimer -= Time.deltaTime;

            // Keep distance / move to hover target
            Vector3 desiredPos = playerTransform.position + (transform.position - playerTransform.position).normalized * 7.0f;
            desiredPos.y = playerTransform.position.y + hoverY;
            transform.position = Vector3.MoveTowards(transform.position, desiredPos, Time.deltaTime * 5f);

            // Face player
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

            // Blink away if player gets too close (< 3m)
            if (dist < 3.0f && !isAttacking)
            {
                BlinkAway();
                return;
            }

            // Attack or Special Routine
            if (attackTimer <= 0f)
            {
                if (fairyType == FairyType.Healer)
                {
                    TryHealAllies();
                }
                else if (fairyType == FairyType.WitchElite && CanSummonMinion())
                {
                    StartCoroutine(PerformSummon());
                }
                else
                {
                    StartCoroutine(CastDarkBolt());
                }
            }
        }

        private void BlinkAway()
        {
            Vector3 blinkOffset = Random.insideUnitSphere * 6.0f;
            blinkOffset.y = Mathf.Abs(blinkOffset.y) + 1.5f;
            transform.position += blinkOffset;
            Quaternion rot = transform.rotation;
            rot.Normalize();
            transform.rotation = rot;
            Debug.Log($"[FairyEnemyAI] {gameObject.name} BLINKED!");
        }

        private IEnumerator CastDarkBolt()
        {
            isAttacking = true;
            attackTimer = attackCooldown;

            yield return new WaitForSeconds(0.4f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                // Shoot Dark Bolt Projectile
                GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bolt.name = "DarkBoltProjectile";
                bolt.transform.position = transform.position;
                bolt.transform.localScale = Vector3.one * 0.4f;

                Collider col = bolt.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                Renderer r = bolt.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.7f, 0.1f, 0.9f);

                Vector3 dir = (playerTransform.position + Vector3.up * 1f - transform.position).normalized;
                float elapsed = 0f;

                while (elapsed < 1.5f && bolt != null)
                {
                    bolt.transform.position += dir * 16.0f * Time.deltaTime;
                    if (Vector3.Distance(bolt.transform.position, playerTransform.position + Vector3.up * 1f) < 0.8f)
                    {
                        DamageInfo info = new DamageInfo(12f, dir, 2f, false, gameObject);
                        playerStats.TakeDamage(info);
                        Destroy(bolt);
                        break;
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (bolt != null) Destroy(bolt);
            }

            yield return new WaitForSeconds(0.3f);
            isAttacking = false;
        }

        private void TryHealAllies()
        {
            attackTimer = attackCooldown;
            Collider[] hits = Physics.OverlapSphere(transform.position, 10.0f);
            foreach (var hit in hits)
            {
                EnemyBase ally = hit.GetComponent<EnemyBase>();
                if (ally != null && ally != this && !ally.IsDead && ally.CurrentHP < ally.MaxHP)
                {
                    ally.Heal(25f);
                    Debug.Log($"[FairyEnemyAI] Healer Fairy healed {ally.gameObject.name} for 25 HP!");
                    break;
                }
            }
        }

        private bool CanSummonMinion()
        {
            // Clean destroyed minions from active list
            activeSummonedMinions.RemoveAll(m => m == null);
            return totalSummonsExecuted < MAX_TOTAL_SUMMONS &&
                   summonCooldownTimer <= 0f &&
                   activeSummonedMinions.Count < 2;
        }

        private IEnumerator PerformSummon()
        {
            isAttacking = true;
            summonCooldownTimer = SUMMON_COOLDOWN;
            totalSummonsExecuted++;

            Debug.Log($"[Forest Witch] Summoning minion! ({totalSummonsExecuted}/{MAX_TOTAL_SUMMONS})");

            GameObject minion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            minion.name = "SummonedFairyMinion";
            minion.transform.position = transform.position + transform.right * 2.0f;
            minion.transform.localScale = Vector3.one * 0.5f;

            FairyEnemyAI ai = minion.AddComponent<FairyEnemyAI>();
            activeSummonedMinions.Add(minion);

            yield return new WaitForSeconds(0.8f);
            isAttacking = false;
        }

        protected override void Die()
        {
            if (IsDead) return;

            foreach (var minion in activeSummonedMinions)
            {
                if (minion != null) Destroy(minion);
            }
            activeSummonedMinions.Clear();

            LootResult loot = LootTable.ForFairy();
            LootDrop.SpawnFromResult(loot, transform.position);

            base.Die();
        }
    }
}
