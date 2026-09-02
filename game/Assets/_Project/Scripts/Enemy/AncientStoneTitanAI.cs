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
    /// Flagship World Boss encounter: Ancient Stone Titan.
    /// Mountain-sized colossal boss featuring multi-phase combat:
    /// - Phase 1 Ground Combat: Titan Stomp (jumpable shockwaves), Titan Grab (close-range punishment),
    ///   Boulder Throw (creates rock cover), Earthquake Roar & Falling Rocks (dynamic arena changing hazards).
    /// - Exhaustion & Kneeling State: Kneels for 12s, allowing climbing onto legs/back/nape.
    /// - Multi-Stage Crystal Exposure: 3-Stage weakspot progression (Cracked -> Exposed Core -> Shattered Finisher).
    /// - Ground Validation & Anti-Stuck System: Strict terrain height clamping & anti-prop climbing safeguards.
    /// - Phase 2 Enrage (30% HP): Magma veins, faster shockwaves, shorter kneeling duration (7s).
    /// </summary>
    public class AncientStoneTitanAI : EnemyBase
    {
        [Header("Ancient Stone Titan Settings")]
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float stompRadius = 8.5f;
        [SerializeField] private float stompDamage = 25f;
        [SerializeField] private float grabRange = 4.5f;
        [SerializeField] private float grabDamage = 35f;

        [Header("Stagger & Kneeling Settings")]
        [SerializeField] private float maxStagger = 300f;
        private float currentStagger = 0f;

        [Header("Detection Ranges")]
        [SerializeField] private float detectionRadius = 35f;
        [SerializeField] private float loseTargetRadius = 60f;

        public override bool IsBossEnemy => true;
        public override string DisplayName => "Ancient Stone Titan";

        private bool isEnraged = false;
        private bool isKneeling = false;
        private bool inCombat = false;
        private float closeFootTimer = 0f;
        private Vector3 spawnPos;
        private GameObject titanModel;
        private TitanClimbNode climbNode;
        private Renderer titanRenderer;
        private Renderer crystalRenderer;
        private List<TitanHitZone> hitZones = new List<TitanHitZone>();

        private static readonly Color NormalStoneColor = new Color(0.35f, 0.35f, 0.38f);
        private static readonly Color EnragedMagmaColor = new Color(0.95f, 0.25f, 0.1f);
        private static readonly Color CrystalGlowColor = new Color(0.2f, 0.85f, 0.95f);

        protected override void Awake()
        {
            if (enemyData == null)
            {
                EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
                data.enemyName = "Ancient Stone Titan";
                data.maxHealth = 2000f; // Standard World Boss HP (2000 HP)
                data.xpReward = 1000;
                data.modelScale = new Vector3(3.5f, 3.5f, 3.5f);
                data.enemyColor = NormalStoneColor;
                enemyData = data;
            }

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            spawnPos = transform.position;

            if (characterController != null)
            {
                characterController.stepOffset = 0.3f; // Prevent stepping on rocks/props
                characterController.slopeLimit = 35f;
            }

            BuildTitanVisualsAndClimbNodes();
        }

        private void BuildTitanVisualsAndClimbNodes()
        {
            if (transform.Find("TitanVisual_Root") != null) return;

            titanModel = Environment.WorldPlaceholderFactory.Build(
                Environment.PlaceholderAssetKey.StoneTitanBossModel,
                transform,
                null,
                1.0f
            );
            titanModel.name = "TitanVisual_Root";

            titanRenderer = titanModel.GetComponentInChildren<Renderer>();
            meshRenderer = titanRenderer;
            originalColor = NormalStoneColor;

            // Setup Nape Crystal Visual reference
            Transform crystalVis = titanModel.transform.Find("NapeCrystal_Visual");
            if (crystalVis != null)
            {
                crystalRenderer = crystalVis.GetComponent<Renderer>();
            }

            // Setup Main Nape Target Node
            GameObject napeNodeObj = new GameObject("TitanClimbNode_Nape");
            napeNodeObj.transform.SetParent(transform, false);
            napeNodeObj.transform.localPosition = new Vector3(0, 12.5f, -2.5f);

            climbNode = napeNodeObj.AddComponent<TitanClimbNode>();
            climbNode.parentTitan = this;
            climbNode.napePosition = napeNodeObj.transform;
            if (crystalVis != null) climbNode.napeCrystalVisual = crystalVis.gameObject;
            climbNode.SetNodeActive(false);

            // Setup Ground-Level Leg Climb Anchor Triggers
            SetupLegClimbAnchor("LeftLegClimbAnchor", new Vector3(-2.2f, 1.5f, 0.5f));
            SetupLegClimbAnchor("RightLegClimbAnchor", new Vector3(2.2f, 1.5f, 0.5f));

            // Attach HitZone components to model colliders
            AttachHitZone(titanModel.transform.Find("TitanLeftLeg"), TitanHitZoneType.LeftLeg);
            AttachHitZone(titanModel.transform.Find("TitanRightLeg"), TitanHitZoneType.RightLeg);
            AttachHitZone(titanModel.transform.Find("TitanTorso"), TitanHitZoneType.Torso);
            AttachHitZone(titanModel.transform.Find("TitanShoulderL"), TitanHitZoneType.LeftArm);
            AttachHitZone(titanModel.transform.Find("TitanShoulderR"), TitanHitZoneType.RightArm);
            AttachHitZone(titanModel.transform.Find("TitanHead"), TitanHitZoneType.Head);
            AttachHitZone(crystalVis, TitanHitZoneType.NapeCrystal);
        }

        private void SetupLegClimbAnchor(string name, Vector3 localPos)
        {
            Transform anchor = titanModel.transform.Find(name);
            if (anchor == null)
            {
                GameObject obj = new GameObject(name);
                obj.transform.SetParent(titanModel.transform, false);
                obj.transform.localPosition = localPos;
                anchor = obj.transform;
            }

            SphereCollider col = anchor.GetComponent<SphereCollider>();
            if (col == null) col = anchor.gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 3.0f;

            // Route trigger interaction to main climb node
            TitanClimbLegAnchor legScript = anchor.GetComponent<TitanClimbLegAnchor>();
            if (legScript == null) legScript = anchor.gameObject.AddComponent<TitanClimbLegAnchor>();
            legScript.mainClimbNode = climbNode;
        }

        private void AttachHitZone(Transform target, TitanHitZoneType zoneType)
        {
            if (target == null) return;

            TitanHitZone zone = target.GetComponent<TitanHitZone>();
            if (zone == null) zone = target.gameObject.AddComponent<TitanHitZone>();
            zone.Initialize(this, zoneType);
            hitZones.Add(zone);
        }

        public void ActivateCombat()
        {
            inCombat = true;
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead) return;

            if (playerTransform != null && !inCombat)
            {
                float d = Vector3.Distance(transform.position, playerTransform.position);
                if (d <= detectionRadius)
                {
                    inCombat = true;
                }
            }

            if (!inCombat) return;

            // Strict Ground Validation & Anti-Stuck Safeguards
            GroundValidationUpdate();

            if (isAttacking || isKneeling || playerTransform == null || playerStats.IsDead || !SafeCanMove()) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > loseTargetRadius)
            {
                inCombat = false;
                return;
            }

            // Enrage check at 30% HP
            if (!isEnraged && CurrentHP / MaxHP <= 0.30f)
            {
                TriggerEnrage();
            }

            // Enforce Arena Tether Bounds (Max 60m from spawnPos)
            float distFromSpawn = Vector3.Distance(transform.position, spawnPos);
            if (distFromSpawn > loseTargetRadius)
            {
                Vector3 returnDir = (spawnPos - transform.position).normalized;
                characterController.Move(returnDir * moveSpeed * Time.deltaTime);
                return;
            }

            attackTimer -= Time.deltaTime;
            dist = Vector3.Distance(transform.position, playerTransform.position);

            FacePlayer();

            // Titan Grab Check: Player stays near feet for > 3.5 seconds
            if (dist <= grabRange)
            {
                closeFootTimer += Time.deltaTime;
                if (closeFootTimer >= 3.5f && attackTimer <= 0f)
                {
                    closeFootTimer = 0f;
                    StartCoroutine(PerformGrabAttack());
                    return;
                }
            }
            else
            {
                closeFootTimer = 0f;
            }

            if (attackTimer <= 0f)
            {
                float roll = Random.value;
                if (dist <= stompRadius && roll < 0.40f)
                {
                    StartCoroutine(PerformStomp());
                }
                else if (roll < 0.75f)
                {
                    StartCoroutine(PerformBoulderThrow());
                }
                else
                {
                    StartCoroutine(PerformEarthquakeRoar());
                }
            }
            else if (dist > stompRadius)
            {
                MoveTowardPlayer();
            }
        }

        /// <summary>
        /// Strict Ground Validation System & Anti-Stuck Height Check.
        /// Prevents Titan from climbing environmental rocks, props, ruins, or floating.
        /// </summary>
        private void GroundValidationUpdate()
        {
            float targetTerrainY = Environment.SceneEnvironmentBuilder.GetTerrainHeightY(transform.position.x, transform.position.z);
            float currentY = transform.position.y;
            float heightDiff = Mathf.Abs(targetTerrainY - currentY);

            // Anti-Stuck Height Threshold (2.5m)
            if (heightDiff > 2.5f)
            {
                // Instantly clamp position down to valid terrain surface
                Vector3 clampedPos = transform.position;
                clampedPos.y = targetTerrainY;
                transform.position = clampedPos;
            }
            else if (heightDiff > 0.05f)
            {
                // Smooth ground anchor alignment
                Vector3 pos = transform.position;
                pos.y = Mathf.Lerp(pos.y, targetTerrainY, Time.deltaTime * 15f);
                transform.position = pos;
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
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 3f);
            }
        }

        private void MoveTowardPlayer()
        {
            Vector3 dir = playerTransform.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                float speed = isEnraged ? moveSpeed * 1.35f : moveSpeed;
                characterController.Move(dir.normalized * speed * Time.deltaTime);
            }
        }

        public void ProcessZoneDamage(DamageInfo info, TitanHitZoneType zoneType)
        {
            if (zoneType == TitanHitZoneType.NapeCrystal)
            {
                if (climbNode != null && climbNode.IsMounted)
                {
                    climbNode.StrikeNapeCrystal(info.amount);
                    return;
                }
                base.TakeDamage(info);
                return;
            }

            float multiplier = 0.05f;
            if (zoneType == TitanHitZoneType.LeftLeg || zoneType == TitanHitZoneType.RightLeg)
            {
                multiplier = 0.05f;
                currentStagger += info.amount * 2.0f;
            }
            else if (zoneType == TitanHitZoneType.LeftArm || zoneType == TitanHitZoneType.RightArm)
            {
                multiplier = 0.05f;
                currentStagger += info.amount * 1.0f;
            }
            else if (zoneType == TitanHitZoneType.Torso || zoneType == TitanHitZoneType.Head)
            {
                multiplier = 0.02f;
            }

            if (!isKneeling && !IsDead && currentStagger >= maxStagger)
            {
                currentStagger = 0f;
                StartCoroutine(EnterExhaustedKneelingState());
            }

            DamageInfo reducedInfo = new DamageInfo(
                info.amount * multiplier,
                info.knockbackDirection,
                info.knockbackForce,
                info.isCritical,
                info.attacker
            );

            base.TakeDamage(reducedInfo);
        }

        protected override void Die()
        {
            if (IsDead) return;

            // 1. Dismount player from climb node immediately before boss shrinking/destruction
            if (climbNode != null && climbNode.IsMounted)
            {
                climbNode.DismountPlayer();
            }

            // 2. Safely rescue player transform, scale, colliders, and input mode
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null && playerTransform != null) player = playerTransform.gameObject;
            if (player != null)
            {
                if (player.transform.parent != null)
                {
                    player.transform.SetParent(null);
                }
                player.transform.localScale = Vector3.one;

                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;

                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.enabled = true;
            }

            // 3. Set InputStateManager and GameStateManager back to Gameplay
            if (Roguelite.Core.InputStateManager.Instance != null)
            {
                Roguelite.Core.InputStateManager.Instance.SetGameplayMode();
            }
            if (Roguelite.Core.StateMachine.GameStateManager.Instance != null)
            {
                Roguelite.Core.StateMachine.GameStateManager.Instance.SetState(Roguelite.Core.StateMachine.GameState.Gameplay);
            }

            // 4. Force restore player camera ownership
            if (Roguelite.Core.CameraManager.Instance != null)
            {
                Roguelite.Core.CameraManager.Instance.ForceRestorePlayerCamera("AncientStoneTitanAI.Die");
            }

            // 5. Clean up arena barrier pillars
            StoneTitanArenaTrigger arena = FindFirstObjectByType<StoneTitanArenaTrigger>();
            if (arena != null) arena.UnlockArena();

            CleanupKneelingRamps();

            base.Die();
        }

        public void TakeNapeDirectDamage(float directDamage)
        {
            DamageInfo info = new DamageInfo(directDamage, Vector3.down, 0f, true, gameObject);
            base.TakeDamage(info);
        }

        public void ShakeOffPlayerAndStandUp()
        {
            StopAllCoroutines();
            StartCoroutine(PerformShakeOffAndStandUpRoutine());
        }

        private IEnumerator PerformShakeOffAndStandUpRoutine()
        {
            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.8f, 0.5f);

            CleanupKneelingRamps();
            if (climbNode != null) climbNode.SetNodeActive(false);

            // Stand back up
            if (titanModel != null)
            {
                titanModel.transform.localPosition = Vector3.zero;
                titanModel.transform.localRotation = Quaternion.identity;
            }

            yield return new WaitForSeconds(0.6f);

            isKneeling = false;
            isAttacking = false;
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            if (titanRenderer != null)
            {
                titanRenderer.material.color = EnragedMagmaColor;
                originalColor = EnragedMagmaColor;
            }

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.8f, 0.5f);
        }

        // ── 1. Titan Stomp Attack (Jumpable Shockwaves) ───────────
        private IEnumerator PerformStomp()
        {
            isAttacking = true;
            attackTimer = isEnraged ? 2.5f : 3.8f;

            // Telegraph ground ring
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "TitanStompTelegraph";
            StripCollider(marker);
            marker.transform.position = transform.position + new Vector3(0, 0.05f, 0);
            marker.transform.localScale = new Vector3(stompRadius * 2f, 0.02f, stompRadius * 2f);
            SetMaterialColor(marker, new Color(0.9f, 0.35f, 0.1f, 0.5f));

            yield return new WaitForSeconds(0.8f);
            Destroy(marker);

            // Foot Slam Shockwave Wave
            int waveCount = isEnraged ? 2 : 1;
            for (int w = 0; w < waveCount; w++)
            {
                if (IsDead) yield break;

                if (playerStats != null && !playerStats.IsDead)
                {
                    float dist = Vector3.Distance(transform.position, playerTransform.position);
                    PlayerController pc = playerTransform.GetComponent<PlayerController>();

                    bool isAirborne = pc != null && !pc.IsGrounded;
                    if (dist <= stompRadius && !isAirborne)
                    {
                        DamageInfo info = new DamageInfo(stompDamage, (playerTransform.position - transform.position).normalized, 14f, false, gameObject);
                        playerStats.TakeDamage(info);
                    }
                }

                ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
                if (cam != null) cam.TriggerShake(0.6f, 0.35f);

                GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wave.name = "TitanStompShockwave";
                StripCollider(wave);
                wave.transform.position = transform.position + new Vector3(0, 0.05f, 0);
                SetMaterialColor(wave, new Color(0.95f, 0.5f, 0.15f, 0.45f));
                StartCoroutine(ExpandAndFade(wave, stompRadius));

                if (w < waveCount - 1) yield return new WaitForSeconds(0.4f);
            }

            isAttacking = false;
        }

        // ── 2. Titan Grab Attack ──────────────────────────────────
        private IEnumerator PerformGrabAttack()
        {
            isAttacking = true;
            attackTimer = 4.5f;

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.5f, 0.3f);

            yield return new WaitForSeconds(0.5f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= grabRange + 1.0f)
                {
                    Vector3 throwDir = (playerTransform.position - transform.position).normalized + Vector3.up * 0.8f;
                    DamageInfo info = new DamageInfo(grabDamage, throwDir, 25f, false, gameObject);
                    playerStats.TakeDamage(info);

                    if (cam != null) cam.TriggerShake(0.75f, 0.4f);
                }
            }

            yield return new WaitForSeconds(0.8f);
            isAttacking = false;
        }

        // ── 3. Boulder Throw Attack (Leaves Rock Cover) ───────────
        private IEnumerator PerformBoulderThrow()
        {
            isAttacking = true;
            attackTimer = isEnraged ? 3.0f : 4.5f;

            Vector3 startPos = transform.position + Vector3.up * 6f + transform.forward * 2f;
            Vector3 targetPos = playerTransform != null ? playerTransform.position : transform.position + transform.forward * 12f;

            GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulder.name = "TitanBoulderProjectile";
            StripCollider(boulder);
            boulder.transform.position = startPos;
            boulder.transform.localScale = Vector3.one * 2.2f;
            SetMaterialColor(boulder, new Color(0.4f, 0.38f, 0.35f));

            float flightTime = 0.85f;
            float elapsed = 0f;
            while (elapsed < flightTime)
            {
                if (boulder == null) yield break;
                elapsed += Time.deltaTime;
                float p = elapsed / flightTime;
                boulder.transform.position = Vector3.Lerp(startPos, targetPos, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * 4f;
                yield return null;
            }

            Vector3 impactPos = boulder.transform.position;
            Destroy(boulder);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(impactPos, playerTransform.position);
                if (dist <= 3.5f)
                {
                    DamageInfo info = new DamageInfo(22f, (playerTransform.position - impactPos).normalized, 12f, false, gameObject);
                    playerStats.TakeDamage(info);
                }
            }

            GameObject coverRock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coverRock.name = "TitanBoulderRockCover";
            coverRock.transform.position = impactPos;
            coverRock.transform.localScale = Vector3.one * 2.5f;
            SetMaterialColor(coverRock, new Color(0.35f, 0.35f, 0.38f));
            Destroy(coverRock, 12.0f);

            ThirdPersonCamera c = FindFirstObjectByType<ThirdPersonCamera>();
            if (c != null) c.TriggerShake(0.5f, 0.3f);

            isAttacking = false;
        }

        // ── 4. Earthquake Roar & Falling Rocks Attack ─────────────
        private IEnumerator PerformEarthquakeRoar()
        {
            isAttacking = true;
            attackTimer = 6.0f;

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.9f, 0.8f);

            // 3 Tremor Waves
            for (int i = 0; i < 3; i++)
            {
                if (IsDead) yield break;

                if (playerStats != null && !playerStats.IsDead)
                {
                    PlayerController pc = playerTransform.GetComponent<PlayerController>();
                    bool isAirborne = pc != null && !pc.IsGrounded;
                    if (!isAirborne && Vector3.Distance(transform.position, playerTransform.position) < 35f)
                    {
                        DamageInfo info = new DamageInfo(12f, Vector3.up, 4f, false, gameObject);
                        playerStats.TakeDamage(info);
                    }
                }
                yield return new WaitForSeconds(0.4f);
            }

            // Drop 5 Falling Rocks from Canyon Cliffs
            for (int r = 0; r < 5; r++)
            {
                Quaternion rot = Quaternion.Euler(0, r * 72f, 0);
                Vector3 dropPos = transform.position + rot * Vector3.forward * Random.Range(8f, 28f);
                dropPos.y = Environment.SceneEnvironmentBuilder.GetTerrainHeightY(dropPos.x, dropPos.z);
                StartCoroutine(DropFallingRock(dropPos));
            }

            yield return new WaitForSeconds(1.2f);

            // Enter Exhausted Kneeling State after Earthquake!
            StartCoroutine(EnterExhaustedKneelingState());
        }

        private IEnumerator DropFallingRock(Vector3 groundPos)
        {
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "FallingRockShadowMarker";
            StripCollider(shadow);
            shadow.transform.position = groundPos + Vector3.up * 0.05f;
            shadow.transform.localScale = new Vector3(3.5f, 0.02f, 3.5f);
            SetMaterialColor(shadow, new Color(0.85f, 0.2f, 0.1f, 0.5f));

            yield return new WaitForSeconds(0.85f);
            Destroy(shadow);

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "FallingCanyonRock";
            StripCollider(rock);
            Vector3 startPos = groundPos + Vector3.up * 18f;
            rock.transform.position = startPos;
            rock.transform.localScale = Vector3.one * 1.8f;
            SetMaterialColor(rock, new Color(0.4f, 0.4f, 0.42f));

            float fallTime = 0.45f;
            float t = 0f;
            while (t < fallTime)
            {
                if (rock == null) yield break;
                t += Time.deltaTime;
                rock.transform.position = Vector3.Lerp(startPos, groundPos, t / fallTime);
                yield return null;
            }

            Destroy(rock);

            if (playerStats != null && !playerStats.IsDead)
            {
                if (Vector3.Distance(groundPos, playerTransform.position) <= 2.5f)
                {
                    DamageInfo info = new DamageInfo(16f, Vector3.down, 5f, false, gameObject);
                    playerStats.TakeDamage(info);
                }
            }

            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obstacle.name = "FallenCanyonRockCover";
            obstacle.transform.position = groundPos;
            obstacle.transform.localScale = Vector3.one * 2.0f;
            SetMaterialColor(obstacle, new Color(0.35f, 0.35f, 0.38f));
            Destroy(obstacle, 15.0f);
        }

        private List<GameObject> activeClimbRamps = new List<GameObject>();

        // ── 5. Exhausted Kneeling Vulnerability Window (15s) ───────
        private IEnumerator EnterExhaustedKneelingState()
        {
            isKneeling = true;
            if (climbNode != null) climbNode.SetNodeActive(true);

            // Lower Titan visual posture low to ground (knees & arms near Y = 1.0m)
            if (titanModel != null)
            {
                titanModel.transform.localPosition = new Vector3(0, -7.5f, 2.5f);
                titanModel.transform.localRotation = Quaternion.Euler(22f, 0, 0);
            }

            // Spawn physical climb ramps around Titan's feet
            SpawnKneelingClimbRamps();

            // Visual Crystal Emission Glow
            if (crystalRenderer != null)
            {
                crystalRenderer.material.color = CrystalGlowColor;
            }

            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.6f, 0.4f);

            float kneelDuration = isEnraged ? 10.0f : 15.0f; // 15.0s vulnerability window
            yield return new WaitForSeconds(kneelDuration);

            // Stand back up if player didn't strike crystal
            CleanupKneelingRamps();
            if (climbNode != null) climbNode.SetNodeActive(false);
            if (titanModel != null)
            {
                titanModel.transform.localPosition = Vector3.zero;
                titanModel.transform.localRotation = Quaternion.identity;
            }

            if (cam != null) cam.TriggerShake(0.7f, 0.4f);

            isKneeling = false;
            isAttacking = false;
        }

        private void SpawnKneelingClimbRamps()
        {
            CleanupKneelingRamps();

            // Left & Right fallen stone armor ramps
            Vector3[] offsets = { new Vector3(-2.8f, 0.4f, 1.5f), new Vector3(2.8f, 0.4f, 1.5f) };
            Vector3[] rotations = { new Vector3(-25f, -15f, 0f), new Vector3(-25f, 15f, 0f) };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = $"ClimbRamp_ArmorSlab_{i}";
                ramp.transform.SetParent(transform, false);
                ramp.transform.localPosition = offsets[i];
                ramp.transform.localRotation = Quaternion.Euler(rotations[i]);
                ramp.transform.localScale = new Vector3(2.2f, 0.6f, 4.2f);
                SetMaterialColor(ramp, new Color(0.40f, 0.40f, 0.44f));
                activeClimbRamps.Add(ramp);
            }
        }

        private void CleanupKneelingRamps()
        {
            foreach (var r in activeClimbRamps)
            {
                if (r != null) Destroy(r);
            }
            activeClimbRamps.Clear();
        }

        private void OnGUI()
        {
            if (isKneeling && !IsDead)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 24;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = new Color(1.0f, 0.85f, 0.2f);
                style.alignment = TextAnchor.MiddleCenter;

                GUI.Label(new Rect(Screen.width / 2f - 350, 110, 700, 45), "THE TITAN IS VULNERABLE! CLIMB ITS LEGS TO STRIKE THE CRYSTAL CORE!", style);
            }
        }

        // ── Loot Drop Override ────────────────────────────────────
        protected override void SpawnEnemyLoot()
        {
            StoneTitanArenaTrigger arena = FindFirstObjectByType<StoneTitanArenaTrigger>();
            if (arena != null) arena.UnlockArena();

            LootResult loot = LootTable.ForAncientStoneTitan();
            LootDrop.SpawnFromResult(loot, transform.position + Vector3.up * 1.5f);
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

        private IEnumerator ExpandAndFade(GameObject ring, float targetRadius)
        {
            float duration = 0.4f;
            float t = 0f;
            Vector3 startScale = new Vector3(0.5f, 0.02f, 0.5f);
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
    }

    /// <summary>
    /// Auxiliary leg anchor trigger script attached to Titan's left/right feet.
    /// Routes ground-level player climb interactions to main TitanClimbNode.
    /// </summary>
    public class TitanClimbLegAnchor : MonoBehaviour
    {
        public TitanClimbNode mainClimbNode;

        private void OnTriggerStay(Collider other)
        {
            if (mainClimbNode != null)
            {
                PlayerController p = other.GetComponent<PlayerController>();
                if (p == null && other.CompareTag("Player")) p = other.GetComponentInParent<PlayerController>();

                if (p != null && (Input.GetKeyDown(KeyCode.E) || !p.IsGrounded))
                {
                    mainClimbNode.MountPlayer(p);
                }
            }
        }
    }
}
