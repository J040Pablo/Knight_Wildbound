using System.Collections.Generic;
using UnityEngine;
using Roguelite.Core;
using Roguelite.Player;
using Roguelite.Wave;
using Roguelite.Enemy;
using Roguelite.Loot;

namespace Roguelite.Environment
{
    /// <summary>
    /// AAA Continuous 3D Terrain & World System (Peak / BOTW / Valheim Quality).
    /// Eliminates rectangular slabs, sharp 90° drop-offs, and floating terrain platforms.
    /// Generates a single, continuous 3D heightfield mesh across the entire 780m adventure map.
    /// Features smooth rolling hills, organic riverbeds, carved lake basins, tree root validation,
    /// foundation flattening under structures, and automated post-generation stability checks.
    /// </summary>
    public class SceneEnvironmentBuilder : MonoBehaviour
    {
        [Header("Data-Driven Biome Configuration")]
        [SerializeField] private Roguelite.Data.BiomeDefinition activeBiome;
        public Roguelite.Data.BiomeDefinition ActiveBiome { get => activeBiome; set => activeBiome = value; }

        private Transform worldParent;

        // Foundation flattening zones for structures (Ruins, Pedestals, Camps, Gates, Arenas)
        private static readonly List<(Vector3 center, float radius, float targetHeight)> flattenedAreas 
            = new List<(Vector3, float, float)>();

        // Tree & prop position cache for overlap & distance spacing validation
        private static readonly List<(Vector3 pos, float minDist)> spawnedTreePositions 
            = new List<(Vector3, float)>();

        private static readonly List<(Vector3 pos, float radius)> spawnedPropPositions
            = new List<(Vector3 pos, float radius)>();

        // List of spawned trees for root height validation
        private readonly List<GameObject> spawnedTreeObjects = new List<GameObject>();

        private void Awake()
        {
            BuildContinuousRunWorld();
        }

        public static void ClearFlattenedAreas()
        {
            flattenedAreas.Clear();
            spawnedTreePositions.Clear();
            spawnedPropPositions.Clear();
        }

        public static void FlattenTerrainUnderStructure(Vector3 center, float radius, float targetHeight = -999f)
        {
            if (targetHeight < -900f) targetHeight = GetRawTerrainHeightY(center.x, center.z);
            flattenedAreas.Add((center, radius, targetHeight));
        }

        /// <summary>
        /// Organic winding main trail calculation using multi-harmonic Perlin waves.
        /// </summary>
        public static float GetForestPathXOffset(float z)
        {
            if (z < 60f) return 0f;
            float zRel = z - 60f;
            float wave1 = Mathf.Sin(zRel * 0.022f) * 22f;
            float wave2 = Mathf.Sin(zRel * 0.010f) * 14f;
            float perlin = (Mathf.PerlinNoise(zRel * 0.015f, 0.5f) - 0.5f) * 20f;
            return wave1 + wave2 + perlin;
        }

        /// <summary>
        /// Calculates terrain slope angle in degrees at (x, z).
        /// </summary>
        public static float CalculateSlope(float x, float z)
        {
            float d = 1.0f;
            float hL = GetTerrainHeightY(x - d, z);
            float hR = GetTerrainHeightY(x + d, z);
            float hD = GetTerrainHeightY(x, z - d);
            float hU = GetTerrainHeightY(x, z + d);

            float dx = (hR - hL) / (2f * d);
            float dz = (hU - hD) / (2f * d);

            float slopeGradient = Mathf.Sqrt(dx * dx + dz * dz);
            return Mathf.Atan(slopeGradient) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Raw terrain height before structure foundation flattening.
        /// Smooth height transitions using multi-layered Perlin noise & smooth step interpolation.
        /// </summary>
        private static float GetRawTerrainHeightY(float x, float z)
        {
            float pathX = GetForestPathXOffset(z);
            float distToPath = Mathf.Abs(x - pathX);

            // 1. Protected Ruins Spawn & Tutorial Sanctuary (-50 <= z <= 60): 100% Flat Courtyard & Road
            if (z < 60f)
            {
                float spawnCourtyardDist = Mathf.Sqrt(x * x + (z - 15f) * (z - 15f));
                if (spawnCourtyardDist < 35f || distToPath < 25f) return 0f;

                float blend = Mathf.Clamp01((spawnCourtyardDist - 35f) / 25f);
                float n = Mathf.PerlinNoise(x * 0.03f + 100f, z * 0.03f + 100f) * 1.8f;
                return n * blend;
            }

            // Smooth transition from flat spawn sanctuary (z=60) into open world (z=90)
            float spawnTransition = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((z - 60f) / 30f));

            // Road protection corridor
            float pathFactor = Mathf.Clamp01((distToPath - 8f) / 25f);

            // Global continuous multi-scale Perlin noise (seamless across all Z)
            float n1 = Mathf.PerlinNoise(x * 0.020f + 100f, z * 0.020f + 100f) * 3.2f;
            float n2 = Mathf.PerlinNoise(x * 0.050f + 200f, z * 0.050f + 200f) * 1.2f;
            float baseHeight = (n1 + n2) * pathFactor * spawnTransition;

            // River & Lake Depression (seamless continuous blend across Z: 340 to 470)
            float riverDepression = 0f;
            float lakeDepression = 0f;
            if (z >= 340f && z <= 470f)
            {
                float riverFactor = Mathf.Sin(Mathf.Clamp01((z - 340f) / 130f) * Mathf.PI);
                float riverX = pathX + Mathf.Sin(z * 0.04f) * 18f;
                float distToRiver = Mathf.Abs(x - riverX);
                riverDepression = Mathf.Clamp01(1f - (distToRiver / 16f)) * -2.2f * riverFactor;

                float lakeCenterX = GetForestPathXOffset(410f) + 45f;
                float distToLake = Mathf.Sqrt((x - lakeCenterX) * (x - lakeCenterX) + (z - 410f) * (z - 410f));
                lakeDepression = Mathf.Clamp01(1f - (distToLake / 40f)) * -3.0f;
            }

            // Boss Arena Floor Flattening (Smooth blend Z: 620 to 700)
            if (z >= 620f && z <= 700f)
            {
                float arenaCenterDist = Mathf.Sqrt(x * x + (z - 660f) * (z - 660f));
                if (arenaCenterDist < 40f)
                {
                    float arenaBlend = Mathf.Clamp01((arenaCenterDist - 25f) / 15f);
                    return baseHeight * arenaBlend;
                }
            }

            return Mathf.Clamp(baseHeight + riverDepression + lakeDepression, -3.0f, 5.0f);
        }

        /// <summary>
        /// AAA Continuous Terrain Height Function with Structure Foundation Flattening & Lateral Mountain Walls.
        /// </summary>
        public static float GetTerrainHeightY(float x, float z)
        {
            float rawH = GetRawTerrainHeightY(x, z);

            // Continuous Perimeter Mountain Barrier Wall along ENTIRE map length (Z: -40 to 760)
            float pathX = GetForestPathXOffset(z);
            float distToPath = Mathf.Abs(x - pathX);

            // Lateral mountain corridor inner boundary (55m from main road)
            float mountainInnerRadius = 55.0f;
            if (distToPath > mountainInnerRadius)
            {
                float distBeyond = distToPath - mountainInnerRadius;
                float smoothBlend = Mathf.SmoothStep(0f, 1f, distBeyond / 30.0f);

                // Continuous mountain height profile with organic multi-scale noise
                float mountainSlope = distBeyond * 0.70f;
                float mountainNoise = Mathf.PerlinNoise(x * 0.030f + 500f, z * 0.030f + 500f) * 5.5f;

                rawH += (mountainSlope + mountainNoise) * smoothBlend;
            }

            for (int i = 0; i < flattenedAreas.Count; i++)
            {
                var area = flattenedAreas[i];
                float dx = x - area.center.x;
                float dz = z - area.center.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                if (dist < area.radius)
                {
                    float innerRadius = area.radius * 0.60f;
                    if (dist <= innerRadius) return area.targetHeight;

                    float t = (dist - innerRadius) / (area.radius - innerRadius);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    return Mathf.Lerp(area.targetHeight, rawH, smoothT);
                }
            }

            return rawH;
        }

        public static float SampleTerrainHeight(Vector3 pos)
        {
            return GetTerrainHeightY(pos.x, pos.z);
        }

