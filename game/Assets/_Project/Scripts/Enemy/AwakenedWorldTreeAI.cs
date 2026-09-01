using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Core;
using Roguelite.Data;
using Roguelite.Environment;
using Roguelite.Items;
using Roguelite.Loot;
using Roguelite.Player;
using Roguelite.Progression;

namespace Roguelite.Enemy
{
    /// <summary>
    /// Regional Final Boss of Biome 1 — Árvore Sagrada Corrompida.
    /// Stationary colossal boss (1500 HP) that awakens after defeating the Fairy Queen.
    /// Features 2 Phases with root eruption, vine sweeps, spore clouds, root showers, and shockwaves.
    /// </summary>
    public class AwakenedWorldTreeAI : EnemyBase
    {
        [Header("Awakened World Tree Stats")]
        [SerializeField] private float vineSweepDamage = 35f;
        [SerializeField] private float vineSweepRadius = 15f;
        [SerializeField] private float rootSpikeDamage = 30f;
        [SerializeField] private float shockwaveDamage = 50f;
        [SerializeField] private float shockwaveRadius = 18f;

        private float attackCooldownTimer = 0f;
        private bool isAwakened = false;
        private bool isPhase2 = false;
        private Vector3 arenaCenterPos;

        public bool IsAwakened => isAwakened;
        public bool IsPhase2 => isPhase2;
        public override bool IsBossEnemy => true;
        public override string DisplayName => "Árvore Sagrada Corrompida";

        private readonly List<GameObject> activeMinions = new List<GameObject>();
        private readonly List<GameObject> arenaBoundaryRoots = new List<GameObject>();
        private Renderer trunkRenderer;
        private Renderer canopyRenderer;
        private Renderer faceRenderer;
        private Color normalWoodColor = new Color(0.28f, 0.18f, 0.12f);
        private Color enragedWoodColor = new Color(0.55f, 0.12f, 0.15f);

        protected override void Awake()
        {
            base.Awake();
            if (enemyData == null) enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.enemyName = "Árvore Sagrada Corrompida";
            enemyData.enemyType = EnemyType.Boss;
            enemyData.maxHealth = 1500f;
            enemyData.moveSpeed = 0f;
            enemyData.xpReward = 800;

            MaxHP = enemyData.maxHealth;
            CurrentHP = MaxHP;
        }

        protected override void Start()
        {
            base.Start();
            arenaCenterPos = transform.position;
            BuildAwakenedTreeVisuals();
        }

        public void AwakenBoss()
        {
            if (isAwakened) return;
            isAwakened = true;

            // Debug.Log("🌳 [AWAKENED WORLD TREE] — The Corrupted World Tree has awakened! (1500 HP)");

            // Spawn Arena Boundary Roots to trap player in court
            SpawnArenaBoundaryRoots();
        }

        private void BuildAwakenedTreeVisuals()
        {
            if (transform.Find("AwakenedTrunk_Visual") != null) return;

            transform.localScale = new Vector3(4.5f, 4.5f, 4.5f);

            // Gnarled Colossal Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "AwakenedTrunk_Visual";
            trunk.transform.parent = transform;
            trunk.transform.localPosition = new Vector3(0, 2.0f, 0);
            trunk.transform.localScale = new Vector3(1.3f, 2.0f, 1.3f);
            Collider tCol = trunk.GetComponent<Collider>();
            if (tCol != null) Destroy(tCol);
            trunkRenderer = trunk.GetComponent<Renderer>();
            if (trunkRenderer != null)
            {
                trunkRenderer.material.color = normalWoodColor;
            }

            // Glowing Corrupted Face (Eyes & Mouth)
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "AwakenedFace_Visual";
            face.transform.parent = transform;
            face.transform.localPosition = new Vector3(0, 2.5f, 0.6f);
            face.transform.localScale = new Vector3(0.8f, 0.5f, 0.2f);
            Collider fCol = face.GetComponent<Collider>();
            if (fCol != null) Destroy(fCol);
            faceRenderer = face.GetComponent<Renderer>();
            if (faceRenderer != null)
            {
                faceRenderer.material.color = new Color(0.95f, 0.2f, 0.85f); // Corrupted Magenta Glow
            }

            // Crown Canopy Top
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            canopy.name = "AwakenedCanopy_Visual";
            canopy.transform.parent = transform;
            canopy.transform.localPosition = new Vector3(0, 4.2f, 0);
            canopy.transform.localScale = new Vector3(3.5f, 1.4f, 3.5f);
            Collider cCol = canopy.GetComponent<Collider>();
            if (cCol != null) Destroy(cCol);
            canopyRenderer = canopy.GetComponent<Renderer>();
            if (canopyRenderer != null)
            {
                canopyRenderer.material.color = new Color(0.45f, 0.12f, 0.5f); // Deep Corrupted Purple
            }
        }

