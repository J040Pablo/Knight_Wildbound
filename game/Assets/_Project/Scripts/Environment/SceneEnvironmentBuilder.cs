using UnityEngine;
using Roguelite.Core;
using Roguelite.Player;
using Roguelite.Wave;
using Roguelite.Enemy;

namespace Roguelite.Environment
{
    public class SceneEnvironmentBuilder : MonoBehaviour
    {
        private void Awake()
        {
            BuildContinuousRunWorld();
        }

        /// <summary>
        /// Multi-harmonic Perlin path offset calculator for organic, natural winding forest trails.
        /// </summary>
        public static float GetForestPathXOffset(float z)
        {
            if (z < 80f) return 0f;
            float zRel = z - 80f;
            float wave1 = Mathf.Sin(zRel * 0.03f) * 12f;
            float wave2 = Mathf.Sin(zRel * 0.011f) * 8f;
            float perlin = (Mathf.PerlinNoise(zRel * 0.02f, 0.5f) - 0.5f) * 10f;
            return wave1 + wave2 + perlin;
        }

        public void BuildContinuousRunWorld()
        {
            SetupGlobalSunLight();

            // 1. RUINS REGION (Z: -20 to 35)
            BuildRuinsRegion();

            // 2. HORSE AREA REGION (Z: 35 to 80)
            BuildHorseAreaRegion();

            // 3. FOREST BIOME REGION (Z: 80 to 330)
            BuildForestBiomeRegion();

            // 4. THEMATIC DARK TRANSITION CORRIDOR (Z: 330 to 353)
            BuildDarkTransitionCorridor();

            // 5. HOLLOW TREE BOSS ARENA REGION (Z: 353 to 430)
            BuildBossArenaRegion();

            // 6. CONTINUOUS WORLD BOUNDARY (Expanded X size 180m for wide open 80m forest clearings)
            CreateWorldBoundary(new Vector3(0, 5f, 200f), new Vector3(180f, 30f, 500f));

            // Apply starting region settings (Ruins)
            var startingRegion = GameObject.Find("RegionTrigger_Ruins")?.GetComponent<BiomeRegionTrigger>();
            if (startingRegion != null)
            {
                startingRegion.ApplyRegionSettings();
            }
        }

        private void SetupGlobalSunLight()
        {
            GameObject sun = GameObject.Find("Directional Sun Light");
            if (sun == null)
            {
                sun = new GameObject("Directional Sun Light");
                Light l = sun.AddComponent<Light>();
                l.type = LightType.Directional;
            }

            Light lightComp = sun.GetComponent<Light>();
            if (lightComp != null)
            {
                lightComp.color = new Color(1.0f, 0.9f, 0.75f);
                lightComp.intensity = 1.2f;
            }
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        // ==========================================
        // 1. RUINS REGION (Z: -20 to 35)
        // ==========================================
        private void BuildRuinsRegion()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "RuinsGround";
            ground.tag = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 10f);
            ground.transform.localScale = new Vector3(35f, 0.5f, 45f);

            Collider primCol = ground.GetComponent<Collider>();
            if (primCol != null) DestroyImmediate(primCol);
            ground.AddComponent<BoxCollider>();

            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.42f, 0.40f, 0.38f); // Ancient stone grey

            // Ruined Pillars
            for (int i = 0; i < 8; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * 9f;
                float z = (i / 2) * 10f - 5f;

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"RuinedPillar_{i}";
                pillar.transform.position = new Vector3(x, 2.5f, z);
                pillar.transform.localScale = new Vector3(1.2f, 2.5f, 1.2f);
                Renderer pR = pillar.GetComponent<Renderer>();
                if (pR != null) pR.material.color = new Color(0.50f, 0.48f, 0.45f);
            }

            // Central Campfire
            GameObject campfire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            campfire.name = "RuinsCampfire";
            campfire.transform.position = new Vector3(0, 0.1f, 0);
            campfire.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
            Collider cfCol = campfire.GetComponent<Collider>();
            if (cfCol != null) DestroyImmediate(cfCol);
            Renderer cR = campfire.GetComponent<Renderer>();
            if (cR != null) cR.material.color = new Color(0.3f, 0.2f, 0.1f);

