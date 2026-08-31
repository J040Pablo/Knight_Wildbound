using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Loot;
using Roguelite.Player;
using Roguelite.Progression;

namespace Roguelite.Enemy
{
    public class StoneGiantAI : EnemyBase
    {
        [Header("Stone Giant Settings")]
        [SerializeField] private bool isColossusMiniBoss = false;
        [SerializeField] private float ambushActivationDistance = 10.0f;
        [SerializeField] private float slamRadius = 6.0f;
        [SerializeField] private float slamDamage = 25.0f;

        private bool isAwakened = false;
        private float attackTimer = 0f;

        public override bool IsBossEnemy => isColossusMiniBoss;
        public override string DisplayName => isColossusMiniBoss ? "Ancient Colossus" : "Stone Giant";

        public bool IsColossus => isColossusMiniBoss;
        public bool IsAwakened => isAwakened;

        public void SetAsColossusMiniBoss()
        {
            isColossusMiniBoss = true;
            MaxHP = 250f;
            CurrentHP = MaxHP;
            transform.localScale = Vector3.one * 1.8f;
        }

        protected override void Awake()
        {
            base.Awake();
            MaxHP = isColossusMiniBoss ? 250f : 120f;
            CurrentHP = MaxHP;
        }

        protected override void Start()
        {
            base.Start();
            if (meshRenderer != null)
            {
                meshRenderer.material.color = isColossusMiniBoss ? new Color(0.35f, 0.38f, 0.32f) : new Color(0.45f, 0.48f, 0.42f); // Mossy Rock Gray
            }
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || playerTransform == null || playerStats.IsDead || isAttacking) return;

            // Sleeping Boulder Ambush Logic
            if (!isAwakened)
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distToPlayer <= ambushActivationDistance)
                {
                    StartCoroutine(EruptAmbush());
                }
                return;
            }

            if (!SafeCanMove()) return;

            attackTimer -= Time.deltaTime;
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            // Turn towards player
            Vector3 lookDir = (playerTransform.position - transform.position);
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(lookDir, Vector3.up);
                rot.Normalize();
                transform.rotation = rot;
            }

            if (dist > slamRadius && attackTimer <= 0f)
            {
                StartCoroutine(PerformBoulderThrow());
            }
            else if (dist <= slamRadius && attackTimer <= 0f)
            {
                StartCoroutine(PerformGroundSlam());
            }
        }

        private IEnumerator EruptAmbush()
        {
            isAttacking = true;
            Debug.Log($"🌋 [STONE GIANT AMBUSH] — {(isColossusMiniBoss ? "ANCIENT COLOSSUS" : "STONE GIANT")} ERUPT FROM GROUND!");

            // Eruption shockwave animation / camera shake
            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.6f, 0.4f);

            yield return new WaitForSeconds(0.8f);
            isAwakened = true;
            isAttacking = false;
        }

        private IEnumerator PerformGroundSlam()
        {
            isAttacking = true;
            attackTimer = isColossusMiniBoss ? 3.5f : 5.0f;

            // Telegraph
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "GiantSlamTelegraph";
            Collider iCol = indicator.GetComponent<Collider>();
            if (iCol != null) Destroy(iCol);

            indicator.transform.position = transform.position + new Vector3(0, 0.05f, 0);
            indicator.transform.localScale = new Vector3(slamRadius * 2f, 0.02f, slamRadius * 2f);
            Renderer r = indicator.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.9f, 0.3f, 0.1f, 0.5f);

            yield return new WaitForSeconds(1.0f);
            Destroy(indicator);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= slamRadius)
                {
                    Vector3 kbDir = (playerTransform.position - transform.position).normalized;
                    DamageInfo info = new DamageInfo(slamDamage * (isColossusMiniBoss ? 1.4f : 1.0f), kbDir, 12f, true, gameObject);
                    playerStats.TakeDamage(info);
                }

                ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
                if (cam != null) cam.TriggerShake(0.4f, 0.3f);
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private IEnumerator PerformBoulderThrow()
        {
            isAttacking = true;
            attackTimer = isColossusMiniBoss ? 4.0f : 6.0f;

            yield return new WaitForSeconds(0.5f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                boulder.name = "ThrownBoulder";
                boulder.transform.position = transform.position + Vector3.up * 2.0f;
                boulder.transform.localScale = Vector3.one * 1.2f;

                Renderer r = boulder.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.4f, 0.4f, 0.38f);

                Vector3 targetPos = playerTransform.position;
                Vector3 dir = (targetPos - boulder.transform.position).normalized;
                float elapsed = 0f;

                while (elapsed < 1.8f && boulder != null)
                {
                    boulder.transform.position += dir * 14.0f * Time.deltaTime;
                    if (Vector3.Distance(boulder.transform.position, playerTransform.position) < 1.2f)
                    {
                        DamageInfo info = new DamageInfo(18f, dir, 10f, false, gameObject);
                        playerStats.TakeDamage(info);
                        Destroy(boulder);
                        break;
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (boulder != null) Destroy(boulder);
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        protected override void Die()
        {
            if (IsDead) return;

            if (isColossusMiniBoss)
            {
                // Award 150 XP for Ancient Colossus
                if (playerStats != null) playerStats.AddXP(150);
                if (ProgressionManager.Instance != null) ProgressionManager.Instance.AddXP(150);

                LootResult colossusLoot = LootTable.ForColossusMiniBoss();
                LootDrop.SpawnFromResult(colossusLoot, transform.position);

                // Spawn guaranteed reward Treasure Chest
                var chest = Environment.SceneEnvironmentBuilder.SpawnInteractiveTreasureChest(transform.position + Vector3.forward * 2f, Quaternion.identity, ChestRarity.Rare);
                if (chest != null) chest.name = "ColossusRewardChest";
            }
            else
            {
                LootResult loot = LootTable.ForStoneGiant();
                LootDrop.SpawnFromResult(loot, transform.position);
            }

            base.Die();
        }
    }
}