        private void SpawnArenaBoundaryRoots()
        {
            Vector3 center = new Vector3(0, 0, 680f);
            int rootCount = 12;
            for (int i = 0; i < rootCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / rootCount);
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * 32f, 0, Mathf.Sin(angle) * 32f);
                pos.y = SceneEnvironmentBuilder.GetTerrainHeightY(pos.x, pos.z);

                GameObject root = WorldPlaceholderFactory.Build(PlaceholderAssetKey.RootEmerging, transform.parent, new Color(0.5f, 0.1f, 0.2f), 2.2f);
                if (root != null)
                {
                    root.transform.position = pos;
                    root.transform.rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
                    BoxCollider col = root.AddComponent<BoxCollider>();
                    col.size = new Vector3(3f, 8f, 3f);
                    arenaBoundaryRoots.Add(root);
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            if (!isAwakened || IsDead || playerTransform == null || playerStats.IsDead || isAttacking) return;

            // Phase 2 Enrage Check at 50% HP (750 HP)
            if (!isPhase2 && CurrentHP <= MaxHP * 0.5f)
            {
                EnterPhase2();
            }

            attackCooldownTimer -= Time.deltaTime;

            if (attackCooldownTimer <= 0f)
            {
                int maxChoice = isPhase2 ? 6 : 4;
                int choice = UnityEngine.Random.Range(0, maxChoice);
                switch (choice)
                {
                    case 0:
                        StartCoroutine(PerformRootSpikes());
                        break;
                    case 1:
                        StartCoroutine(PerformVineSweep());
                        break;
                    case 2:
                        StartCoroutine(PerformSporeCloud());
                        break;
                    case 3:
                        StartCoroutine(PerformSummonCorruptedFairies());
                        break;
                    case 4:
                        if (isPhase2) StartCoroutine(PerformRootShower());
                        break;
                    case 5:
                        if (isPhase2) StartCoroutine(PerformTremorShockwave());
                        break;
                }
            }
        }

        protected override void ApplyKnockbackDecay()
        {
            // Colossal rooted tree is immune to knockback displacement
            knockbackVelocity = Vector3.zero;
        }

        public override void TakeDamage(DamageInfo damageInfo)
        {
            if (!isAwakened) AwakenBoss();
            base.TakeDamage(damageInfo);
        }

        private void EnterPhase2()
        {
            isPhase2 = true;
            if (trunkRenderer != null) trunkRenderer.material.color = enragedWoodColor;
            if (faceRenderer != null) faceRenderer.material.color = new Color(1.0f, 0.05f, 0.05f); // Deep Red Eyes

            if (Wave.EncounterManager.Instance != null)
            {
                Wave.EncounterManager.Instance.TriggerBanner("⚠️ FASE 2 — FÚRIA DA FLORESTA!");
            }

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(1.5f, 1.2f);
        }

        private float GetFlatDistanceToPlayer(Vector3 fromPos)
        {
            if (playerTransform == null) return 999f;
            Vector3 pPos = playerTransform.position;
            return Vector2.Distance(new Vector2(fromPos.x, fromPos.z), new Vector2(pPos.x, pPos.z));
        }

