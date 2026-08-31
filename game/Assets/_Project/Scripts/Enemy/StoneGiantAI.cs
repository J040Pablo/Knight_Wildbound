using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Loot;
using Roguelite.Player;
using Roguelite.Progression;

namespace Roguelite.Enemy
{
    /// <summary>
    /// Stone Giant Elite Enemy AI.
    /// Massive 3x tall rock creature featuring Rock Slam, Heavy Punch, 30% HP Enrage state,
    /// heavy footsteps, 15m ambush activation, and 600 HP.
    /// </summary>
    public class StoneGiantAI : EnemyBase
    {
        [Header("Stone Giant Stats")]
        [SerializeField] private bool isColossusMiniBoss = false;
        [SerializeField] private float activationDistance = 12.0f; // 12m activation radius
        [SerializeField] private float baseMoveSpeed = 2.5f;       // Move speed 2.5
        [SerializeField] private float rockSlamRadius = 4.0f;       // 4m slam radius
        [SerializeField] private float rockSlamDamage = 25.0f;      // 25 damage
        [SerializeField] private float heavyPunchDamage = 15.0f;    // 15 damage
        [SerializeField] private float heavyPunchRange = 3.0f;      // 3m range

        private bool isAwakened = false;
        private bool isEnraged = false;
        private float attackTimer = 0f;
        private Vector3 spawnPos;
        private Transform leftArmTransform;
        private Transform rightArmTransform;
        private Renderer coreRenderer;

        public override bool IsBossEnemy => isColossusMiniBoss;
        public override string DisplayName => isColossusMiniBoss ? "Ancient Colossus" : "Stone Giant";

        public bool IsColossus => isColossusMiniBoss;
        public bool IsAwakened => isAwakened;

        public void SetAsColossusMiniBoss()
        {
            isColossusMiniBoss = true;
            MaxHP = 800f;
            CurrentHP = MaxHP;
            transform.localScale = new Vector3(3.2f, 4.0f, 3.2f);
        }

        protected override void Awake()
        {
            base.Awake();
            MaxHP = isColossusMiniBoss ? 800f : 600f; // 600 HP for Stone Giant
            CurrentHP = MaxHP;
            transform.localScale = new Vector3(2.6f, 3.2f, 2.6f); // 3x taller than player
        }

        protected override void Start()
        {
            base.Start();
            spawnPos = transform.position;
            BuildStoneGiantVisuals();
        }

        private void BuildStoneGiantVisuals()
        {
            if (transform.Find("GiantBody_Visual") != null) return;

            Color stoneColor = isColossusMiniBoss ? new Color(0.35f, 0.38f, 0.32f) : new Color(0.45f, 0.48f, 0.42f);

            // 1. Torso
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "GiantBody_Visual";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0, 1.4f, 0);
            body.transform.localScale = new Vector3(1.1f, 1.2f, 0.8f);
            StripCollider(body);
            SetMaterialColor(body, stoneColor);

            // 2. Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "GiantHead_Visual";
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0, 2.3f, 0.1f);
            head.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            StripCollider(head);
            SetMaterialColor(head, stoneColor);

            // Core Glowing Eye
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "GiantCore_Glow";
            core.transform.SetParent(head.transform, false);
            core.transform.localPosition = new Vector3(0, 0, 0.55f);
            core.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            StripCollider(core);
            coreRenderer = core.GetComponent<Renderer>();
            if (coreRenderer != null) coreRenderer.material.color = new Color(0.95f, 0.65f, 0.15f); // Amber Orange

            // 3. Massive Arms
            GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armL.name = "GiantArm_L";
            armL.transform.SetParent(transform, false);
            armL.transform.localPosition = new Vector3(-0.85f, 1.3f, 0);
            armL.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
            armL.transform.localRotation = Quaternion.Euler(15f, 0, -10f);
            StripCollider(armL);
            SetMaterialColor(armL, stoneColor);
            leftArmTransform = armL.transform;

            GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armR.name = "GiantArm_R";
            armR.transform.SetParent(transform, false);
            armR.transform.localPosition = new Vector3(0.85f, 1.3f, 0);
            armR.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
            armR.transform.localRotation = Quaternion.Euler(15f, 0, 10f);
            StripCollider(armR);
            SetMaterialColor(armR, stoneColor);
            rightArmTransform = armR.transform;

