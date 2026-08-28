using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Core;

namespace Roguelite.Enemy
{
    public class HollowTreeBossAI : EnemyBase
    {
        [Header("Hollow Tree Boss Specifics")]
        [SerializeField] private float branchSweepDamage = 30f;
        [SerializeField] private float rootStrikeDamage = 25f;
        [SerializeField] private float groundSlamDamage = 45f;
        [SerializeField] private float groundSlamRadius = 7.5f;

        private float attackCooldownTimer = 0f;
        private bool isAttacking = false;
        private bool isPhase2 = false;

        public bool IsPhase2 => isPhase2;
        public event Action<float, float> OnBossHealthChanged;

        private List<GameObject> activeSummonedMinions = new List<GameObject>();

        protected override void Awake()
        {
            base.Awake();
            MaxHP = 750f;
            CurrentHP = MaxHP;
        }

        protected override void Start()
        {
            base.Start();
            BuildHollowTreeVisuals();
            OnBossHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        private void BuildHollowTreeVisuals()
        {
            transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);

            // Gnarled Trunk with Carved Face
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "HollowTreeTrunk_Visual";
            trunk.transform.parent = transform;
            trunk.transform.localPosition = new Vector3(0, 1.8f, 0);
            trunk.transform.localScale = new Vector3(1.2f, 1.8f, 1.2f);
            Collider tCol = trunk.GetComponent<Collider>();
            if (tCol != null) DestroyImmediate(tCol);
            meshRenderer = trunk.GetComponent<Renderer>();
            if (meshRenderer != null)
            {
                originalColor = new Color(0.32f, 0.2f, 0.12f); // Dark corrupted wood
                meshRenderer.material.color = originalColor;
            }

            // Glowing Corrupted Eyes/Mouth Face
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "HollowTreeFace_Visual";
            face.transform.parent = transform;
            face.transform.localPosition = new Vector3(0, 2.3f, 0.55f);
            face.transform.localScale = new Vector3(0.7f, 0.4f, 0.15f);
            Collider fCol = face.GetComponent<Collider>();
            if (fCol != null) DestroyImmediate(fCol);
            Renderer fR = face.GetComponent<Renderer>();
            if (fR != null) fR.material.color = new Color(0.95f, 0.7f, 0.1f); // Glowing amber eyes

            // Autumn Canopy Top
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            canopy.name = "HollowTreeCanopy_Visual";
            canopy.transform.parent = transform;
            canopy.transform.localPosition = new Vector3(0, 3.6f, 0);
            canopy.transform.localScale = new Vector3(3.0f, 1.2f, 3.0f);
            Collider cCol = canopy.GetComponent<Collider>();
            if (cCol != null) DestroyImmediate(cCol);
            Renderer cR = canopy.GetComponent<Renderer>();
            if (cR != null) cR.material.color = new Color(0.85f, 0.35f, 0.08f); // Autumn orange leaves
        }

        protected override void Update()
        {
            base.Update();
            // Strict check: Boss must be activated by trigger before attacking or running state logic
            if (!Environment.BossActivationTrigger.IsBossActivated || IsDead || playerTransform == null || playerStats.IsDead || isAttacking) return;

            // Check Phase 2 Transition
            if (!isPhase2 && CurrentHP <= MaxHP * 0.5f)
            {
                EnterPhase2();
            }

            attackCooldownTimer -= Time.deltaTime;

            if (attackCooldownTimer <= 0)
            {
                // Choose Attack Pattern
                int choice = UnityEngine.Random.Range(0, isPhase2 ? 4 : 3);
                switch (choice)
                {
                    case 0:
                        StartCoroutine(PerformBranchSweep());
                        break;
                    case 1:
                        StartCoroutine(PerformRootStrike());
                        break;
                    case 2:
                        StartCoroutine(PerformGroundSlam());
                        break;
                    case 3:
                        if (isPhase2) StartCoroutine(PerformSummonSaplings());
                        break;
                }
            }
        }