        private IEnumerator PerformRootSpikes()
        {
            isAttacking = true;
            attackCooldownTimer = isPhase2 ? 2.5f : 3.8f;

            Vector3 targetPos = playerTransform.position;

            // Telegraph ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "RootSpikeTelegraph";
            Collider rCol = ring.GetComponent<Collider>();
            if (rCol != null) Destroy(rCol);
            ring.transform.position = targetPos + new Vector3(0, 0.05f, 0);
            ring.transform.localScale = new Vector3(4f, 0.02f, 4f);
            Renderer rR = ring.GetComponent<Renderer>();
            if (rR != null) rR.material.color = new Color(0.95f, 0.1f, 0.1f, 0.6f);

            yield return new WaitForSeconds(0.9f);
            Destroy(ring);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                GameObject rootSpike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rootSpike.name = "EruptedRootSpike";
                rootSpike.transform.position = targetPos + new Vector3(0, 1.5f, 0);
                rootSpike.transform.localScale = new Vector3(1.2f, 3.0f, 1.2f);
                Collider sCol = rootSpike.GetComponent<Collider>();
                if (sCol != null) Destroy(sCol);
                Renderer sR = rootSpike.GetComponent<Renderer>();
                if (sR != null) sR.material.color = new Color(0.35f, 0.15f, 0.08f);

                if (GetFlatDistanceToPlayer(targetPos) <= 3.2f)
                {
                    Vector3 kbDir = (playerTransform.position - targetPos).normalized;
                    DamageInfo damage = new DamageInfo(rootSpikeDamage * (isPhase2 ? 1.3f : 1.0f), kbDir, 10f, false, gameObject);
                    playerStats.TakeDamage(damage);
                }

                Destroy(rootSpike, 1.2f);
            }

            yield return new WaitForSeconds(0.3f);
            isAttacking = false;
        }