            // 4. Thick Legs
            GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            legL.name = "GiantLeg_L";
            legL.transform.SetParent(transform, false);
            legL.transform.localPosition = new Vector3(-0.4f, 0.4f, 0);
            legL.transform.localScale = new Vector3(0.45f, 0.4f, 0.45f);
            StripCollider(legL);
            SetMaterialColor(legL, stoneColor);

            GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            legR.name = "GiantLeg_R";
            legR.transform.SetParent(transform, false);
            legR.transform.localPosition = new Vector3(0.4f, 0.4f, 0);
            legR.transform.localScale = new Vector3(0.45f, 0.4f, 0.45f);
            StripCollider(legR);
            SetMaterialColor(legR, stoneColor);
        }

        private void StripCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }

        private void SetMaterialColor(GameObject go, Color color)
        {
            if (go.TryGetComponent<Renderer>(out var r))
            {
                r.material.color = color;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || isAttacking) return;

            // Check Enrage State (< 30% HP)
            if (!isEnraged && CurrentHP / MaxHP <= 0.30f)
            {
                TriggerEnrage();
            }

            // Sleeping / Guard Activation Check (12m radius with Line-of-Sight)
            if (!isAwakened)
            {
                if (playerTransform != null && !playerStats.IsDead)
                {
                    float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                    if (distToPlayer <= activationDistance)
                    {
                        // Raycast line of sight check (No awakening through hills or thick obstacles)
                        Vector3 eyePos = transform.position + Vector3.up * 2.0f;
                        Vector3 targetEyePos = playerTransform.position + Vector3.up * 1.0f;
                        Vector3 eyeDir = targetEyePos - eyePos;

                        bool hasLOS = true;
                        if (Physics.Raycast(eyePos, eyeDir.normalized, out RaycastHit wallHit, distToPlayer))
                        {
                            if (!wallHit.collider.isTrigger && !wallHit.collider.CompareTag("Player") && wallHit.distance < distToPlayer - 0.5f)
                            {
                                hasLOS = false;
                            }
                        }

                        if (hasLOS)
                        {
                            StartCoroutine(EruptAmbush());
                        }
                    }
                }
                return;
            }

            if (playerTransform == null || playerStats.IsDead || !SafeCanMove()) return;

            // Encounter Area Boundary Leash Check (25m from spawn position)
            float distFromSpawn = Vector3.Distance(transform.position, spawnPos);
            float distToPlayerActual = Vector3.Distance(transform.position, playerTransform.position);

            if (distFromSpawn > 25f)
            {
                // Return to spawn position
                Vector3 returnDir = (spawnPos - transform.position).normalized;
                characterController.Move(returnDir * baseMoveSpeed * Time.deltaTime);
                return;
            }

            attackTimer -= Time.deltaTime;

            // Turn towards player
            Vector3 lookDir = (playerTransform.position - transform.position);
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                rot.Normalize();
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 6f);
            }

            // Attack Decision
            if (attackTimer <= 0f)
            {
                if (distToPlayerActual <= heavyPunchRange && Random.value < 0.6f)
                {
                    StartCoroutine(PerformHeavyPunch());
                }
                else if (distToPlayerActual <= rockSlamRadius + 2.0f)
                {
                    StartCoroutine(PerformRockSlam());
                }
                else
                {
                    // Move closer
                    float speed = isEnraged ? baseMoveSpeed * 1.25f : baseMoveSpeed;
                    characterController.Move(lookDir.normalized * speed * Time.deltaTime);
                }
            }
            else
            {
                // Move towards player while waiting for attack cooldown
                float speed = isEnraged ? baseMoveSpeed * 1.25f : baseMoveSpeed;
                if (distToPlayerActual > heavyPunchRange)
                {
                    characterController.Move(lookDir.normalized * speed * Time.deltaTime);
                }
            }
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            Debug.Log($"🔥 [STONE GIANT ENRAGE] — HP below 30%! Move speed +25%, attack speed +20%!");

            if (coreRenderer != null)
            {
                coreRenderer.material.color = new Color(1.0f, 0.2f, 0.05f); // Glowing Fiery Red
            }

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.5f, 0.4f);
        }

        private IEnumerator EruptAmbush()
        {
            isAttacking = true;
            Debug.Log($"🌋 [STONE GIANT] — Awakened at 15m radius!");

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.6f, 0.4f);

            yield return new WaitForSeconds(0.6f);
            isAwakened = true;
            isAttacking = false;
        }

        private IEnumerator PerformRockSlam()
        {
            isAttacking = true;
            float cd = isEnraged ? 3.2f : 4.0f; // 4s base cooldown (3.2s when enraged)
            attackTimer = cd;

            // Raise both arms telegraph
            if (leftArmTransform != null) leftArmTransform.localRotation = Quaternion.Euler(-90f, 0, -10f);
            if (rightArmTransform != null) rightArmTransform.localRotation = Quaternion.Euler(-90f, 0, 10f);

            // Telegraph ring on ground
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "GiantSlamTelegraph";
            StripCollider(indicator);
            indicator.transform.position = transform.position + new Vector3(0, 0.05f, 0);
            indicator.transform.localScale = new Vector3(rockSlamRadius * 2f, 0.02f, rockSlamRadius * 2f);

            if (indicator.TryGetComponent<Renderer>(out var r))
            {
                r.material.color = new Color(0.95f, 0.35f, 0.1f, 0.5f);
            }

            yield return new WaitForSeconds(0.8f);
            Destroy(indicator);

            // Slam arms down
            if (leftArmTransform != null) leftArmTransform.localRotation = Quaternion.Euler(30f, 0, -10f);
            if (rightArmTransform != null) rightArmTransform.localRotation = Quaternion.Euler(30f, 0, 10f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= rockSlamRadius)
                {
                    Vector3 kbDir = (playerTransform.position - transform.position).normalized;
                    DamageInfo info = new DamageInfo(rockSlamDamage, kbDir, 14f, true, gameObject);
                    playerStats.TakeDamage(info);
                }

                ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
                if (cam != null) cam.TriggerShake(0.5f, 0.3f);
            }

            yield return new WaitForSeconds(0.4f);

            // Reset arms
            if (leftArmTransform != null) leftArmTransform.localRotation = Quaternion.Euler(15f, 0, -10f);
            if (rightArmTransform != null) rightArmTransform.localRotation = Quaternion.Euler(15f, 0, 10f);

            isAttacking = false;
        }

        private IEnumerator PerformHeavyPunch()
        {
            isAttacking = true;
            float cd = isEnraged ? 1.6f : 2.0f; // 2s base cooldown (1.6s when enraged)
            attackTimer = cd;

            // Telegraph right arm punch
            if (rightArmTransform != null) rightArmTransform.localRotation = Quaternion.Euler(-60f, 20f, 10f);
            yield return new WaitForSeconds(0.4f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= heavyPunchRange + 0.5f)
                {
                    Vector3 dir = (playerTransform.position - transform.position).normalized;
                    DamageInfo info = new DamageInfo(heavyPunchDamage, dir, 8f, false, gameObject);
                    playerStats.TakeDamage(info);
                }
            }

            yield return new WaitForSeconds(0.2f);
            if (rightArmTransform != null) rightArmTransform.localRotation = Quaternion.Euler(15f, 0, 10f);
            isAttacking = false;
        }

        protected override void Die()
        {
            if (IsDead) return;

            // 150 XP Reward
            if (playerStats != null) playerStats.AddXP(150);
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.AddXP(150);

            if (isColossusMiniBoss)
            {
                LootResult colossusLoot = LootTable.ForColossusMiniBoss();
                LootDrop.SpawnFromResult(colossusLoot, transform.position);

                var chest = Environment.SceneEnvironmentBuilder.SpawnInteractiveTreasureChest(transform.position + Vector3.forward * 2f, Quaternion.identity, ChestRarity.Rare);
                if (chest != null) chest.name = "ColossusRewardChest";
            }
            else
            {
                LootResult loot = LootTable.ForStoneGiant();
                LootDrop.SpawnFromResult(loot, transform.position);
            }

            Debug.Log($"🪨 [STONE GIANT DEFEATED] — 150 XP awarded!");
            base.Die();
        }
    }
}