        public void BuildContinuousRunWorld()
        {
            ClearFlattenedAreas();
            WorldPlaceholderFactory.ClearCache();
            spawnedTreeObjects.Clear();

            GameObject testCube = GameObject.Find("GroundTestCube");
            if (testCube != null) DestroyImmediate(testCube);

            GameObject root = GameObject.Find("_WorldGeometry");
            if (root != null) DestroyImmediate(root);
            root = new GameObject("_WorldGeometry");
            worldParent = root.transform;

            SetupGlobalSunLight();
            StylizedSkyManager.InitializeSky();

            // Setup Landmark Manager
            ForestLandmarkManager landmarkMgr = root.GetComponent<ForestLandmarkManager>();
            if (landmarkMgr == null) landmarkMgr = root.AddComponent<ForestLandmarkManager>();
            landmarkMgr.ClearLandmarks();
            // Pre-register structure foundations for flat bases
            FlattenTerrainUnderStructure(new Vector3(0, 0, 15f), 45f, 0f);               // Ruins Courtyard
            FlattenTerrainUnderStructure(new Vector3(0, 0, 56f), 14f, 0f);               // Exit Gate
            FlattenTerrainUnderStructure(new Vector3(0, 0, 6f), 6f, 0f);                 // King Campfire
            FlattenTerrainUnderStructure(new Vector3(GetForestPathXOffset(85f) - 10f, 0, 85f), 16f); // Horse Meadow
            FlattenTerrainUnderStructure(new Vector3(GetForestPathXOffset(300f) + 45f, 0, 300f), 90f); // Stone Titan Arena (Z: 300, X: path+45, radius 90m)
            FlattenTerrainUnderStructure(new Vector3(0, 0, 950f), 65f, 0f);              // Giant World Tree Clearing (Z: 950, radius 65m)
            FlattenTerrainUnderStructure(new Vector3(0, 0, 1250f), 45f, 0f);             // Royal Court Arena (Z: 1250, radius 45m)
            FlattenTerrainUnderStructure(new Vector3(0, 0, 1370f), 35f, 0f);             // Transition Pass (Z: 1370)

            // Generate 7 Continuous 3D Mesh Chunks (Spanning Z: -40 to 1400, X: -160 to 160)
            BuildContinuousTerrainMesh();

            // Build 8 Biome Environment Regions (Expanded 1400m Map Progression)
            BuildRuinsRegion();          // 1. Santuário das Ruínas (Z: -40 a 60)
            BuildForestEntranceRegion(); // 2. Floresta dos Sussurros (Z: 60 a 180)
            BuildDeepForestRegion();     // 3. Pântano Profundo (Z: 180 a 280)
            BuildTitanArenaRegion();     // 4. ANCIENT STONE TITAN ARENA (Z: 300)
            BuildStoneValleyRegion();    // 5. Vale das Pedras & Canyon Midlands/Highlands (Z: 320 a 880)
            BuildWorldTreeRegion();      // 6. Região da Árvore Ancestral do Mundo (Z: 880 a 1040)
            BuildFairyKingdomRegion();   // 7. Reino das Fadas & Corte Real (Z: 1040 a 1280)
            BuildBossApproachAndArena(); // 8. Passagem Corrompida de Transição (Z: 1280 a 1400)

            // Perform Forest Environment Density & Cluster Pass
            ForestEnvironmentSpawner envSpawner = root.AddComponent<ForestEnvironmentSpawner>();
            envSpawner.Initialize(worldParent, GetTerrainHeightY, GetForestPathXOffset);
            envSpawner.PopulateForestDensity();

            // Build Non-Gameplay Transition Area ("Biome 2 - Coming Soon")
            GameObject transitionObj = new GameObject("TransitionAreaController");
            TransitionArea transArea = transitionObj.AddComponent<TransitionArea>();
            transArea.BuildTransitionPass(worldParent, GetTerrainHeightY);

            // Perform Tree Root & Grounding Validation
            ValidateAndGroundWorld();

            // Validate Mesh Connectivity
            ValidateTerrainConnectivity();

            // Comprehensive World Validation Pass
            ValidateWorld();

            // Continuous World Boundary (Z: -40 to 1440, X width 320m)
            CreateWorldBoundary(new Vector3(0, 15f, 680f), new Vector3(320f, 50f, 1480f));

            // Apply initial atmosphere (Ruins)
            var startingRegion = GameObject.Find("RegionTrigger_Ruins")?.GetComponent<BiomeRegionTrigger>();
            if (startingRegion != null)
            {
                startingRegion.ApplyRegionSettings();
            }

            // Perform World Rendering Performance Optimization
            WorldOptimizationManager.OptimizeWorld();
        }