        private IEnumerator PerformVineSweep()
        {
            isAttacking = true;
            attackCooldownTimer = isPhase2 ? 3.0f : 4.2f;

            yield return new WaitForSeconds(0.5f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = GetFlatDistanceToPlayer(transform.position);
                if (dist <= vineSweepRadius)
                {
                    Vector3 kbDir = (playerTransform.position - transform.position).normalized;
                    DamageInfo damage = new DamageInfo(vineSweepDamage * (isPhase2 ? 1.3f : 1.0f), kbDir, 12f, false, gameObject);
                    playerStats.TakeDamage(damage);
                }

                ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
                if (cam != null) cam.TriggerShake(0.6f, 0.4f);
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private IEnumerator PerformSporeCloud()
        {
            isAttacking = true;
            attackCooldownTimer = isPhase2 ? 4.0f : 5.5f;

            Vector3 spawnPos = arenaCenterPos + UnityEngine.Random.insideUnitSphere * 12f;
            spawnPos.y = SceneEnvironmentBuilder.GetTerrainHeightY(spawnPos.x, spawnPos.z) + 1.2f;

            GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "ToxicSporeCloud";
            cloud.transform.position = spawnPos;
            cloud.transform.localScale = Vector3.one * 6f;
            Collider col = cloud.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Renderer rR = cloud.GetComponent<Renderer>();
            if (rR != null) rR.material.color = new Color(0.4f, 0.85f, 0.2f, 0.45f);

            float elapsed = 0f;
            while (elapsed < 5.0f && !IsDead)
            {
                elapsed += Time.deltaTime;
                if (playerStats != null && Vector3.Distance(playerTransform.position, spawnPos) <= 4.0f)
                {
                    DamageInfo info = new DamageInfo(8f * Time.deltaTime * 5f, Vector3.zero, 0f, false, gameObject);
                    playerStats.TakeDamage(info);
                }
                yield return null;
            }

            Destroy(cloud);
            isAttacking = false;
        }

        private IEnumerator PerformSummonCorruptedFairies()
        {
            isAttacking = true;
            attackCooldownTimer = 15.0f;

            for (int i = 0; i < 2; i++)
            {
                Vector3 spawnPos = transform.position + (i == 0 ? transform.right : -transform.right) * 6f;
                spawnPos.y += 2f;
                GameObject fairy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fairy.name = "CorruptedFairyMinion";
                fairy.transform.position = spawnPos;
                fairy.transform.localScale = Vector3.one * 0.9f;

                FairyEnemyAI fairyAI = fairy.AddComponent<FairyEnemyAI>();
                activeMinions.Add(fairy);
            }

            yield return new WaitForSeconds(0.6f);
            isAttacking = false;
        }

        private IEnumerator PerformRootShower()
        {
            isAttacking = true;
            attackCooldownTimer = 6.0f;

            for (int i = 0; i < 5; i++)
            {
                if (IsDead || playerStats == null || playerStats.IsDead) break;

                Vector3 dropPos = playerTransform.position + UnityEngine.Random.insideUnitSphere * 4f;
                dropPos.y = SceneEnvironmentBuilder.GetTerrainHeightY(dropPos.x, dropPos.z);

                StartCoroutine(DropRootSpear(dropPos));
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(0.5f);
            isAttacking = false;
        }

        private IEnumerator DropRootSpear(Vector3 targetPos)
        {
            GameObject spear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spear.name = "RootSpearFalling";
            spear.transform.position = targetPos + Vector3.up * 15f;
            spear.transform.localScale = new Vector3(0.6f, 2.5f, 0.6f);
            Collider sCol = spear.GetComponent<Collider>();
            if (sCol != null) Destroy(sCol);

            float speed = 25f;
            while (spear.transform.position.y > targetPos.y + 0.5f && spear != null)
            {
                spear.transform.position += Vector3.down * speed * Time.deltaTime;
                yield return null;
            }

            if (spear != null)
            {
                if (playerStats != null && Vector3.Distance(playerTransform.position, targetPos) < 2.5f)
                {
                    DamageInfo info = new DamageInfo(28f, Vector3.up, 5f, false, gameObject);
                    playerStats.TakeDamage(info);
                }
                Destroy(spear, 0.8f);
            }
        }

        private IEnumerator PerformTremorShockwave()
        {
            isAttacking = true;
            attackCooldownTimer = 7.0f;

            GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shockwave.name = "TremorShockwave";
            Collider col = shockwave.GetComponent<Collider>();
            if (col != null) Destroy(col);
            shockwave.transform.position = transform.position + new Vector3(0, 0.1f, 0);
            shockwave.transform.localScale = new Vector3(1f, 0.05f, 1f);

            Renderer rR = shockwave.GetComponent<Renderer>();
            if (rR != null) rR.material.color = new Color(0.95f, 0.2f, 0.1f, 0.6f);

            float radius = 1f;
            while (radius < shockwaveRadius && shockwave != null)
            {
                radius += Time.deltaTime * 22f;
                shockwave.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);

                if (playerStats != null && Mathf.Abs(GetFlatDistanceToPlayer(transform.position) - radius) < 1.5f)
                {
                    Vector3 kbDir = (playerTransform.position - transform.position).normalized;
                    DamageInfo damage = new DamageInfo(shockwaveDamage, kbDir, 16f, true, gameObject);
                    playerStats.TakeDamage(damage);
                }

                yield return null;
            }

            Destroy(shockwave);
            isAttacking = false;
        }

        protected override void Die()
        {
            if (IsDead) return;

            foreach (var m in activeMinions) if (m != null) Destroy(m);
            activeMinions.Clear();

            foreach (var r in arenaBoundaryRoots) if (r != null) Destroy(r);
            arenaBoundaryRoots.Clear();

            // 800 XP Reward
            if (playerStats != null) playerStats.AddXP(800);
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.AddXP(800);

            // 1. Spawn Fairy Crown
            ItemData crown = ItemDatabase.Get("accessory_fairy_queen_crown");
            if (crown != null) LootDrop.SpawnSingle(transform.position + Vector3.right * 2f, crown, 1);

            // 2. Spawn Fairy Dust
            ItemData dust = ItemDatabase.Get("material_fairy_dust");
            if (dust != null) LootDrop.SpawnSingle(transform.position + Vector3.left * 2f, dust, 1);

            // 3. Guaranteed Epic/Legendary Equipment Drop
            ItemData equip = ItemDatabase.Get("weapon_dawnbringer") ?? ItemDatabase.Get("ring_of_shadows");
            if (equip != null) LootDrop.SpawnSingle(transform.position + Vector3.forward * 2f, equip, 1);

            // 4. Guaranteed Legendary Chest
            var chest = SceneEnvironmentBuilder.SpawnInteractiveTreasureChest(transform.position + Vector3.forward * 4f, Quaternion.identity, ChestRarity.Legendary);
            if (chest != null) chest.name = "AwakenedWorldTreeLegendaryChest";

            // 5. Trigger Boss Defeat Progression & Weakened Destructible Root Exit
            BossDefeatProgression progression = FindFirstObjectByType<BossDefeatProgression>();
            if (progression != null)
            {
                progression.ExecuteWorldTreeDefeatSequence();
            }

            if (Wave.EncounterManager.Instance != null)
            {
                Wave.EncounterManager.Instance.TriggerBanner("🌿 O CORAÇÃO DA FLORESTA FOI PURIFICADO!");
            }

            base.Die();
        }
    }
}