            // King NPC
            GameObject kingObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            kingObj.name = "KingNPC";
            kingObj.transform.position = new Vector3(0, 1.0f, 5f);
            Renderer kR = kingObj.GetComponent<Renderer>();
            if (kR != null) kR.material.color = new Color(0.9f, 0.75f, 0.1f); // Royal Gold

            // Crown Visual
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "KingCrown_Visual";
            crown.transform.parent = kingObj.transform;
            crown.transform.localPosition = new Vector3(0, 1.1f, 0);
            crown.transform.localScale = new Vector3(0.6f, 0.2f, 0.6f);
            Collider crCol = crown.GetComponent<Collider>();
            if (crCol != null) DestroyImmediate(crCol);
            Renderer crR = crown.GetComponent<Renderer>();
            if (crR != null) crR.material.color = new Color(1.0f, 0.85f, 0.0f);

            kingObj.AddComponent<KingNPC>();

            // 3 Weapon Selection Pedestals
            CreateWeaponPedestal(new Vector3(-4f, 0f, 10f), CharacterType.Knight);
            CreateWeaponPedestal(new Vector3(0f, 0f, 11f), CharacterType.Mage);
            CreateWeaponPedestal(new Vector3(4f, 0f, 10f), CharacterType.Druid);

            // Player Spawn Point node inside Ruins
            CreatePlayerSpawnPoint("RuinsPlayerSpawn", new Vector3(0, 0.5f, 8.0f), Quaternion.identity);

            // Ruins Exit Gate to Horse Area & Forest
            GameObject exitGate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitGate.name = "RuinsExitGate";
            exitGate.transform.position = new Vector3(0, 2.0f, 32f);
            exitGate.transform.localScale = new Vector3(8.0f, 4.0f, 0.8f);
            Renderer exR = exitGate.GetComponent<Renderer>();
            if (exR != null) exR.material.color = new Color(0.5f, 0.5f, 0.5f, 0.9f);
            exitGate.AddComponent<RuinsExitGate>();