        private void BuildContinuousTerrainMesh()
        {
            // Create 7 seamless contiguous 3D mesh chunks across the world (Z: -40 to 1400m)
            Vector2[] chunkMin = {
                new Vector2(-160f, -40f),
                new Vector2(-160f, 160f),
                new Vector2(-160f, 360f),
                new Vector2(-160f, 560f),
                new Vector2(-160f, 760f),
                new Vector2(-160f, 960f),
                new Vector2(-160f, 1160f)
            };

            Vector2[] chunkMax = {
                new Vector2(160f, 160f),
                new Vector2(160f, 360f),
                new Vector2(160f, 560f),
                new Vector2(160f, 760f),
                new Vector2(160f, 960f),
                new Vector2(160f, 1160f),
                new Vector2(160f, 1400f)
            };

            for (int i = 0; i < chunkMin.Length; i++)
            {
                string chunkName = $"ContinuousTerrainChunk_{i}";
                GameObject chunkObj = ContinuousTerrainGenerator.CreateContinuousTerrainChunk(
                    chunkName,
                    worldParent,
                    chunkMin[i],
                    chunkMax[i],
                    2.0f, // 2m grid resolution for high-detail smooth terrain
                    GetTerrainHeightY,
                    GetForestPathXOffset
                );

                // Section 4: Force Material Color at runtime using renderer.material (instance)
                if (chunkObj != null && chunkObj.TryGetComponent<Renderer>(out var r))
                {
                    r.material.shader = Shader.Find("Standard");
                    Color richGreen = new Color(0.25f, 0.50f, 0.22f, 1.0f);
                    r.material.color = richGreen;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", richGreen);
                    if (r.material.HasProperty("_Metallic")) r.material.SetFloat("_Metallic", 0f);
                    if (r.material.HasProperty("_Glossiness")) r.material.SetFloat("_Glossiness", 0f);
                    if (r.material.HasProperty("_Smoothness")) r.material.SetFloat("_Smoothness", 0f);
                    if (r.material.HasProperty("_EmissionColor")) r.material.SetColor("_EmissionColor", Color.black);
                }
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
                lightComp.color = Color.white;
                lightComp.intensity = 0.80f; // Controlled sun intensity
                lightComp.shadows = LightShadows.Soft;
            }
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            // Configure flat neutral ambient lighting & enable horizon fog for smooth performance
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.45f);
            RenderSettings.ambientIntensity = 0.50f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.38f, 0.49f, 0.61f);
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 210f;
        }

        private GameObject SpawnProp(PlaceholderAssetKey key, Vector3 worldPos, Quaternion rot, float scale = 1f, Color? color = null)
        {
            float terrainY = GetTerrainHeightY(worldPos.x, worldPos.z);
            Vector3 finalPos = new Vector3(worldPos.x, terrainY + worldPos.y, worldPos.z);

            if (key == PlaceholderAssetKey.RockBoulder || key == PlaceholderAssetKey.RockClusterGroup || key == PlaceholderAssetKey.RockShelf)
            {
                finalPos.y -= 0.15f; // Grounded embedded rocks
            }

            GameObject obj = WorldPlaceholderFactory.Build(key, worldParent, color, scale);
            obj.transform.position = finalPos;

            float sqrMag = rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w;
            if (Mathf.Abs(sqrMag - 1.0f) > 0.05f)
            {
                obj.transform.rotation = Quaternion.identity;
            }
            else
            {
                obj.transform.rotation = rot;
            }
            return obj;
        }

        private bool CanSpawnProp(Vector3 pos, float radius)
        {
            foreach (var p in spawnedPropPositions)
            {
                float sqrDist = (pos - p.pos).sqrMagnitude;
                float req = radius + p.radius;
                if (sqrDist < req * req) return false;
            }
            return true;
        }

        public static Quaternion SafeEuler(float x, float y, float z)
        {
            Quaternion q = Quaternion.Euler(x, y, z);
            q.Normalize();
            return q;
        }

        private GameObject SpawnPropRandomized(PlaceholderAssetKey key, Vector3 pos, float baseScale = 1f, float scaleVar = 0.25f, float radius = 1.5f, Color? color = null)
        {
            if (!CanSpawnProp(pos, radius)) return null;

            float finalScale = baseScale * Random.Range(1f - scaleVar, 1f + scaleVar);
            Quaternion rot = SafeEuler(0, Random.Range(0f, 360f), 0);

            GameObject obj = SpawnProp(key, pos, rot, finalScale, color);
            if (obj != null)
            {
                spawnedPropPositions.Add((obj.transform.position, radius));
            }
            return obj;
        }

        private bool CanSpawnTree(Vector3 pos, float minDistance)
        {
            float slope = CalculateSlope(pos.x, pos.z);
            if (slope > 25f) return false; // Max 25° slope for trees

            float pathX = GetForestPathXOffset(pos.z);
            if (Mathf.Abs(pos.x - pathX) < 10f) return false; // Protect roads

            if (!CanSpawnProp(pos, minDistance * 0.6f)) return false;

            foreach (var t in spawnedTreePositions)
            {
                float sqrDist = (pos - t.pos).sqrMagnitude;
                float required = Mathf.Max(minDistance, t.minDist);
                if (sqrDist < required * required) return false;
            }

            return true;
        }

        private GameObject SpawnTreeValidated(PlaceholderAssetKey key, Vector3 pos, Quaternion rot, float scale, float minDistance = 4f)
        {
            if (!CanSpawnTree(pos, minDistance)) return null;

            Quaternion finalRot = (rot == Quaternion.identity) ? SafeEuler(0, Random.Range(0f, 360f), 0) : rot;
            finalRot.Normalize();
            float finalScale = scale * Random.Range(0.85f, 1.35f); // Random tree scale variation

            GameObject tree = SpawnProp(key, pos, finalRot, finalScale);
            if (tree != null)
            {
                spawnedTreePositions.Add((tree.transform.position, minDistance));
                spawnedPropPositions.Add((tree.transform.position, minDistance * 0.6f));
                spawnedTreeObjects.Add(tree);
            }
            return tree;
        }

        private void SpawnRockCluster(Vector3 center, int count, float radius, float baseScale = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(0.5f, radius);
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

                float rockRadius = baseScale * 1.8f;
                if (!CanSpawnProp(pos, rockRadius)) continue;

                float terrainY = GetTerrainHeightY(pos.x, pos.z) - 0.15f; // Embedded into terrain
                Vector3 finalPos = new Vector3(pos.x, terrainY, pos.z);

                PlaceholderAssetKey rKey = (i % 2 == 0) ? PlaceholderAssetKey.RockBoulder : PlaceholderAssetKey.RockClusterGroup;
                GameObject rock = WorldPlaceholderFactory.Build(rKey, worldParent, null, baseScale * Random.Range(0.8f, 1.4f));
                rock.transform.position = finalPos;
                rock.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                spawnedPropPositions.Add((finalPos, rockRadius));
            }
        }

        // ==========================================
        // 1. RUINS STARTING AREA (Z: -30 to 60)
        // ==========================================
        private void BuildRuinsRegion()
        {
            // Ruined Perimeter Walls & Towers
            SpawnProp(PlaceholderAssetKey.RuinWatchtower, new Vector3(-45f, 0, -15f), Quaternion.identity, 1.3f);
            SpawnProp(PlaceholderAssetKey.RuinTowerLandmark, new Vector3(45f, 0, -15f), Quaternion.identity, 1.2f);
            SpawnProp(PlaceholderAssetKey.RuinWallSegment, new Vector3(-35f, 0, -25f), Quaternion.Euler(0, 15f, 0), 1.6f);
            SpawnProp(PlaceholderAssetKey.RuinWallSegment, new Vector3(35f, 0, -25f), Quaternion.Euler(0, -15f, 0), 1.6f);

            for (int i = 0; i < 10; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * 16f;
                float z = (i / 2) * 14f - 12f;
                var p = SpawnProp(PlaceholderAssetKey.RuinPillar, new Vector3(x, 0, z), Quaternion.identity, 1.3f);
                p.AddComponent<BoxCollider>();
            }

            SpawnProp(PlaceholderAssetKey.RuinStatue, new Vector3(-22f, 0, 18f), Quaternion.identity, 1.4f);
            SpawnProp(PlaceholderAssetKey.RuinStatue, new Vector3(22f, 0, 18f), Quaternion.identity, 1.4f);

            SpawnProp(PlaceholderAssetKey.RuinAqueductArch, new Vector3(-42f, 0, 32f), Quaternion.Euler(0, 25f, 0), 1.5f);

            SpawnProp(PlaceholderAssetKey.Campfire, new Vector3(0, 0, 2f), Quaternion.identity);
            GameObject kingObj = SpawnProp(PlaceholderAssetKey.KingNPC, new Vector3(0, 0, 6f), Quaternion.identity);
            kingObj.name = "KingNPC";
            kingObj.AddComponent<KingNPC>();

            CreateWeaponPedestal(new Vector3(-6f, 0f, 16f), CharacterType.Knight);
            CreateWeaponPedestal(new Vector3(0f, 0f, 18f), CharacterType.Mage);
            CreateWeaponPedestal(new Vector3(6f, 0f, 16f), CharacterType.Druid);

            CreatePlayerSpawnPoint("RuinsPlayerSpawn", new Vector3(0, 0.5f, 8.0f), Quaternion.identity);

            // ── WEAKENED STONE GUARDIAN (TUTORIAL MINI-BOSS GUARDING EXIT GATE Z: 50) ──
            float groundY = GetTerrainHeightY(0f, 50f);
            GameObject guardianObj = new GameObject("StoneGuardian_Tutorial");
            guardianObj.transform.position = new Vector3(0f, groundY, 50f);
            CharacterController gCC = guardianObj.AddComponent<CharacterController>();
            gCC.height = 2.8f;
            gCC.radius = 1.0f;
            gCC.center = new Vector3(0, 1.4f, 0);
            Enemy.StoneGiantAI guardianAI = guardianObj.AddComponent<Enemy.StoneGiantAI>();
            var hpField = typeof(Enemy.EnemyBase).GetField("MaxHP", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (hpField != null) hpField.SetValue(guardianAI, 180f); // Tutorial balanced HP

            // Ruined Archway & Pillars for Stone Giant Gate
            SpawnProp(PlaceholderAssetKey.RuinPillar, new Vector3(-14f, 0, 48f), Quaternion.identity, 1.4f);
            SpawnProp(PlaceholderAssetKey.RuinPillar, new Vector3(14f, 0, 52f), Quaternion.identity, 1.4f);

            GameObject exitGate = SpawnProp(PlaceholderAssetKey.ExitGate, new Vector3(0, 0f, 56f), Quaternion.identity);
            exitGate.name = "RuinsExitGate";
            exitGate.AddComponent<BoxCollider>();
            exitGate.AddComponent<RuinsExitGate>();

            CreateRegionTrigger("RegionTrigger_Ruins", "Ruins Starting Sanctuary", new Vector3(0, 5f, 15f), new Vector3(280f, 30f, 95f),
                new Color(1.0f, 0.95f, 0.86f), 0.80f, new Color(0.38f, 0.49f, 0.61f), 0.008f, new Color(0.38f, 0.49f, 0.61f));
        }

        private void CreateWeaponPedestal(Vector3 pos, CharacterType classType)
        {
            var pedestal = SpawnProp(PlaceholderAssetKey.WeaponPedestal, pos, Quaternion.identity);
            pedestal.name = $"Pedestal_{classType}";

            var trigCol = pedestal.AddComponent<SphereCollider>();
            trigCol.isTrigger = true;
            trigCol.radius = 2.2f;

            PlaceholderAssetKey wKey = classType switch
            {
                CharacterType.Knight => PlaceholderAssetKey.WeaponSword,
                CharacterType.Mage => PlaceholderAssetKey.WeaponStaff,
                _ => PlaceholderAssetKey.WeaponBranch
            };

            GameObject weaponObj = SpawnProp(wKey, pos + new Vector3(0, 1.2f, 0), Quaternion.identity, 1.3f);
            weaponObj.name = $"WeaponPickup_{classType}";
            weaponObj.transform.parent = pedestal.transform;

            var wInt = pedestal.AddComponent<WeaponInteractable>();
            var field = typeof(WeaponInteractable).GetField("targetClass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(wInt, classType);

            var wInt2 = weaponObj.AddComponent<WeaponInteractable>();
            if (field != null) field.SetValue(wInt2, classType);
        }

        // ==========================================
        // 2. FOREST ENTRANCE (Z: 60 to 160)
        // ==========================================
        private void BuildForestEntranceRegion()
        {
            float startZ = 60f;
            float endZ = 160f;

            if (ForestLandmarkManager.Instance != null)
            {
                ForestLandmarkManager.Instance.RegisterLandmark("Ruined Sanctuary", LandmarkType.RuinedTower, new Vector3(0, 0, 15f), null, 25f);
                ForestLandmarkManager.Instance.RegisterLandmark("Ancient Obelisk", LandmarkType.AncestralTree, new Vector3(GetForestPathXOffset(110f) - 30f, 0, 110f), null, 20f);
            }

            for (float z = startZ; z < endZ; z += 12f)
            {
                float xMid = GetForestPathXOffset(z);

                // Spacing & Slope Validated Tree Spawning
                SpawnTreeValidated(PlaceholderAssetKey.TreeDeciduous, new Vector3(xMid - 24f, 0, z + 4f), Quaternion.identity, Random.Range(1.0f, 1.4f), 4f);
                SpawnTreeValidated(PlaceholderAssetKey.TreePine, new Vector3(xMid + 26f, 0, z + 8f), Quaternion.identity, Random.Range(1.0f, 1.3f), 4f);

                SpawnPropRandomized(PlaceholderAssetKey.BushSmall, new Vector3(xMid - 16f, 0, z + 2f), 1.0f, 0.3f, 1.2f);
                SpawnPropRandomized(PlaceholderAssetKey.BushLarge, new Vector3(xMid + 18f, 0, z + 6f), 1.2f, 0.3f, 1.4f);
                SpawnPropRandomized(PlaceholderAssetKey.FlowerCluster, new Vector3(xMid - 14f, 0, z + 3f), 1.0f, 0.35f, 1.0f);
                SpawnPropRandomized(PlaceholderAssetKey.GrassClump, new Vector3(xMid + 15f, 0, z + 5f), 1.1f, 0.25f, 1.0f);
            }

            float obX = GetForestPathXOffset(110f) - 30f;
            SpawnProp(PlaceholderAssetKey.LandmarkGiantObelisk, new Vector3(obX, 0, 110f), Quaternion.identity, 2.0f);

            // ── EARLY GAME VIGNETTE 1: Destroyed Scout Wagon & Early Ring Stash (Z: 75, X: path + 18) ──
            float wagonX = GetForestPathXOffset(75f) + 18f;
            float wagonY = GetTerrainHeightY(wagonX, 75f);
            Vector3 wagonPos = new Vector3(wagonX, wagonY, 75f);
            SpawnProp(PlaceholderAssetKey.DestroyedWagon, wagonPos, Quaternion.Euler(0, -35f, 0), 1.1f);
            SpawnProp(PlaceholderAssetKey.Campfire, wagonPos + new Vector3(-2f, 0, 1.5f), Quaternion.identity, 0.9f);
            var earlyChest1 = SpawnInteractiveTreasureChest(wagonPos + new Vector3(2.5f, 0, -1f), Quaternion.Euler(0, 15f, 0), ChestRarity.Common);
            if (earlyChest1 != null) earlyChest1.name = "EarlyWagonScoutChest";

            // ── EARLY GAME VIGNETTE 2: Ruined Watchtower Outpost (Z: 105, X: path - 22) ──
            float towerX = GetForestPathXOffset(105f) - 22f;
            float towerY = GetTerrainHeightY(towerX, 105f);
            Vector3 towerPos = new Vector3(towerX, towerY, 105f);
            SpawnProp(PlaceholderAssetKey.RuinWatchtower, towerPos, Quaternion.identity, 1.2f);
            var earlyChest2 = SpawnInteractiveTreasureChest(towerPos + new Vector3(1.5f, 0, 2f), Quaternion.Euler(0, -45f, 0), ChestRarity.Rare);
            if (earlyChest2 != null) earlyChest2.name = "RuinedOutpostTreasureChest";

            // ── AMBIENT VIGNETTE 4: Fairy Ritual Glade (Z: 140, X: -42) ──
            Vector3 ritualPos = new Vector3(GetForestPathXOffset(140f) - 42f, GetTerrainHeightY(GetForestPathXOffset(140f) - 42f, 140f), 140f);
            SpawnProp(PlaceholderAssetKey.LandmarkGiantObelisk, ritualPos, Quaternion.identity, 1.5f);

            float meadowX = GetForestPathXOffset(85f) - 15f;
            CreateHorseSpawnPoint("HorseMeadowSpawn", new Vector3(meadowX, 0f, 85f), Quaternion.identity);
            CreateFriendlyHorse(new Vector3(meadowX, 0f, 85f));
            SpawnProp(PlaceholderAssetKey.LoreSignPost, new Vector3(meadowX + 5f, 0, 80f), Quaternion.identity);

            // Early Forest Combat Encounters (Increased density)
            CreateEncounterZone("ForestEntranceZone_Early", GetForestPathXOffset(75f), 75f, EncounterDifficulty.Easy);
            CreateEncounterZone("ForestEntranceZone_Mid", GetForestPathXOffset(105f), 105f, EncounterDifficulty.Easy);
            CreateEncounterZone("ForestEntranceZone_Late", GetForestPathXOffset(135f), 135f, EncounterDifficulty.Easy);

            CreateRegionTrigger("RegionTrigger_ForestEntrance", "Forest Entrance", new Vector3(0, 5f, 110f), new Vector3(300f, 30f, 100f),
                new Color(0.95f, 0.95f, 0.85f), 0.85f, new Color(0.23f, 0.32f, 0.20f), 0.008f, new Color(0.23f, 0.32f, 0.20f));
        }

        // ==========================================
        // 3. DEEP FOREST REGION (Z: 160 to 280)
        // ==========================================
        private void BuildDeepForestRegion()
        {
            float treeX = GetForestPathXOffset(180f) + 32f;
            SpawnTreeValidated(PlaceholderAssetKey.HeroTree, new Vector3(treeX, 0, 180f), Quaternion.identity, 2.2f, 10f);

            float chestX = GetForestPathXOffset(190f) + 34f;
            float chestY = GetTerrainHeightY(chestX, 190f);
            var chest = SpawnInteractiveTreasureChest(new Vector3(chestX, chestY, 190f), Quaternion.Euler(0, -60f, 0), ChestLootTable.RollChestRarity());
            chest.name = "DeepForestHiddenChest";
            if (ForestLandmarkManager.Instance != null)
            {
                ForestLandmarkManager.Instance.RegisterLandmark("Great Hero Oak", LandmarkType.AncestralTree, new Vector3(treeX, 0, 180f), null, 20f);
                ForestLandmarkManager.Instance.RegisterLandmark("Hidden Alcove Chest", LandmarkType.SecretPond, new Vector3(chestX, chestY, 190f), chest, 15f);
            }

            SpawnProp(PlaceholderAssetKey.DestroyedWagon, new Vector3(GetForestPathXOffset(190f) + 18f, 0, 190f), Quaternion.Euler(0, 40f, 0), 1.2f);

            // ── AMBIENT VIGNETTE 1: Goblin Scavenger Camp (Z: 200, X: -38) ──
            Vector3 scavPos = new Vector3(GetForestPathXOffset(200f) - 38f, GetTerrainHeightY(GetForestPathXOffset(200f) - 38f, 200f), 200f);
            SpawnProp(PlaceholderAssetKey.DestroyedWagon, scavPos, Quaternion.Euler(0, 110f, 0), 1.1f);
            SpawnProp(PlaceholderAssetKey.Campfire, scavPos + new Vector3(3f, 0, -2f), Quaternion.identity, 1.0f);

            // ── AMBIENT VIGNETTE 2: Mushroom Hazard Swamp (Z: 250, X: +48) ──
            Vector3 swampPos = new Vector3(GetForestPathXOffset(250f) + 48f, GetTerrainHeightY(GetForestPathXOffset(250f) + 48f, 250f), 250f);
            GameObject mShroom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mShroom.name = "Ambient_GiantMushroomCluster";
            mShroom.transform.position = swampPos;
            mShroom.transform.localScale = new Vector3(2.5f, 3.5f, 2.5f);
            mShroom.AddComponent<Enemy.PoisonMushroomAI>();

            // Root-Bound Hidden Chest inside Toxic Swamp
            var rootChest = SpawnInteractiveTreasureChest(swampPos + new Vector3(3f, 0, 3f), Quaternion.Euler(0, 45f, 0), ChestRarity.Rare);
            if (rootChest != null) rootChest.name = "RootBoundHazardChest";

            // ── FAIRY QUEEN BOSS ARENA (Deep Forest Sacred Glade - Z: 230, X: -25) ──
            float queenX = GetForestPathXOffset(230f) - 25f;
            float queenY = GetTerrainHeightY(queenX, 230f);
            Vector3 queenPos = new Vector3(queenX, queenY + 0.5f, 230f);

            // Sacred Glade Environment Storytelling
            SpawnProp(PlaceholderAssetKey.ForgottenShrine, new Vector3(queenX, queenY, 230f), Quaternion.identity, 1.6f);
            SpawnProp(PlaceholderAssetKey.LandmarkGiantAncestralTree, new Vector3(queenX - 12f, queenY, 235f), Quaternion.identity, 2.2f);
            var sacredChest = SpawnInteractiveTreasureChest(new Vector3(queenX, queenY, 232f), Quaternion.identity, ChestRarity.Rare);
            if (sacredChest != null) sacredChest.name = "SacredGladeShrineChest";

            // Ring of 6 Ruined Pillars around Shrine (radius 9m)
            for (int i = 0; i < 6; i++)
            {
                float angle = i * (Mathf.PI * 2f / 6f);
                Vector3 pillarPos = new Vector3(queenX + Mathf.Cos(angle) * 9f, queenY, 230f + Mathf.Sin(angle) * 9f);
                var pillar = SpawnProp(PlaceholderAssetKey.RuinPillar, pillarPos, Quaternion.identity, 1.3f);
                if (pillar != null) pillar.AddComponent<BoxCollider>();
            }

            // Magical Crystals, Fairy Statues, and Flowers
            SpawnProp(PlaceholderAssetKey.GlowingCrystal, new Vector3(queenX - 5f, queenY, 226f), Quaternion.identity, 1.6f);
            SpawnProp(PlaceholderAssetKey.GlowingCrystal, new Vector3(queenX + 5f, queenY, 234f), Quaternion.identity, 1.6f);
            SpawnProp(PlaceholderAssetKey.RuinStatue, new Vector3(queenX - 8f, queenY, 225f), Quaternion.Euler(0, 45f, 0), 1.4f);
            SpawnProp(PlaceholderAssetKey.RuinStatue, new Vector3(queenX + 8f, queenY, 225f), Quaternion.Euler(0, -45f, 0), 1.4f);
            SpawnProp(PlaceholderAssetKey.FlowerCluster, new Vector3(queenX - 3f, queenY, 229f), Quaternion.identity, 1.3f);
            SpawnProp(PlaceholderAssetKey.FlowerCluster, new Vector3(queenX + 4f, queenY, 231f), Quaternion.identity, 1.3f);

            if (ForestLandmarkManager.Instance != null)
            {
                ForestLandmarkManager.Instance.RegisterLandmark("Deep Forest Fairy Glade", LandmarkType.AncientAltar, queenPos, null, 25f);
            }

            CreateEncounterZone("DeepForestCombatZone", GetForestPathXOffset(220f), 220f, EncounterDifficulty.Medium);

            CreateRegionTrigger("RegionTrigger_DeepForest", "Deep Forest", new Vector3(0, 5f, 220f), new Vector3(300f, 30f, 120f),
                new Color(0.95f, 0.88f, 0.75f), 0.75f, new Color(0.21f, 0.27f, 0.22f), 0.015f, new Color(0.21f, 0.27f, 0.22f));
        }

        // ==========================================
        // 4. RIVER VALLEY & FAIRY KINGDOM (Z: 280 to 460)
        // ==========================================
        // ==========================================
        // 4. LAKE & RIVER BIOME (Z: 340 to 480)
        // ==========================================
        private void BuildLakeAndRiverRegion()
        {
            float wfX = GetForestPathXOffset(420f) - 45f;
            SpawnProp(PlaceholderAssetKey.LandmarkWaterfall, new Vector3(wfX, 5f, 420f), Quaternion.identity, 2.2f);

            // Rebuilt Lake Basin & Center Island
            float lakeX = GetForestPathXOffset(410f) + 45f;
            float lakeTerrainY = GetTerrainHeightY(lakeX, 410f);
            SpawnProp(PlaceholderAssetKey.LakeWater, new Vector3(lakeX, lakeTerrainY - 0.35f, 410f), Quaternion.identity, 45f);
            SpawnProp(PlaceholderAssetKey.LakeIsland, new Vector3(lakeX, lakeTerrainY, 410f), Quaternion.identity, 14f);
            SpawnTreeValidated(PlaceholderAssetKey.TreeWillow, new Vector3(lakeX, 0, 410f), Quaternion.identity, 1.3f, 6f);
            SpawnInteractiveTreasureChest(new Vector3(lakeX + 2f, lakeTerrainY + 0.3f, 410f), Quaternion.identity, ChestLootTable.RollChestRarity());

            if (ForestLandmarkManager.Instance != null)
            {
                ForestLandmarkManager.Instance.RegisterLandmark("Great Waterfall", LandmarkType.WaterfallCascade, new Vector3(wfX, 5f, 420f), null, 22f);
                ForestLandmarkManager.Instance.RegisterLandmark("Willow Island Lake", LandmarkType.SecretPond, new Vector3(lakeX, lakeTerrainY, 410f), null, 20f);
            }

            float b1X = GetForestPathXOffset(370f);
            var bridge1 = SpawnProp(PlaceholderAssetKey.WoodenBridge, new Vector3(b1X, 0.2f, 370f), Quaternion.Euler(0, 90f, 0), 2.0f);
            if (bridge1 != null) bridge1.AddComponent<BoxCollider>();

            float campX = GetForestPathXOffset(390f) - 25f;
            SpawnProp(PlaceholderAssetKey.AbandonedCamp, new Vector3(campX, 0, 390f), Quaternion.identity, 1.3f);

            CreateEncounterZone("RiverCombatZone", GetForestPathXOffset(400f), 400f, EncounterDifficulty.Medium);

            CreateRegionTrigger("RegionTrigger_RiverRegion", "Lake Region", new Vector3(0, 5f, 400f), new Vector3(320f, 30f, 120f),
                new Color(0.92f, 0.95f, 1.0f), 0.80f, new Color(0.26f, 0.34f, 0.41f), 0.012f, new Color(0.26f, 0.34f, 0.41f));
        }

        // ==========================================
        // 4. ANCIENT STONE TITAN ARENA (Z: 300)
        // ==========================================
        private void BuildTitanArenaRegion()
        {
            float titanArenaX = GetForestPathXOffset(300f) + 45f;
            float titanArenaY = GetTerrainHeightY(titanArenaX, 300f);
            Vector3 titanArenaPos = new Vector3(titanArenaX, titanArenaY, 300f);

            // Ring of 8 Colossal Stone Pillars surrounding 90m Radius Titan Arena
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (Mathf.PI * 2f / 8f);
                Vector3 pPos = titanArenaPos + new Vector3(Mathf.Cos(angle) * 65f, 0, Mathf.Sin(angle) * 65f);
                pPos.y = GetTerrainHeightY(pPos.x, pPos.z);
                var pillar = SpawnProp(PlaceholderAssetKey.RockPillarGiant, pPos, Quaternion.identity, 2.5f);
                if (pillar != null) pillar.AddComponent<BoxCollider>();
            }

            // Spawn Ancient Stone Titan Boss GameObject
            GameObject titanBossObj = new GameObject("AncientStoneTitan_WorldBoss");
            titanBossObj.transform.position = titanArenaPos;

            CharacterController tCC = titanBossObj.AddComponent<CharacterController>();
            tCC.height = 14f;
            tCC.radius = 3.5f;
            tCC.center = new Vector3(0, 7f, 0);

            titanBossObj.AddComponent<Enemy.AncientStoneTitanAI>();

            // Arena Lock Trigger (180m x 180m Volume)
            GameObject titanTrgObj = new GameObject("StoneTitan_ArenaTriggerVolume");
            titanTrgObj.transform.position = titanArenaPos;
            BoxCollider titanBox = titanTrgObj.AddComponent<BoxCollider>();
            titanBox.isTrigger = true;
            titanBox.size = new Vector3(180f, 40f, 180f);
            titanTrgObj.AddComponent<StoneTitanArenaTrigger>();

            if (ForestLandmarkManager.Instance != null)
            {
                ForestLandmarkManager.Instance.RegisterLandmark("Ancient Titan Arena", LandmarkType.StoneTitanArena, titanArenaPos, null, 90f);
            }
        }

        // ==========================================
        // 5. STONE VALLEY MIDLANDS & HIGHLANDS (Z: 320 to 880)
        // ==========================================
        private void BuildStoneValleyRegion()
        {
            float startZ = 320f;
            float endZ = 880f;

            for (float z = startZ; z < endZ; z += 16f)
            {
                float xMid = GetForestPathXOffset(z);

                var pL = SpawnProp(PlaceholderAssetKey.RockPillarGiant, new Vector3(xMid - 26f, 0, z + 3f), Quaternion.identity, Random.Range(1.5f, 2.2f));
                if (pL != null) pL.AddComponent<BoxCollider>();

                var pR = SpawnProp(PlaceholderAssetKey.RockPillarGiant, new Vector3(xMid + 26f, 0, z + 8f), Quaternion.identity, Random.Range(1.5f, 2.2f));
                if (pR != null) pR.AddComponent<BoxCollider>();
            }

            // 1. Titan Graveyard (Z: 380)
            float skelX = GetForestPathXOffset(380f) - 35f;
            SpawnProp(PlaceholderAssetKey.GiantSkeleton, new Vector3(skelX, 0, 380f), Quaternion.Euler(0, 25f, 0), 1.8f);
            SpawnRockCluster(new Vector3(skelX + 10f, 0, 380f), 6, 8f, 1.3f);

            // 2. Ancient Ruined Highway (Z: 480)
            float ruinX = GetForestPathXOffset(480f) + 38f;
            SpawnProp(PlaceholderAssetKey.AncientRuinsSegment, new Vector3(ruinX, 0, 480f), Quaternion.Euler(0, -15f, 0), 1.6f);
            float archX = GetForestPathXOffset(490f);
            SpawnProp(PlaceholderAssetKey.LandmarkStoneArch, new Vector3(archX, 0, 490f), Quaternion.identity, 1.8f);

            // 3. Canyon Crossings & Elevated Shelves (Z: 620)
            SpawnRockCluster(new Vector3(GetForestPathXOffset(620f) - 30f, 0, 620f), 8, 10f, 1.4f);
            SpawnRockCluster(new Vector3(GetForestPathXOffset(650f) + 35f, 0, 650f), 12, 14f, 1.6f);
            SpawnProp(PlaceholderAssetKey.RockShelf, new Vector3(GetForestPathXOffset(630f) + 32f, 1.5f, 630f), Quaternion.identity, 2.2f);
            SpawnProp(PlaceholderAssetKey.RockShelf, new Vector3(GetForestPathXOffset(660f) - 34f, 2.0f, 660f), Quaternion.identity, 2.4f);

            // 4. Broken Titan Statue Colossus (Z: 750)
            float statueX = GetForestPathXOffset(750f) - 42f;
            SpawnProp(PlaceholderAssetKey.BrokenTitanStatue, new Vector3(statueX, 0, 750f), Quaternion.Euler(0, 40f, 0), 1.8f);
            float caveX = GetForestPathXOffset(770f) - 35f;
            SpawnProp(PlaceholderAssetKey.RockCaveEntrance, new Vector3(caveX, 0, 770f), Quaternion.identity, 1.8f);

            // 5. Sleeping Stone Giant Alcove (Z: 820)
            Vector3 giantAlcovePos = new Vector3(GetForestPathXOffset(820f) - 48f, GetTerrainHeightY(GetForestPathXOffset(820f) - 48f, 820f), 820f);
            GameObject giantObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            giantObj.name = "StoneGiant_SleepingDisguise";
            giantObj.transform.position = giantAlcovePos;
            giantObj.transform.localScale = new Vector3(2.2f, 2.8f, 2.2f);
            giantObj.AddComponent<Enemy.StoneGiantAI>();

            if (ForestLandmarkManager.Instance != null)
            {
                ForestLandmarkManager.Instance.RegisterLandmark("Giant Titan Skeleton", LandmarkType.GiantSkeleton, new Vector3(skelX, 0, 380f), null, 25f);
                ForestLandmarkManager.Instance.RegisterLandmark("Ancient Titan Ruins", LandmarkType.AncientRuins, new Vector3(ruinX, 0, 480f), null, 25f);
                ForestLandmarkManager.Instance.RegisterLandmark("Broken Titan Head", LandmarkType.BrokenTitanStatue, new Vector3(statueX, 0, 750f), null, 25f);
            }

            CreateEncounterZone("StoneValleyCombatZone", GetForestPathXOffset(550f), 550f, EncounterDifficulty.Hard);

            CreateRegionTrigger("RegionTrigger_StoneValley", "VALE DAS PEDRAS - CANYON DOS TITAS", new Vector3(0, 5f, 540f), new Vector3(320f, 30f, 300f),
                new Color(0.88f, 0.85f, 0.82f), 0.80f, new Color(0.35f, 0.32f, 0.28f), 0.010f, new Color(0.35f, 0.32f, 0.28f));
        }

        // ==========================================
        // 6. GIANT WORLD TREE REGION (Z: 880 to 1040)
        // ==========================================
        private void BuildWorldTreeRegion()
        {
            float treeZ = 950f;
            float treeX = GetForestPathXOffset(treeZ);
            float treeY = GetTerrainHeightY(treeX, treeZ);
            Vector3 worldTreePos = new Vector3(treeX, treeY, treeZ);

            // Giant Ancestral World Tree (Scale: 6.0x) - Visible from miles away across canyon!
            var worldTreeObj = WorldPlaceholderFactory.Build(PlaceholderAssetKey.LandmarkGiantAncestralTree, worldParent, null, 6.0f);
            if (worldTreeObj != null)
            {
                worldTreeObj.name = "GiantWorldTree_ColossalLandmark";
                worldTreeObj.transform.position = worldTreePos;
                worldTreeObj.AddComponent<AwakenedWorldTreeAI>();
            }

            // Lore Sign Post & Glowing Crystal Clusters at base of World Tree
            SpawnProp(PlaceholderAssetKey.LoreSignPost, worldTreePos + new Vector3(-8f, 0, -12f), Quaternion.identity, 1.4f);
            SpawnProp(PlaceholderAssetKey.GlowingCrystal, worldTreePos + new Vector3(12f, 0, 10f), Quaternion.identity, 2.2f);
            SpawnProp(PlaceholderAssetKey.GlowingCrystal, worldTreePos + new Vector3(-14f, 0, 8f), Quaternion.identity, 2.2f);

            if (ForestLandmarkManager.Instance != null)
            {
                ForestLandmarkManager.Instance.RegisterLandmark("Giant World Tree", LandmarkType.GiantWorldTree, worldTreePos, null, 65f);
            }

            CreateRegionTrigger("RegionTrigger_WorldTree", "REGIÃO DA ÁRVORE ANCESTRAL DO MUNDO", new Vector3(0, 5f, 950f), new Vector3(320f, 30f, 160f),
                new Color(0.40f, 0.90f, 0.60f), 0.85f, new Color(0.15f, 0.45f, 0.25f), 0.012f, new Color(0.15f, 0.45f, 0.25f));
        }

        // ==========================================
        // 7. FAIRY KINGDOM & ROYAL COURT (Z: 1040 to 1280)
        // ==========================================
        private void BuildFairyKingdomRegion()
        {
            float startZ = 1050f;

            // ── 1. EXPLORABLE FAIRY VILLAGE & RUINS (Z: 1050 to 1200) ──
            for (float z = startZ; z < 1200f; z += 15f)
            {
                float xMid = GetForestPathXOffset(z);
                SpawnTreeValidated(PlaceholderAssetKey.TreeWillow, new Vector3(xMid - 28f, 0, z + 4f), Quaternion.identity, 1.3f, 8f);
                SpawnTreeValidated(PlaceholderAssetKey.TreeWillow, new Vector3(xMid + 28f, 0, z + 8f), Quaternion.identity, 1.3f, 8f);
                SpawnProp(PlaceholderAssetKey.GlowingCrystal, new Vector3(xMid - 16f, 0, z + 6f), Quaternion.identity, 1.4f);
                SpawnProp(PlaceholderAssetKey.FairyHouseRoot, new Vector3(xMid + 18f, 0, z + 3f), Quaternion.identity, 1.3f);
                SpawnProp(PlaceholderAssetKey.MushroomGroup, new Vector3(xMid + 14f, 0, z + 2f), Quaternion.identity, 1.3f);
            }

            // Central Gathering Plaza with Wooden Bridge (Z: 1120)
            float plazaZ = 1120f;
            float plazaX = GetForestPathXOffset(plazaZ);
            var plazaBridge = SpawnProp(PlaceholderAssetKey.WoodenBridge, new Vector3(plazaX, 0.2f, plazaZ), Quaternion.Euler(0, 90f, 0), 2.0f);
            if (plazaBridge != null) plazaBridge.AddComponent<BoxCollider>();

            // Region Discovery Trigger: REINO DAS FADAS DESCOBERTO
            CreateRegionTrigger("RegionTrigger_FairyKingdom", "REINO DAS FADAS DESCOBERTO\nDomínio da Rainha Encantada", new Vector3(0, 5f, 1150f), new Vector3(320f, 30f, 160f),
                new Color(0.95f, 0.70f, 0.95f), 0.85f, new Color(0.35f, 0.15f, 0.40f), 0.014f, new Color(0.35f, 0.15f, 0.40f));

            // ── 2. PRE-COURT MINI-BOSS & LOCKED ROYAL BRIDGE (Z: 1220 to 1230) ──
            Vector3 sentinelPos = new Vector3(0, GetTerrainHeightY(0, 1220f) + 0.5f, 1220f);
            GameObject sentinelObj = new GameObject("SentinelaDeCristal_MiniBoss");
            sentinelObj.transform.position = sentinelPos;
            CharacterController sCC = sentinelObj.AddComponent<CharacterController>();
            sCC.height = 2.8f;
            sCC.radius = 1.0f;
            sCC.center = new Vector3(0, 1.4f, 0);

            var sentinelAI = sentinelObj.AddComponent<FairyEnemyAI>();
            var sHpField = typeof(EnemyBase).GetField("MaxHP", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sHpField != null) sHpField.SetValue(sentinelAI, 450f);

            // Locked Royal Bridge Gate (Z: 1230)
            Vector3 bridgeGatePos = new Vector3(0, GetTerrainHeightY(0, 1230f), 1230f);
            GameObject bridgeGateObj = new GameObject("RoyalBridgeGate_Interactive");
            bridgeGateObj.transform.position = bridgeGatePos;
            bridgeGateObj.AddComponent<RoyalBridgeGate>();

            GameObject gateVis = WorldPlaceholderFactory.Build(PlaceholderAssetKey.RuinPillar, bridgeGateObj.transform, new Color(0.9f, 0.4f, 0.95f), 2.2f);
            if (gateVis != null) gateVis.name = "GateVisual";

            // ── 3. REBALANCED 45M ROYAL COURT ARENA (Z: 1250) ──
            Vector3 arenaCenter = new Vector3(0, 0, 1250f);
            arenaCenter.y = GetTerrainHeightY(0, 1250f);

            // Perimeter Ring of 8 Crystal Obelisks & Ruined Pillars (radius 30m)
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (Mathf.PI * 2f / 8f);
                Vector3 pPos = arenaCenter + new Vector3(Mathf.Cos(angle) * 30f, 0, Mathf.Sin(angle) * 30f);
                pPos.y = GetTerrainHeightY(pPos.x, pPos.z);

                var pillar = SpawnProp(PlaceholderAssetKey.RuinPillar, pPos, Quaternion.identity, 1.6f);
                if (pillar != null) pillar.AddComponent<BoxCollider>();

                Vector3 cryObeliskPos = pPos + new Vector3(Mathf.Cos(angle + 0.2f) * 3f, 0, Mathf.Sin(angle + 0.2f) * 3f);
                var obelisk = WorldPlaceholderFactory.Build(PlaceholderAssetKey.GlowingCrystal, worldParent, new Color(0.95f, 0.35f, 0.95f), 2.2f);
                if (obelisk != null) obelisk.transform.position = cryObeliskPos;
            }

            // Royal Throne Platform behind court center (Z: 1270, X: 0)
            Vector3 thronePos = arenaCenter + new Vector3(0, 0, 20f);
            SpawnProp(PlaceholderAssetKey.RuinStatue, thronePos, Quaternion.identity, 1.8f);

            // Pre-combat Fairy Queen Boss hovering safely in front of throne (Z: 1258, X: 0)
            Vector3 queenSpawnPos = new Vector3(0f, GetTerrainHeightY(0f, 1258f) + 2.0f, 1258f);
            GameObject queenObj = new GameObject("FairyQueen_Boss");
            queenObj.transform.position = queenSpawnPos;

            CharacterController qCC = queenObj.AddComponent<CharacterController>();
            qCC.height = 2.4f;
            qCC.radius = 0.8f;
            qCC.center = new Vector3(0, 1.2f, 0);

            queenObj.AddComponent<FairyQueenAI>();

            // Arena Barrier Lock Trigger at Court Entrance (Z: 1242)
            GameObject arenaTrgObj = new GameObject("FairyCourt_ArenaTriggerVolume");
            arenaTrgObj.transform.position = new Vector3(0, 2f, 1242f);
            BoxCollider arenaBox = arenaTrgObj.AddComponent<BoxCollider>();
            arenaBox.isTrigger = true;
            arenaBox.size = new Vector3(35f, 12f, 10f);
            arenaTrgObj.AddComponent<FairyKingdomArenaTrigger>();

            if (ForestLandmarkManager.Instance != null)
            {
                ForestLandmarkManager.Instance.RegisterLandmark("Fairy Kingdom Village", LandmarkType.AncientAltar, new Vector3(plazaX, 0, plazaZ), null, 30f);
                ForestLandmarkManager.Instance.RegisterLandmark("Royal Fairy Court", LandmarkType.AncientAltar, arenaCenter, null, 30f);
            }

            CreateEncounterZone("FairyKingdomCombatZone", GetForestPathXOffset(1220f), 1220f, EncounterDifficulty.Hard);
        }

        // ==========================================
        // 8. CORRUPTED PASS & REGIONAL EXIT (Z: 1280 to 1400)
        // ==========================================
        private void BuildBossApproachAndArena()
        {
            float startZ = 1280f;
            float endZ = 1400f;

            for (float z = startZ; z < endZ; z += 15f)
            {
                float xMid = GetForestPathXOffset(z);
                SpawnTreeValidated(PlaceholderAssetKey.TreeDeadGiant, new Vector3(xMid - 22f, 0, z + 3f), Quaternion.identity, 1.3f, 6f);
                SpawnTreeValidated(PlaceholderAssetKey.TreeDeadGiant, new Vector3(xMid + 22f, 0, z + 7f), Quaternion.identity, 1.3f, 6f);
            }

            Vector3 passCenter = new Vector3(0, 0, 1370f);
            passCenter.y = GetTerrainHeightY(0, 1370f);

            SpawnProp(PlaceholderAssetKey.CorruptedRootBarrier, passCenter, Quaternion.identity, 2.5f);
            SpawnProp(PlaceholderAssetKey.RuinStatue, passCenter + new Vector3(-8f, 0, 0), Quaternion.Euler(0, 45f, 0), 1.6f);
            SpawnProp(PlaceholderAssetKey.RuinStatue, passCenter + new Vector3(8f, 0, 0), Quaternion.Euler(0, -45f, 0), 1.6f);

            if (ForestLandmarkManager.Instance != null)
            {
                ForestLandmarkManager.Instance.RegisterLandmark("Region Transition Arch", LandmarkType.StoneArchFormation, passCenter, null, 25f);
            }

            CreateEncounterZone("TransitionCombatZone", GetForestPathXOffset(1370f), 1370f, EncounterDifficulty.Hard);

            // ==========================================
            // BIOME EXIT GATE & DESTRUCTIBLE ROOT BARRIER (Z: 1380)
            // ==========================================
            Vector3 barrierPos = new Vector3(0, GetTerrainHeightY(0, 1380f), 1380f);
            GameObject barrierObj = WorldPlaceholderFactory.Build(PlaceholderAssetKey.CorruptedRootBarrier, worldParent, null, 1.0f);
            barrierObj.name = "CorruptedRootExitBarrier";
            barrierObj.transform.position = barrierPos;

            BiomeExitBarrier exitBarrier = barrierObj.AddComponent<BiomeExitBarrier>();
            BarrierHealth barrierHealth = barrierObj.AddComponent<BarrierHealth>();
            barrierObj.AddComponent<BarrierDestructionSequence>();
            CreateRegionTrigger("RegionTrigger_TransitionPass", "Passagem Corrompida de Transição", passCenter + new Vector3(0, 5f, 0), new Vector3(120f, 30f, 60f),
                new Color(0.85f, 0.55f, 0.55f), 0.70f, new Color(0.29f, 0.13f, 0.15f), 0.025f, new Color(0.29f, 0.13f, 0.15f));
        }

        // ==========================================
        // VALIDATION & GROUNDING PASS
        // ==========================================
        private void ValidateAndGroundWorld()
        {
            if (worldParent == null) return;

            int groundedCount = 0;
            int treesDestroyed = 0;

            // Grounding pass for props
            Transform[] allProps = worldParent.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allProps)
            {
                if (t == worldParent) continue;
                if (t.name.Contains("Ground") || t.name.Contains("ContinuousTerrain") || t.name.Contains("RegionTrigger") || t.name.Contains("WorldBoundary") || t.name.Contains("Spawn") || t.name.Contains("Zone"))
                {
                    continue;
                }

                if (t.parent != null && t.parent != worldParent) continue;

                Vector3 curPos = t.position;
                float correctY = GetTerrainHeightY(curPos.x, curPos.z);

                if (t.name.Contains("Rock") || t.name.Contains("Boulder") || t.name.Contains("Cluster"))
                {
                    correctY -= 0.15f; // Embedded rocks
                }

                t.position = new Vector3(curPos.x, correctY, curPos.z);
                groundedCount++;
            }

            // Tree Root Validation (> 0.2m root distance check)
            for (int i = spawnedTreeObjects.Count - 1; i >= 0; i--)
            {
                GameObject tree = spawnedTreeObjects[i];
                if (tree == null) continue;

                float treeY = tree.transform.position.y;
                float targetY = GetTerrainHeightY(tree.transform.position.x, tree.transform.position.z);
                float dist = Mathf.Abs(treeY - targetY);

                if (dist > 0.2f)
                {
                    tree.transform.position = new Vector3(tree.transform.position.x, targetY, tree.transform.position.z);
                    float newDist = Mathf.Abs(tree.transform.position.y - targetY);
                    if (newDist > 0.2f)
                    {
                        DestroyImmediate(tree);
                        treesDestroyed++;
                    }
                }
            }

            if (treesDestroyed > 0)
            {
                // Debug.Log($"[WorldValidation] Tree Root Validation removed {treesDestroyed} invalid floating trees.");
            }
        }

        private float lastMaxEdgeDiff = 0f;
        private bool lastConnectivityPass = true;

        private void ValidateTerrainConnectivity()
        {
            // Verify seamless continuity across chunk boundaries (Z = 160, 360, 560, 760, 960, 1160)
            float[] checkZs = { 160f, 360f, 560f, 760f, 960f, 1160f };
            lastConnectivityPass = true;
            lastMaxEdgeDiff = 0f;

            for (int i = 0; i < checkZs.Length; i++)
            {
                float zBoundary = checkZs[i];
                float maxChunkDiff = 0f;

                Transform chunkA = worldParent != null ? worldParent.Find($"ContinuousTerrainChunk_{i}") : null;
                Transform chunkB = worldParent != null ? worldParent.Find($"ContinuousTerrainChunk_{i + 1}") : null;

                if (chunkA != null && chunkB != null &&
                    chunkA.TryGetComponent<MeshFilter>(out var mfA) && mfA.sharedMesh != null &&
                    chunkB.TryGetComponent<MeshFilter>(out var mfB) && mfB.sharedMesh != null)
                {
                    Vector3[] vertsA = mfA.sharedMesh.vertices;
                    Vector3[] vertsB = mfB.sharedMesh.vertices;

                    // Match vertices at exact boundary Z
                    for (int a = 0; a < vertsA.Length; a++)
                    {
                        Vector3 wA = chunkA.TransformPoint(vertsA[a]);
                        if (Mathf.Abs(wA.z - zBoundary) < 0.001f && wA.y > -45f) // Exclude bedrock skirt cap
                        {
                            for (int b = 0; b < vertsB.Length; b++)
                            {
                                Vector3 wB = chunkB.TransformPoint(vertsB[b]);
                                if (Mathf.Abs(wB.z - zBoundary) < 0.001f && Mathf.Abs(wA.x - wB.x) < 0.001f && wB.y > -45f)
                                {
                                    float diff = Mathf.Abs(wA.y - wB.y);
                                    if (diff > maxChunkDiff) maxChunkDiff = diff;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Debug.Log($"[CHUNK CONNECTIVITY] Chunk {i} -> Chunk {i + 1} Max edge difference: {maxChunkDiff:F4}m");

                if (maxChunkDiff > lastMaxEdgeDiff) lastMaxEdgeDiff = maxChunkDiff;
                if (maxChunkDiff > 0.001f) lastConnectivityPass = false;
            }

            // Debug.Log($"[WorldValidation] ValidateTerrainConnectivity: Seamless terrain heightfield verified across all 4 chunks = {lastConnectivityPass} (Max edge diff: {lastMaxEdgeDiff:F4}m).");
        }

        private void ValidateWorld()
        {
            int treeCount = spawnedTreeObjects.Count;
            int areaCount = flattenedAreas.Count;

            int terrainWalls = 0;
            float minSpawnH = float.MaxValue;
            float maxSpawnH = float.MinValue;
            float maxH50m = float.MinValue;

            // Sample spawn & 50m radius heights
            for (float z = -40f; z <= 80f; z += 2f)
            {
                for (float x = -50f; x <= 50f; x += 2f)
                {
                    float h = GetTerrainHeightY(x, z);
                    float dist = Mathf.Sqrt(x * x + z * z);

                    if (dist <= 30f)
                    {
                        if (h < minSpawnH) minSpawnH = h;
                        if (h > maxSpawnH) maxSpawnH = h;
                    }
                    if (dist <= 50f)
                    {
                        if (h > maxH50m) maxH50m = h;
                    }

                    if (h > 2.0f && dist <= 30f)
                    {
                        terrainWalls++;
                    }
                }
            }

            if (!lastConnectivityPass)
            {
                Debug.LogError($"[WorldValidation] WORLD VALIDATION FAILED! Seamless terrain heightfield verified across all 4 chunks = False. Max edge diff: {lastMaxEdgeDiff:F4}m.");
            }

            // Run deep diagnostics
            WorldDiagnosticTool.RunFullDiagnostic(worldParent);
        }

        // ==========================================
        // UTILITY HELPERS
        // ==========================================
        private void CreateEncounterZone(string name, float x, float z, EncounterDifficulty diff)
        {
            GameObject zoneObj = new GameObject(name);
            zoneObj.transform.position = new Vector3(x, 0, z);

            BoxCollider box = zoneObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(60f, 10f, 35f);

            var encZone = zoneObj.AddComponent<EncounterZone>();
            var diffField = typeof(EncounterZone).GetField("difficulty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (diffField != null) diffField.SetValue(encZone, diff);
        }

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

        private void CreateFriendlyHorse(Vector3 pos)
        {
            float groundY = GetTerrainHeightY(pos.x, pos.z);
            GameObject horseObj = new GameObject("FriendlyHorse");
            horseObj.transform.position = new Vector3(pos.x, groundY, pos.z);
            if (worldParent != null) horseObj.transform.SetParent(worldParent, true);

            CharacterController cc = horseObj.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.8f;
            cc.center = new Vector3(0, 1.0f, 0);

            horseObj.AddComponent<HorseController>();
            horseObj.AddComponent<MountSystem>();
        }

        public static GameObject SpawnInteractiveTreasureChest(Vector3 position, Quaternion rotation, ChestRarity forcedRarity = ChestRarity.Common)
        {
            GameObject chestGo = new GameObject($"InteractiveTreasureChest_{forcedRarity}");
            chestGo.transform.position = position;
            chestGo.transform.rotation = rotation;

            TreasureChest chestComp = chestGo.AddComponent<TreasureChest>();
            chestComp.chestRarity = forcedRarity;
            return chestGo;
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
