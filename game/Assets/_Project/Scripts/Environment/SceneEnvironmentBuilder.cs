using UnityEngine;
using UnityEngine.SceneManagement;
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
            BuildEnvironmentForActiveScene();
        }

        public void BuildEnvironmentForActiveScene()
        {
            string activeSceneName = SceneManager.GetActiveScene().name;

            if (activeSceneName == SceneTransitionManager.SCENE_RUINS || activeSceneName == "MainScene" || activeSceneName == "GameArena")
            {
                BuildRuinsEnvironment();
            }
            else if (activeSceneName == SceneTransitionManager.SCENE_FOREST)
            {
                BuildForestEnvironment();
            }
            else if (activeSceneName == SceneTransitionManager.SCENE_BOSS)
            {
                BuildBossEnvironment();
            }
            else
            {
                BuildRuinsEnvironment();
            }
        }

        public Vector3 GetSafePlayerSpawnPosition()
        {
            return new Vector3(0, 1.2f, -3f);
        }

        // ==========================================
        // 1. SCENE 01 — RUINS ENVIRONMENT BUILDER
        // ==========================================
        private void BuildRuinsEnvironment()
        {
            // Ground Cobblestone
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "RuinsGround";
            ground.transform.position = new Vector3(0, -0.5f, 15f);
            ground.transform.localScale = new Vector3(35f, 0.5f, 45f);
            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.42f, 0.40f, 0.38f); // Ancient stone grey

            // Lighting
            SetupLighting(new Color(1.0f, 0.9f, 0.75f), 1.2f, new Color(0.4f, 0.4f, 0.5f), 0.015f);

            // Ruined Pillars & Archways
            for (int i = 0; i < 8; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * 9f;
                float z = (i / 2) * 10f;

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

            // Explicit Player & Horse Spawn Points + World Boundary setup
            CreatePlayerSpawnPoint("RuinsPlayerSpawn", new Vector3(0, 0.5f, -3f), Quaternion.identity);
            CreateHorseSpawnPoint("RuinsHorseSpawn", new Vector3(-7f, 0f, 18f), Quaternion.identity);
            CreateWorldBoundary(new Vector3(0, 5f, 15f), new Vector3(36f, 20f, 48f));

            // Friendly Horse resting area
            CreateFriendlyHorse(new Vector3(-7f, 0f, 18f));

            // Ruins Exit Gate to Forest
            GameObject exitGate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitGate.name = "RuinsExitGate";
            exitGate.transform.position = new Vector3(0, 2.0f, 32f);
            exitGate.transform.localScale = new Vector3(6.0f, 4.0f, 0.5f);
            exitGate.GetComponent<Collider>().isTrigger = true;
            Renderer exR = exitGate.GetComponent<Renderer>();
            if (exR != null) exR.material.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            exitGate.AddComponent<RuinsExitGate>();
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
        // 2. SCENE 02 — FOREST ENVIRONMENT BUILDER
        // ==========================================
        private void BuildForestEnvironment()
        {
            // Autumn Lighting & Atmospheric Fog
            SetupLighting(new Color(1.0f, 0.85f, 0.65f), 1.15f, new Color(0.45f, 0.35f, 0.3f), 0.022f);

            float routeLength = 280f;

            // Dirt Road Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "ForestRoadGround";
            ground.transform.position = new Vector3(0, -0.5f, routeLength / 2f);
            ground.transform.localScale = new Vector3(18f, 0.5f, routeLength);
            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.38f, 0.28f, 0.18f); // Autumn dirt path brown

            // Crooked Trees & Pumpkins along route
            for (int z = 0; z < (int)routeLength; z += 12)
            {
                CreateAutumnTree(new Vector3(-8f, 0, z));
                CreateAutumnTree(new Vector3(8f, 0, z + 6));

                if (z % 24 == 0)
                {
                    CreatePumpkinProp(new Vector3(-6.5f, 0.4f, z + 4));
                    CreatePumpkinProp(new Vector3(6.5f, 0.4f, z + 10));
                }
            }

            // Gated Encounters along route
            // 1. Easy Encounter (z = 40)
            CreateEncounterZone(new Vector3(0, 0, 40), EncounterDifficulty.Easy, new Vector3(0, 1.5f, 60));

            // Friendly Horse Safe Stop 1 (z = 75)
            CreateFriendlyHorse(new Vector3(-5f, 0, 75));

            // 2. Medium Encounter (z = 110)
            CreateEncounterZone(new Vector3(0, 0, 110), EncounterDifficulty.Medium, new Vector3(0, 1.5f, 135));

            // Friendly Horse Safe Stop 2 (z = 150)
            CreateFriendlyHorse(new Vector3(5f, 0, 150));

            // 3. Hard Encounter (z = 190)
            CreateEncounterZone(new Vector3(0, 0, 190), EncounterDifficulty.Hard, new Vector3(0, 1.5f, 220));

            // Explicit Player & Horse Spawn Points + World Boundary setup
            CreatePlayerSpawnPoint("ForestPlayerSpawn", new Vector3(0, 0.5f, 2f), Quaternion.identity);
            CreateHorseSpawnPoint("ForestHorseSpawn", new Vector3(-5f, 0f, 8f), Quaternion.identity);
            CreateWorldBoundary(new Vector3(0, 5f, routeLength / 2f), new Vector3(20f, 20f, routeLength + 10f));

            // Forest Exit Portal to Boss Lair (z = 250)
            GameObject bossPortal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bossPortal.name = "ForestExitPortal";
            bossPortal.transform.position = new Vector3(0, 2.0f, 250f);
            bossPortal.transform.localScale = new Vector3(4.0f, 0.2f, 4.0f);
            bossPortal.transform.rotation = Quaternion.Euler(90, 0, 0);
            bossPortal.GetComponent<Collider>().isTrigger = true;
            Renderer pR = bossPortal.GetComponent<Renderer>();
            if (pR != null) pR.material.color = new Color(0.7f, 0.2f, 0.8f, 0.7f);

            var portalComp = bossPortal.AddComponent<ForestExitPortal>();
            portalComp.UnlockPortal(); // Auto-unlock for route completion flow
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
            if (fR != null) fR.material.color = new Color(0.85f, 0.42f, 0.10f); // Vibrant autumn foliage
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
        // 3. SCENE 03 — FOREST BOSS ENVIRONMENT BUILDER
        // ==========================================
        private void BuildBossEnvironment()
        {
            // Dark Corrupted Boss Arena Lighting
            SetupLighting(new Color(0.85f, 0.35f, 0.35f), 0.9f, new Color(0.35f, 0.15f, 0.25f), 0.035f);

            // Arena Ground Glade
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "BossArenaGround";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(45f, 0.5f, 45f);
            Renderer gR = ground.GetComponent<Renderer>();
            if (gR != null) gR.material.color = new Color(0.25f, 0.18f, 0.15f); // Dark corrupted dirt

            // Surrounding Ring of Colossal Corrupted Trees
            int treeRingCount = 20;
            for (int i = 0; i < treeRingCount; i++)
            {
                float angle = (i / (float)treeRingCount) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 23f, 0, Mathf.Sin(angle) * 23f);

                GameObject giantTree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                giantTree.name = $"BossBoundaryTree_{i}";
                giantTree.transform.position = pos + new Vector3(0, 4f, 0);
                giantTree.transform.localScale = new Vector3(2.5f, 4f, 2.5f);
                Renderer r = giantTree.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.2f, 0.1f, 0.12f);
            }

            // Explicit Player & Horse Spawn Points + World Boundary setup
            CreatePlayerSpawnPoint("ForestBossPlayerSpawn", new Vector3(0, 0.5f, -15f), Quaternion.identity);
            CreateHorseSpawnPoint("ForestBossHorseSpawn", new Vector3(0, 0.5f, -18f), Quaternion.identity);
            CreateWorldBoundary(Vector3.zero, new Vector3(46f, 20f, 46f));

            // Spawn Hollow Tree Boss at Center
            GameObject bossObj = new GameObject("TheHollowTreeBoss");
            bossObj.transform.position = new Vector3(0, 0, 8f);

            CharacterController cc = bossObj.AddComponent<CharacterController>();
            cc.height = 4.0f;
            cc.radius = 1.6f;
            cc.center = new Vector3(0, 2.0f, 0);

            bossObj.AddComponent<HollowTreeBossAI>();
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

        private void SetupLighting(Color sunColor, float intensity, Color ambientColor, float fogDensity)
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
                lightComp.color = sunColor;
                lightComp.intensity = intensity;
            }
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            RenderSettings.ambientLight = ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogColor = ambientColor;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
        }
    }
}