        public override void TakeDamage(DamageInfo damageInfo)
        {
            base.TakeDamage(damageInfo);
            OnBossHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        private void EnterPhase2()
        {
            isPhase2 = true;
            if (meshRenderer != null)
            {
                originalColor = new Color(0.55f, 0.1f, 0.18f); // Corrupted dark purple/red
                meshRenderer.material.color = originalColor;
            }

            // Phase 2 Banner notification via EncounterManager or HUD
            var encMgr = FindFirstObjectByType<Wave.EncounterManager>();
            if (encMgr != null)
            {
                encMgr.TriggerBanner("⚠️ HOLLOW TREE ENRAGED! PHASE 2 STARTED!");
            }
        }

        private IEnumerator PerformBranchSweep()
        {
            isAttacking = true;
            attackCooldownTimer = isPhase2 ? 2.5f : 3.8f;

            // Face player
            Vector3 lookDir = (playerTransform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

            yield return new WaitForSeconds(0.4f);

            if (!IsDead && Environment.BossActivationTrigger.IsBossActivated && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= 8.5f)
                {
                    DamageInfo damage = new DamageInfo(
                        branchSweepDamage * (isPhase2 ? 1.3f : 1.0f),
                        (playerTransform.position - transform.position).normalized,
                        8.0f,
                        false,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private IEnumerator PerformRootStrike()
        {
            isAttacking = true;
            attackCooldownTimer = isPhase2 ? 3.0f : 4.5f;

            // Spawn root under player position
            Vector3 rootPos = playerTransform.position;

            // Telegraph marker
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "RootStrikeTelegraph";
            Collider mCol = marker.GetComponent<Collider>();
            if (mCol != null) DestroyImmediate(mCol);
            marker.transform.position = rootPos + new Vector3(0, 0.05f, 0);
            marker.transform.localScale = new Vector3(3f, 0.02f, 3f);
            Renderer mR = marker.GetComponent<Renderer>();
            if (mR != null) mR.material.color = new Color(0.9f, 0.1f, 0.1f, 0.5f);

            yield return new WaitForSeconds(1.0f);
            Destroy(marker);

            if (!IsDead && Environment.BossActivationTrigger.IsBossActivated && playerStats != null && !playerStats.IsDead)
            {
                // Erupt Root Spike
                GameObject rootSpike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rootSpike.name = "EruptedRoot";
                rootSpike.transform.position = rootPos + new Vector3(0, 1.2f, 0);
                rootSpike.transform.localScale = new Vector3(0.8f, 2.5f, 0.8f);
                Collider sCol = rootSpike.GetComponent<Collider>();
                if (sCol != null) DestroyImmediate(sCol);
                Renderer rR = rootSpike.GetComponent<Renderer>();
                if (rR != null) rR.material.color = new Color(0.25f, 0.15f, 0.08f);

                float dist = Vector3.Distance(rootPos, playerTransform.position);
                if (dist <= 2.2f)
                {
                    DamageInfo damage = new DamageInfo(
                        rootStrikeDamage * (isPhase2 ? 1.35f : 1.0f),
                        Vector3.up,
                        10.0f,
                        false,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }

                Destroy(rootSpike, 1.2f);
            }

            yield return new WaitForSeconds(0.3f);
            isAttacking = false;
        }

        private IEnumerator PerformGroundSlam()
        {
            isAttacking = true;
            attackCooldownTimer = isPhase2 ? 4.0f : 5.5f;

            // Telegraph Circle expanding
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "BossSlamTelegraph";
            Collider iCol = indicator.GetComponent<Collider>();
            if (iCol != null) DestroyImmediate(iCol);
            indicator.transform.position = transform.position + new Vector3(0, 0.05f, 0);
            indicator.transform.localScale = new Vector3(groundSlamRadius * 2f, 0.02f, groundSlamRadius * 2f);

            Renderer indRenderer = indicator.GetComponent<Renderer>();
            if (indRenderer != null) indRenderer.material.color = new Color(1.0f, 0.2f, 0.2f, 0.5f);

            yield return new WaitForSeconds(1.1f);
            Destroy(indicator);

            if (!IsDead && Environment.BossActivationTrigger.IsBossActivated && playerStats != null && !playerStats.IsDead)
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distToPlayer <= groundSlamRadius)
                {
                    Vector3 knockbackDir = (playerTransform.position - transform.position).normalized;
                    knockbackDir.y = 0.6f;
                    DamageInfo damage = new DamageInfo(
                        groundSlamDamage * (isPhase2 ? 1.3f : 1.0f),
                        knockbackDir,
                        14.0f,
                        true,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
        }

        private IEnumerator PerformSummonSaplings()
        {
            isAttacking = true;
            attackCooldownTimer = 6.0f;

            // Summon 2 Corrupted Saplings (small pumpkin/slime AI trees)
            for (int i = 0; i < 2; i++)
            {
                Vector3 spawnPos = transform.position + Quaternion.Euler(0, i * 180 + 90, 0) * Vector3.forward * 5f;
                GameObject saplingObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                saplingObj.name = "CorruptedSapling";
                saplingObj.transform.position = spawnPos;

                Collider col = saplingObj.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                CharacterController cc = saplingObj.AddComponent<CharacterController>();
                cc.height = 1.6f;
                cc.radius = 0.4f;

                PumpkinEnemyAI saplingAI = saplingObj.AddComponent<PumpkinEnemyAI>();
                activeSummonedMinions.Add(saplingObj);
            }

            yield return new WaitForSeconds(0.5f);
            isAttacking = false;
        }

        protected override void Die()
        {
            if (IsDead) return;
            IsDead = true;

            // Stop all boss attacks & clear summoned minions
            StopAllCoroutines();
            foreach (var minion in activeSummonedMinions)
            {
                if (minion != null) Destroy(minion);
            }
            activeSummonedMinions.Clear();

            // Give XP
            if (playerStats != null) playerStats.AddXP(500);

            // Trigger Victory state in RunManager
            RunManager runManager = FindFirstObjectByType<RunManager>();
            if (runManager != null)
            {
                runManager.TriggerVictory();
            }

            base.Die();
        }
    }
}