            // Ruins Region Trigger
            CreateRegionTrigger("RegionTrigger_Ruins", "Ruins (Tutorial Area)", new Vector3(0, 5f, 5f), new Vector3(40f, 20f, 55f),
                new Color(1.0f, 0.9f, 0.75f), 1.2f, new Color(0.4f, 0.4f, 0.5f), 0.012f, new Color(0.4f, 0.4f, 0.5f));
        }

        private void CreateWeaponPedestal(Vector3 pos, CharacterType classType)
        {
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = $"Pedestal_{classType}";
            pedestal.transform.position = pos + new Vector3(0, 0.4f, 0);
            pedestal.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
            Renderer pR = pedestal.GetComponent<Renderer>();
            if (pR != null) pR.material.color = new Color(0.35f, 0.32f, 0.30f);

            GameObject weaponObj = GameObject.CreatePrimitive(classType == CharacterType.Knight ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            weaponObj.name = $"WeaponPickup_{classType}";
            weaponObj.transform.position = pos + new Vector3(0, 1.2f, 0);

            if (classType == CharacterType.Knight) weaponObj.transform.localScale = new Vector3(0.15f, 1.2f, 0.2f);
            else if (classType == CharacterType.Mage) weaponObj.transform.localScale = new Vector3(0.1f, 1.3f, 0.1f);
            else weaponObj.transform.localScale = new Vector3(0.12f, 1.2f, 0.12f);

            Renderer wR = weaponObj.GetComponent<Renderer>();
            if (wR != null)
            {
                if (classType == CharacterType.Knight) wR.material.color = new Color(0.8f, 0.85f, 0.95f);
                else if (classType == CharacterType.Mage) wR.material.color = new Color(0.85f, 0.7f, 0.2f);
                else wR.material.color = new Color(0.45f, 0.3f, 0.15f);
            }

            var wInt = weaponObj.AddComponent<WeaponInteractable>();
            var field = typeof(WeaponInteractable).GetField("targetClass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(wInt, classType);
        }

        // ==========================================
        // 2. HORSE AREA REGION (Z: 35 to 80)
        // ==========================================
        private void BuildHorseAreaRegion()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "HorseAreaGround";
            ground.tag = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 57.5f);
            ground.transform.localScale = new Vector3(26f, 0.5f, 45f);
            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.28f, 0.48f, 0.22f); // Lush Meadow green

            CreateHorseSpawnPoint("HorseMeadowSpawn", new Vector3(-6f, 0f, 55f), Quaternion.identity);
            CreateFriendlyHorse(new Vector3(-6f, 0f, 55f));

            CreateRegionTrigger("RegionTrigger_HorseMeadow", "Horse Valley", new Vector3(0, 5f, 57.5f), new Vector3(30f, 20f, 45f),
                new Color(1.0f, 0.92f, 0.7f), 1.2f, new Color(0.45f, 0.45f, 0.4f), 0.010f, new Color(0.5f, 0.5f, 0.4f));
        }

        private void CreateFriendlyHorse(Vector3 pos)
        {
            GameObject horseObj = new GameObject("FriendlyHorse");
            horseObj.transform.position = pos;

            CharacterController cc = horseObj.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.8f;
            cc.center = new Vector3(0, 1.0f, 0);

            horseObj.AddComponent<HorseController>();
            horseObj.AddComponent<MountSystem>();
        }

        // ==========================================
        // 3. FOREST BIOME REGION (Z: 80 to 330)
        // ==========================================
        private void BuildForestBiomeRegion()
        {
            float forestStart = 80f;
            float forestEnd = 330f;
            float stepZ = 10f;

            for (float z = forestStart; z <= forestEnd; z += stepZ)
            {
                float xOffset = GetForestPathXOffset(z);
                float xNext = GetForestPathXOffset(z + stepZ);
                float xMid = (xOffset + xNext) / 2f;
                float yElev = Mathf.Sin(z * 0.04f) * 0.35f;

                // 1. Central Warm Dirt Trail Slab (18-25m Wide Main Road)
                GameObject pathSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pathSlab.name = $"ForestPathSlab_{z}";
                pathSlab.tag = "Ground";
                pathSlab.transform.position = new Vector3(xMid, yElev - 0.5f, z + stepZ / 2f);
                pathSlab.transform.localScale = new Vector3(22f, 0.5f, stepZ + 0.2f);
                Renderer pR = pathSlab.GetComponent<Renderer>();

                bool isDarkStage = z >= 240f;
                if (pR != null) pR.material.color = isDarkStage ? new Color(0.22f, 0.16f, 0.16f) : new Color(0.38f, 0.28f, 0.18f);

                // 2. Wide Left Ground Clearing Slab (30m wide)
                GameObject clearingLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                clearingLeft.name = $"ForestClearingLeft_{z}";
                clearingLeft.tag = "Ground";
                clearingLeft.transform.position = new Vector3(xMid - 26f, yElev - 0.5f, z + stepZ / 2f);
                clearingLeft.transform.localScale = new Vector3(30f, 0.5f, stepZ + 0.2f);
                Renderer cLR = clearingLeft.GetComponent<Renderer>();
                if (cLR != null) cLR.material.color = isDarkStage ? new Color(0.18f, 0.12f, 0.15f) : new Color(0.32f, 0.25f, 0.15f);

                // 3. Wide Right Ground Clearing Slab (30m wide)
                GameObject clearingRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                clearingRight.name = $"ForestClearingRight_{z}";
                clearingRight.tag = "Ground";
                clearingRight.transform.position = new Vector3(xMid + 26f, yElev - 0.5f, z + stepZ / 2f);
                clearingRight.transform.localScale = new Vector3(30f, 0.5f, stepZ + 0.2f);
                Renderer cRR = clearingRight.GetComponent<Renderer>();
                if (cRR != null) cRR.material.color = isDarkStage ? new Color(0.18f, 0.12f, 0.15f) : new Color(0.32f, 0.25f, 0.15f);

                // 4. Organic Props & Foliage across 80m Clearing (leaving main 20m trail open)
                if (!isDarkStage)
                {
                    // Autumn Trees along outer margins
                    CreateAutumnTree(new Vector3(xMid - 16f, yElev, z), Random.Range(-12f, 12f));
                    CreateAutumnTree(new Vector3(xMid + 16f, yElev, z + 4f), Random.Range(-12f, 12f));
                    CreateAutumnTree(new Vector3(xMid - 28f, yElev, z + 6f), Random.Range(-15f, 15f));
                    CreateAutumnTree(new Vector3(xMid + 28f, yElev, z + 2f), Random.Range(-15f, 15f));

                    // Decorative props
                    if ((int)z % 20 == 0)
                    {
                        CreatePumpkinProp(new Vector3(xMid - 11f, yElev + 0.4f, z + 3f));
                        CreateLanternProp(new Vector3(xMid + 11f, yElev, z + 8f));
                        CreateFallenLogProp(new Vector3(xMid - 14f, yElev + 0.2f, z + 5f), Random.Range(30f, 70f));
                    }
                    if ((int)z % 30 == 0)
                    {
                        CreateMushroomsProp(new Vector3(xMid + 12f, yElev + 0.1f, z + 4f));
                        CreateBrokenFenceProp(new Vector3(xMid - 12f, yElev, z + 6f));
                        CreateMossyBoulderProp(new Vector3(xMid + 15f, yElev + 0.4f, z + 2f), Random.Range(1.2f, 2.2f));
                        CreateBushProp(new Vector3(xMid - 13f, yElev + 0.3f, z + 8f), 1.5f);
                    }
                }
                else
                {
                    // Corrupted Dead Trees & Gnarled Roots
                    CreateDeadCorruptedTree(new Vector3(xMid - 16f, yElev, z));
                    CreateDeadCorruptedTree(new Vector3(xMid + 16f, yElev, z + 5f));
                    CreateDeadCorruptedTree(new Vector3(xMid - 30f, yElev, z + 3f));
                    CreateDeadCorruptedTree(new Vector3(xMid + 30f, yElev, z + 7f));

                    if ((int)z % 15 == 0)
                    {
                        CreateCrossingRootProp(new Vector3(xMid, yElev + 0.2f, z + 4f), Random.Range(70f, 110f));
                    }
                }
            }

            // Safe Stop Horses along path
            CreateFriendlyHorse(new Vector3(GetForestPathXOffset(165f) - 8f, 0, 165f));
            CreateFriendlyHorse(new Vector3(GetForestPathXOffset(240f) + 8f, 0, 240f));

            // DIRECT ROUTE COMBAT ENCOUNTERS (ON THE MAIN PATH, ZERO GATES, ZERO DETOURS)
            // 1. Easy Section (Z = 120)
            CreateDirectRouteEncounter(120f, EncounterDifficulty.Easy);

            // 2. Medium Section (Z = 200)
            CreateDirectRouteEncounter(200f, EncounterDifficulty.Medium);

            // 3. Hard Section (Z = 280)
            CreateDirectRouteEncounter(280f, EncounterDifficulty.Hard);

            // Stage 2 Biome Region Trigger: Autumn Forest (Z: 80 to 240)
            CreateRegionTrigger("RegionTrigger_AutumnForest", "Autumn Forest", new Vector3(0, 5f, 160f), new Vector3(120f, 25f, 160f),
                new Color(1.0f, 0.85f, 0.65f), 1.15f, new Color(0.45f, 0.35f, 0.3f), 0.018f, new Color(0.45f, 0.35f, 0.3f));

            // Stage 3 Biome Region Trigger: Dark Corrupted Forest (Z: 240 to 330)
            CreateRegionTrigger("RegionTrigger_DarkCorruptedForest", "Dark Corrupted Forest", new Vector3(0, 5f, 285f), new Vector3(120f, 25f, 90f),
                new Color(0.65f, 0.35f, 0.45f), 0.8f, new Color(0.25f, 0.12f, 0.20f), 0.032f, new Color(0.30f, 0.12f, 0.22f));
        }

        private void CreateDirectRouteEncounter(float centerZ, EncounterDifficulty diff)
        {
            float centerX = GetForestPathXOffset(centerZ);
            Vector3 centerPos = new Vector3(centerX, 0, centerZ);

            GameObject zoneObj = new GameObject($"DirectEncounterZone_{diff}");
            zoneObj.transform.position = centerPos;

            BoxCollider box = zoneObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(55f, 8f, 35f); // Spans open wide 80m route

            var encZone = zoneObj.AddComponent<EncounterZone>();

            var diffField = typeof(EncounterZone).GetField("difficulty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (diffField != null) diffField.SetValue(encZone, diff);

            // NO forwardGate passed -> main path remains 100% open and unblocked!
        }

        // ==========================================
        // 4. THEMATIC DARK TRANSITION CORRIDOR (Z: 330 to 353)
        // ==========================================
        private void BuildDarkTransitionCorridor()
        {
            float startZ = 330f;
            float endZ = 353f;
            float length = endZ - startZ;
            float startX = GetForestPathXOffset(startZ);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "DarkTransitionGround";
            ground.tag = "Ground";
            ground.transform.position = new Vector3(startX / 2f, -0.5f, startZ + length / 2f);
            ground.transform.localScale = new Vector3(45f, 0.5f, length + 2f);
            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.18f, 0.12f, 0.14f);

            for (float z = startZ + 4f; z < endZ; z += 6f)
            {
                CreateCrossingRootProp(new Vector3(Random.Range(-3f, 3f), 0.2f, z), Random.Range(70f, 110f));
            }

            CreateDeadCorruptedTree(new Vector3(startX - 14f, 0, startZ + 6f));
            CreateDeadCorruptedTree(new Vector3(startX + 14f, 0, startZ + 14f));

            CreateRegionTrigger("RegionTrigger_DarkCorridor", "Sombreness Pass", new Vector3(0, 5f, startZ + length / 2f), new Vector3(60f, 20f, length),
                new Color(0.6f, 0.3f, 0.3f), 0.75f, new Color(0.2f, 0.1f, 0.15f), 0.038f, new Color(0.25f, 0.1f, 0.15f));
        }

        private void CreateDeadCorruptedTree(Vector3 pos)
        {
            GameObject tree = new GameObject("DeadCorruptedTree");
            tree.transform.position = pos;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0, 2.0f, 0);
            trunk.transform.localScale = new Vector3(0.8f, 2.0f, 0.8f);
            trunk.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0, 360), Random.Range(-12f, 12f));
            Collider tCol = trunk.GetComponent<Collider>();
            if (tCol != null) DestroyImmediate(tCol);
            Renderer tR = trunk.GetComponent<Renderer>();
            if (tR != null) tR.material.color = new Color(0.15f, 0.10f, 0.08f);
        }

        private void CreateAutumnTree(Vector3 pos, float slantAngle)
        {
            GameObject tree = new GameObject("AutumnTree");
            tree.transform.position = pos;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
            trunk.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
            trunk.transform.localRotation = Quaternion.Euler(slantAngle, Random.Range(0, 360), slantAngle * 0.5f);
            Collider tCol = trunk.GetComponent<Collider>();
            if (tCol != null) DestroyImmediate(tCol);
            Renderer tR = trunk.GetComponent<Renderer>();
            if (tR != null) tR.material.color = new Color(0.35f, 0.22f, 0.12f);

            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foliage.name = "Foliage";
            foliage.transform.parent = tree.transform;
            foliage.transform.localPosition = new Vector3(0, 3.8f, 0);
            foliage.transform.localScale = new Vector3(2.8f, 1.2f, 2.8f);
            Collider fCol = foliage.GetComponent<Collider>();
            if (fCol != null) DestroyImmediate(fCol);
            Renderer fR = foliage.GetComponent<Renderer>();
            if (fR != null) fR.material.color = new Color(0.85f, 0.42f, 0.10f);
        }

        private void CreatePumpkinProp(Vector3 pos)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "AutumnPumpkinProp";
            p.transform.position = pos;
            p.transform.localScale = new Vector3(0.9f, 0.75f, 0.9f);
            Collider col = p.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            Renderer r = p.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.92f, 0.45f, 0.08f);
        }

        private void CreateMushroomsProp(Vector3 pos)
        {
            GameObject shroom = new GameObject("MushroomProp");
            shroom.transform.position = pos;

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.transform.parent = shroom.transform;
            cap.transform.localPosition = new Vector3(0, 0.3f, 0);
            cap.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
            Collider col = cap.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            Renderer r = cap.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.85f, 0.2f, 0.15f); // Red cap with white spot theme
        }

        private void CreateLanternProp(Vector3 pos)
        {
            GameObject lantern = new GameObject("GlowingLanternProp");
            lantern.transform.position = pos;

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.parent = lantern.transform;
            post.transform.localPosition = new Vector3(0, 1.2f, 0);
            post.transform.localScale = new Vector3(0.15f, 1.2f, 0.15f);
            Collider pCol = post.GetComponent<Collider>();
            if (pCol != null) DestroyImmediate(pCol);
            Renderer pR = post.GetComponent<Renderer>();
            if (pR != null) pR.material.color = new Color(0.2f, 0.18f, 0.15f);

            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lamp.transform.parent = lantern.transform;
            lamp.transform.localPosition = new Vector3(0, 2.2f, 0);
            lamp.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            Collider lCol = lamp.GetComponent<Collider>();
            if (lCol != null) DestroyImmediate(lCol);
            Renderer lR = lamp.GetComponent<Renderer>();
            if (lR != null) lR.material.color = new Color(1.0f, 0.85f, 0.2f); // Warm yellow glowing lantern
        }

        private void CreateBrokenFenceProp(Vector3 pos)
        {
            GameObject fence = new GameObject("BrokenFenceProp");
            fence.transform.position = pos;

            for (int i = 0; i < 3; i++)
            {
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.transform.parent = fence.transform;
                plank.transform.localPosition = new Vector3(i * 0.8f, 0.4f, 0);
                plank.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
                plank.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
                Collider pCol = plank.GetComponent<Collider>();
                if (pCol != null) DestroyImmediate(pCol);
                Renderer pR = plank.GetComponent<Renderer>();
            }
        }

        private void CreateFallenLogProp(Vector3 pos, float rotationY)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "FallenLogProp";
            log.transform.position = pos;
            log.transform.localScale = new Vector3(0.7f, 3.5f, 0.7f);
            log.transform.rotation = Quaternion.Euler(0, rotationY, 90f);
            Collider col = log.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            Renderer r = log.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.28f, 0.18f, 0.10f);
        }

        private void CreateMossyBoulderProp(Vector3 pos, float scale)
        {
            GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulder.name = "MossyBoulderProp";
            boulder.transform.position = pos;
            boulder.transform.localScale = new Vector3(scale * 1.2f, scale * 0.8f, scale);
            boulder.transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), 0);
            Collider col = boulder.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            Renderer r = boulder.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.38f, 0.42f, 0.35f); // Mossy stone gray
        }

        private void CreateBushProp(Vector3 pos, float scale)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "ForestBushProp";
            bush.transform.position = pos;
            bush.transform.localScale = new Vector3(scale * 1.3f, scale * 0.7f, scale * 1.3f);
            Collider col = bush.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            Renderer r = bush.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.18f, 0.35f, 0.15f); // Deep forest green
        }

        private void CreateCrossingRootProp(Vector3 pos, float angleY)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "CorruptedCrossingRoot";
            root.transform.position = pos;
            root.transform.localScale = new Vector3(0.6f, 10f, 0.6f);
            root.transform.rotation = Quaternion.Euler(0, angleY, 86f);
            Collider rCol = root.GetComponent<Collider>();
            if (rCol != null) DestroyImmediate(rCol);
            Renderer rR = root.GetComponent<Renderer>();
            if (rR != null) rR.material.color = new Color(0.22f, 0.12f, 0.14f);
        }

        // ==========================================
        // 5. HOLLOW TREE BOSS ARENA REGION (Z: 353 to 430)
        // ==========================================
        private void BuildBossArenaRegion()
        {
            Vector3 arenaCenter = new Vector3(0, 0, 385f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "BossArenaGround";
            ground.tag = "Ground";
            ground.transform.position = arenaCenter + new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(50f, 0.5f, 50f);

            Collider primCol = ground.GetComponent<Collider>();
            if (primCol != null) DestroyImmediate(primCol);
            ground.AddComponent<BoxCollider>();

            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.25f, 0.18f, 0.15f); // Dark corrupted dirt

            // Surrounding Ring of Corrupted Trees
            int treeRingCount = 18;
            for (int i = 0; i < treeRingCount; i++)
            {
                float angle = (i / (float)treeRingCount) * Mathf.PI * 2f;
                if (Mathf.Abs(angle - Mathf.PI) < 0.35f) continue; // Entrance open facing z = 353

                Vector3 pos = arenaCenter + new Vector3(Mathf.Cos(angle) * 24f, 0, Mathf.Sin(angle) * 24f);

                GameObject giantTree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                giantTree.name = $"BossBoundaryTree_{i}";
                giantTree.transform.position = pos + new Vector3(0, 4f, 0);
                giantTree.transform.localScale = new Vector3(2.5f, 4f, 2.5f);
                Renderer r = giantTree.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.2f, 0.1f, 0.12f);
            }

            BuildDistantHollowTreeLandmark(arenaCenter + new Vector3(0, 0, 12f));

            // Spawn Hollow Tree Boss at Center of Arena
            GameObject bossObj = new GameObject("TheHollowTreeBoss");
            bossObj.transform.position = arenaCenter + new Vector3(0, 0, 12f);

            CharacterController cc = bossObj.AddComponent<CharacterController>();
            cc.height = 4.0f;
            cc.radius = 1.6f;
            cc.center = new Vector3(0, 2.0f, 0);

            HollowTreeBossAI bossAI = bossObj.AddComponent<HollowTreeBossAI>();
            bossAI.enabled = true;

            // Boss Activation Trigger Volume at entrance of arena (Z = 354)
            GameObject bossTriggerObj = new GameObject("BossActivationTriggerVolume");
            bossTriggerObj.transform.position = new Vector3(0, 2f, 354f);
            BoxCollider bossBox = bossTriggerObj.AddComponent<BoxCollider>();
            bossBox.isTrigger = true;
            bossBox.size = new Vector3(26f, 8f, 10f);
            bossTriggerObj.AddComponent<BossActivationTrigger>();

            // Boss Arena Region Trigger
            CreateRegionTrigger("RegionTrigger_HollowGlade", "The Hollow Glade", arenaCenter + new Vector3(0, 5f, 0), new Vector3(55f, 20f, 65f),
                new Color(0.85f, 0.35f, 0.35f), 0.9f, new Color(0.35f, 0.15f, 0.25f), 0.035f, new Color(0.35f, 0.15f, 0.25f));
        }

        private void BuildDistantHollowTreeLandmark(Vector3 pos)
        {
            GameObject landmarkTrunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            landmarkTrunk.name = "HollowTreeLandmark_Trunk";
            landmarkTrunk.transform.position = pos + new Vector3(0, 15f, 0);
            landmarkTrunk.transform.localScale = new Vector3(5.5f, 15f, 5.5f);
            Collider tCol = landmarkTrunk.GetComponent<Collider>();
            if (tCol != null) DestroyImmediate(tCol);
            Renderer tR = landmarkTrunk.GetComponent<Renderer>();
            if (tR != null) tR.material.color = new Color(0.28f, 0.16f, 0.10f);

            GameObject landmarkCanopy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            landmarkCanopy.name = "HollowTreeLandmark_Canopy";
            landmarkCanopy.transform.position = pos + new Vector3(0, 32f, 0);
            landmarkCanopy.transform.localScale = new Vector3(18f, 4f, 18f);
            Collider cCol = landmarkCanopy.GetComponent<Collider>();
            if (cCol != null) DestroyImmediate(cCol);
            Renderer cR = landmarkCanopy.GetComponent<Renderer>();
            if (cR != null) cR.material.color = new Color(0.95f, 0.40f, 0.05f);
        }

        // ==========================================
        // UTILITY HELPERS
        // ==========================================
        private void CreateRegionTrigger(string name, string displayName, Vector3 center, Vector3 size,
            Color sunColor, float sunIntensity, Color ambientColor, float fogDensity, Color fogColor)
        {
            GameObject trgObj = new GameObject(name);
            trgObj.transform.position = center;
            BoxCollider box = trgObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;

            var trg = trgObj.AddComponent<BiomeRegionTrigger>();
            trg.SetupRegion(displayName, sunColor, sunIntensity, ambientColor, fogDensity, fogColor);
        }

        private void CreatePlayerSpawnPoint(string label, Vector3 pos, Quaternion rot)
        {
            GameObject spObj = new GameObject(label);
            spObj.transform.position = pos;
            spObj.transform.rotation = rot;

            PlayerSpawnPoint spNode = spObj.AddComponent<PlayerSpawnPoint>();
            var field = typeof(PlayerSpawnPoint).GetField("spawnPointLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(spNode, label);
        }

        private void CreateHorseSpawnPoint(string label, Vector3 pos, Quaternion rot)
        {
            GameObject hpObj = new GameObject(label);
            hpObj.transform.position = pos;
            hpObj.transform.rotation = rot;

            HorseSpawnPoint hpNode = hpObj.AddComponent<HorseSpawnPoint>();
            var field = typeof(HorseSpawnPoint).GetField("spawnPointLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(hpNode, label);
        }

        private void CreateWorldBoundary(Vector3 center, Vector3 size)
        {
            GameObject wbObj = new GameObject("WorldBoundary");
            wbObj.transform.position = center;

            WorldBoundary wb = wbObj.AddComponent<WorldBoundary>();
            wb.SetupBoundary(center, size);
        }
    }
}
