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

        public void BuildContinuousRunWorld()
        {
            // Set up overall directional sun light
            SetupGlobalSunLight();

            // 1. RUINS REGION (Z: -20 to 35)
            BuildRuinsRegion();

            // 2. HORSE AREA REGION (Z: 35 to 80)
            BuildHorseAreaRegion();

            // 3. FOREST BIOME REGION (Z: 80 to 330)
            BuildForestBiomeRegion();

            // 4. HOLLOW TREE BOSS ARENA REGION (Z: 330 to 420)
            BuildBossArenaRegion();

            // 5. ENCOMPASSING CONTINUOUS WORLD BOUNDARY
            CreateWorldBoundary(new Vector3(0, 5f, 200f), new Vector3(60f, 30f, 460f));

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
            // Ground Cobblestone
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "RuinsGround";
            ground.tag = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 10f);
            ground.transform.localScale = new Vector3(35f, 0.5f, 45f);

            // FIX: Destroy height-inflated primitive CapsuleCollider and attach BoxCollider for accurate flat ground physics
            Collider primCol = ground.GetComponent<Collider>();
            if (primCol != null) DestroyImmediate(primCol);
            ground.AddComponent<BoxCollider>();

            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.42f, 0.40f, 0.38f); // Ancient stone grey

            // Ruined Pillars & Archways
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
            Destroy(crown.GetComponent<Collider>());
            Renderer crR = crown.GetComponent<Renderer>();
            if (crR != null) crR.material.color = new Color(1.0f, 0.85f, 0.0f);

            kingObj.AddComponent<KingNPC>();

            // 3 Weapon Selection Pedestals
            CreateWeaponPedestal(new Vector3(-4f, 0f, 10f), CharacterType.Knight);
            CreateWeaponPedestal(new Vector3(0f, 0f, 11f), CharacterType.Mage);
            CreateWeaponPedestal(new Vector3(4f, 0f, 10f), CharacterType.Druid);

            // Player Spawn Point node inside Ruins (Center of Platform)
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

            // Weapon Pickup Object
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
            // Meadow Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "HorseAreaGround";
            ground.tag = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 57.5f);
            ground.transform.localScale = new Vector3(26f, 0.5f, 45f);
            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.28f, 0.48f, 0.22f); // Lush Meadow green

            // Horse Spawn Point node
            CreateHorseSpawnPoint("HorseMeadowSpawn", new Vector3(-6f, 0f, 55f), Quaternion.identity);

            // Friendly Horse resting area
            CreateFriendlyHorse(new Vector3(-6f, 0f, 55f));

            // Horse Meadow Region Trigger
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
            float forestLength = 250f;

            // Dirt Path Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "ForestRoadGround";
            ground.tag = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, forestStart + forestLength / 2f);
            ground.transform.localScale = new Vector3(20f, 0.5f, forestLength);
            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.38f, 0.28f, 0.18f); // Autumn dirt path

            // Crooked Autumn Trees & Pumpkins along the road
            for (float z = forestStart; z < forestStart + forestLength; z += 12f)
            {
                CreateAutumnTree(new Vector3(-8.5f, 0, z));
                CreateAutumnTree(new Vector3(8.5f, 0, z + 6f));

                if ((int)z % 24 == 0)
                {
                    CreatePumpkinProp(new Vector3(-7.0f, 0.4f, z + 4f));
                    CreatePumpkinProp(new Vector3(7.0f, 0.4f, z + 10f));
                }
            }

            // Encounters along the route
            // 1. Easy Encounter (Z = 130)
            CreateEncounterZone(new Vector3(0, 0, 130), EncounterDifficulty.Easy, new Vector3(0, 1.5f, 150));

            // Safe Stop Horse (Z = 165)
            CreateFriendlyHorse(new Vector3(-5f, 0, 165));

            // 2. Medium Encounter (Z = 200)
            CreateEncounterZone(new Vector3(0, 0, 200), EncounterDifficulty.Medium, new Vector3(0, 1.5f, 220));

            // Safe Stop Horse (Z = 240)
            CreateFriendlyHorse(new Vector3(5f, 0, 240));

            // 3. Hard Encounter (Z = 275)
            CreateEncounterZone(new Vector3(0, 0, 275), EncounterDifficulty.Hard, new Vector3(0, 1.5f, 295));

            // Forest Region Trigger
            CreateRegionTrigger("RegionTrigger_AutumnForest", "Autumn Forest", new Vector3(0, 5f, forestStart + forestLength / 2f), new Vector3(30f, 20f, forestLength),
                new Color(1.0f, 0.85f, 0.65f), 1.15f, new Color(0.45f, 0.35f, 0.3f), 0.020f, new Color(0.45f, 0.35f, 0.3f));
        }

        private void CreateAutumnTree(Vector3 pos)
        {
            GameObject tree = new GameObject("AutumnTree");
            tree.transform.position = pos;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
            trunk.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
            Renderer tR = trunk.GetComponent<Renderer>();
            if (tR != null) tR.material.color = new Color(0.35f, 0.22f, 0.12f);

            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foliage.name = "Foliage";
            foliage.transform.parent = tree.transform;
            foliage.transform.localPosition = new Vector3(0, 3.8f, 0);
            foliage.transform.localScale = new Vector3(2.8f, 1.2f, 2.8f);
            Renderer fR = foliage.GetComponent<Renderer>();
            if (fR != null) fR.material.color = new Color(0.85f, 0.42f, 0.10f);
        }

        private void CreatePumpkinProp(Vector3 pos)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "AutumnPumpkinProp";
            p.transform.position = pos;
            p.transform.localScale = new Vector3(0.9f, 0.75f, 0.9f);
            Renderer r = p.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.92f, 0.45f, 0.08f);
        }

        private void CreateEncounterZone(Vector3 centerPos, EncounterDifficulty diff, Vector3 gatePos)
        {
            GameObject zoneObj = new GameObject($"EncounterZone_{diff}");
            zoneObj.transform.position = centerPos;

            BoxCollider box = zoneObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(18f, 6f, 30f);

            // Forward Wooden Gate Barrier
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = $"ForwardGate_{diff}";
            gate.transform.position = gatePos;
            gate.transform.localScale = new Vector3(16f, 4f, 0.8f);
            Renderer gR = gate.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.45f, 0.25f, 0.12f);
            gate.SetActive(false); // Initially unlocked until zone starts

            var encZone = zoneObj.AddComponent<EncounterZone>();

            var diffField = typeof(EncounterZone).GetField("difficulty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (diffField != null) diffField.SetValue(encZone, diff);

            var gateField = typeof(EncounterZone).GetField("forwardGate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (gateField != null) gateField.SetValue(encZone, gate);
        }

        // ==========================================
        // 4. HOLLOW TREE BOSS ARENA REGION (Z: 330 to 420)
        // ==========================================
        private void BuildBossArenaRegion()
        {
            Vector3 arenaCenter = new Vector3(0, 0, 375f);

            // Arena Ground Glade
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "BossArenaGround";
            ground.tag = "Ground";
            ground.transform.position = arenaCenter + new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(45f, 0.5f, 45f);

            // FIX: Destroy height-inflated primitive CapsuleCollider and attach BoxCollider for accurate flat ground physics
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
                // Leave entrance open at angle around PI (back side facing z = 330)
                if (Mathf.Abs(angle - Mathf.PI) < 0.35f) continue;

                Vector3 pos = arenaCenter + new Vector3(Mathf.Cos(angle) * 22f, 0, Mathf.Sin(angle) * 22f);

                GameObject giantTree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                giantTree.name = $"BossBoundaryTree_{i}";
                giantTree.transform.position = pos + new Vector3(0, 4f, 0);
                giantTree.transform.localScale = new Vector3(2.5f, 4f, 2.5f);
                Renderer r = giantTree.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.2f, 0.1f, 0.12f);
            }

            // TOWERING LANDMARK HOLLOW TREE (Visible from distant Forest)
            BuildDistantHollowTreeLandmark(arenaCenter + new Vector3(0, 0, 10f));

            // Spawn Hollow Tree Boss at Center of Arena
            GameObject bossObj = new GameObject("TheHollowTreeBoss");
            bossObj.transform.position = arenaCenter + new Vector3(0, 0, 10f);

            CharacterController cc = bossObj.AddComponent<CharacterController>();
            cc.height = 4.0f;
            cc.radius = 1.6f;
            cc.center = new Vector3(0, 2.0f, 0);

            HollowTreeBossAI bossAI = bossObj.AddComponent<HollowTreeBossAI>();
            bossAI.enabled = true; // Ready for fight

            // Boss Activation Trigger Volume at entrance of arena (Z = 335)
            GameObject bossTriggerObj = new GameObject("BossActivationTriggerVolume");
            bossTriggerObj.transform.position = new Vector3(0, 2f, 335f);
            BoxCollider bossBox = bossTriggerObj.AddComponent<BoxCollider>();
            bossBox.isTrigger = true;
            bossBox.size = new Vector3(24f, 8f, 10f);
            bossTriggerObj.AddComponent<BossActivationTrigger>();

            // Boss Arena Region Trigger
            CreateRegionTrigger("RegionTrigger_HollowGlade", "The Hollow Glade", arenaCenter + new Vector3(0, 5f, 0), new Vector3(50f, 20f, 60f),
                new Color(0.85f, 0.35f, 0.35f), 0.9f, new Color(0.35f, 0.15f, 0.25f), 0.035f, new Color(0.35f, 0.15f, 0.25f));
        }

        private void BuildDistantHollowTreeLandmark(Vector3 pos)
        {
            // Colossal visual landmark trunk towering 30 meters high
            GameObject landmarkTrunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            landmarkTrunk.name = "HollowTreeLandmark_Trunk";
            landmarkTrunk.transform.position = pos + new Vector3(0, 15f, 0);
            landmarkTrunk.transform.localScale = new Vector3(5.5f, 15f, 5.5f);
            Destroy(landmarkTrunk.GetComponent<Collider>());
            Renderer tR = landmarkTrunk.GetComponent<Renderer>();
            if (tR != null) tR.material.color = new Color(0.28f, 0.16f, 0.10f);

            // Colossal glowing autumn canopy
            GameObject landmarkCanopy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            landmarkCanopy.name = "HollowTreeLandmark_Canopy";
            landmarkCanopy.transform.position = pos + new Vector3(0, 32f, 0);
            landmarkCanopy.transform.localScale = new Vector3(18f, 4f, 18f);
            Destroy(landmarkCanopy.GetComponent<Collider>());
            Renderer cR = landmarkCanopy.GetComponent<Renderer>();
            if (cR != null) cR.material.color = new Color(0.95f, 0.40f, 0.05f); // Glowing vibrant orange foliage
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
