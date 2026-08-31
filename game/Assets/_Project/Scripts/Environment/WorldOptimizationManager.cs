using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Roguelite.Environment
{
    /// <summary>
    /// AAA World Rendering Optimization Manager.
    /// Eliminates camera movement lag, stuttering, and pop-in across the continuous 780m adventure map.
    /// </summary>
    public class WorldOptimizationManager : MonoBehaviour
    {
        public static WorldOptimizationManager Instance { get; private set; }

        [Header("Camera & View Distance")]
        [SerializeField] private float viewDistance = 350f; // High view distance to prevent far clipping
        [SerializeField] private float nearClipPlane = 0.05f;
        [SerializeField] private bool enableOcclusionCulling = false; // Disabled unbaked occlusion checks to prevent CPU hitching

        [Header("Atmospheric Fog")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private Color defaultFogColor = new Color(0.38f, 0.49f, 0.61f);
        [SerializeField] private float fogDensity = 0.005f;
        [SerializeField] private float fogStartDistance = 140f; // Soft fog begins far away at 140m
        [SerializeField] private float fogEndDistance = 340f;   // Merges with horizon at 340m

        [Header("Shadow Settings")]
        [SerializeField] private float shadowDistance = 120f;   // Crisp shadows within 120m radius
        [SerializeField] private int shadowCascades = 2;

        [Header("Dynamic LOD Thresholds")]
        [SerializeField] private float lod0Threshold = 0.04f;   // 4% screen height (~90m-100m range for LOD0)
        [SerializeField] private float lod1Threshold = 0.012f;  // 1.2% screen height (~180m-200m range for LOD1)
        [SerializeField] private float lod2Threshold = 0.003f;  // 0.3% screen height (~300m-340m range for LOD2)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            OptimizeWorld();
        }

        /// <summary>
        /// Single static entry point called ONCE after scene environment generation completes.
        /// Zero per-frame scanning or runtime rebuilding.
        /// </summary>
        public static void OptimizeWorld()
        {
            GameObject mgrObj = GameObject.Find("WorldOptimizationManager");
            if (mgrObj == null)
            {
                mgrObj = new GameObject("WorldOptimizationManager");
            }

            WorldOptimizationManager manager = mgrObj.GetComponent<WorldOptimizationManager>();
            if (manager == null)
            {
                manager = mgrObj.AddComponent<WorldOptimizationManager>();
            }

            manager.ApplyCameraAndQualitySettings();
            manager.ApplyAtmosphericFog();
            manager.OptimizeShadowsAndSmallProps();
            manager.BuildDynamicLODGroups();

            Debug.Log("[WorldOptimizationManager] World rendering optimization pass executed cleanly!");
        }

        public void ApplyCameraAndQualitySettings()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.nearClipPlane = nearClipPlane;
                mainCam.farClipPlane = viewDistance;
                mainCam.useOcclusionCulling = enableOcclusionCulling;
            }

            // Quality & Shadow Distance Tuning
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadowCascades = shadowCascades;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        public void ApplyAtmosphericFog()
        {
            if (!enableFog) return;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = defaultFogColor;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
            RenderSettings.fogDensity = fogDensity;
        }

        public void OptimizeShadowsAndSmallProps()
        {
            Transform worldGeometry = transform.parent != null ? transform.parent : GameObject.Find("_WorldGeometry")?.transform;
            if (worldGeometry == null) return;

            int disabledShadowCount = 0;

            // Search all renderers in world geometry
            Renderer[] allRenderers = worldGeometry.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in allRenderers)
            {
                if (r == null) continue;

                // CRITICAL SAFETY CHECK: Never modify Horse, Player, NPCs, Bosses, or Chests
                if (IsProtectedEntity(r.gameObject)) continue;

                string objName = r.gameObject.name.ToLower();
                string parentName = r.transform.parent != null ? r.transform.parent.name.ToLower() : "";

                // Disable shadow casting on minor decorations to save draw calls
                if (objName.Contains("grass") || objName.Contains("flower") || objName.Contains("shroom") ||
                    objName.Contains("pebble") || objName.Contains("fern") || objName.Contains("bushsm") ||
                    objName.Contains("mist") || parentName.Contains("flowers") || parentName.Contains("grass") ||
                    parentName.Contains("shroom"))
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    disabledShadowCount++;
                }
            }

            Debug.Log($"[WorldOptimizationManager] Disabled shadow casting on {disabledShadowCount} minor decoration renderers.");
        }

        public void BuildDynamicLODGroups()
        {
            Transform worldGeometry = transform.parent != null ? transform.parent : GameObject.Find("_WorldGeometry")?.transform;
            if (worldGeometry == null) return;

            int lodGroupsCreated = 0;

            // Process top-level children under _WorldGeometry
            for (int i = 0; i < worldGeometry.childCount; i++)
            {
                Transform child = worldGeometry.GetChild(i);
                if (child == null) continue;

                // CRITICAL SAFETY CHECK: Never attach LODGroup to Horse, Player, Characters, Chests, or Spawners
                if (IsProtectedEntity(child.gameObject))
                {
                    LODGroup existingLod = child.GetComponent<LODGroup>();
                    if (existingLod != null) DestroyImmediate(existingLod);
                    continue;
                }

                string name = child.name;
                if (name.StartsWith("ContinuousTerrain") || name.StartsWith("RegionTrigger") ||
                    name.StartsWith("WorldBoundary") || name.StartsWith("Spawn") ||
                    name.Contains("Manager") || name.Contains("Area"))
                {
                    continue;
                }

                // If LODGroup already exists, skip
                if (child.GetComponent<LODGroup>() != null) continue;

                Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;

                // Categorize object type
                bool isTree = name.Contains("Tree") || name.Contains("Oak") || name.Contains("Pine") || name.Contains("Willow") || name.Contains("Ancient") || name.Contains("Hero");
                bool isStructure = name.Contains("Tower") || name.Contains("Wall") || name.Contains("Gate") || name.Contains("Arch") || name.Contains("Obelisk") || name.Contains("Pillar");
                bool isRock = name.Contains("Rock") || name.Contains("Boulder") || name.Contains("Cliff");

                if (!isTree && !isStructure && !isRock)
                {
                    // Skip small single props
                    continue;
                }

                // Create LODGroup
                LODGroup lodGroup = child.gameObject.AddComponent<LODGroup>();

                List<Renderer> lod0Renderers = new List<Renderer>();
                List<Renderer> lod1Renderers = new List<Renderer>();
                List<Renderer> lod2Renderers = new List<Renderer>();

                foreach (Renderer r in renderers)
                {
                    if (r == null) continue;
                    lod0Renderers.Add(r);
                    lod1Renderers.Add(r); // Retain essential foliage and structures in LOD1 to eliminate popping
                    lod2Renderers.Add(r); // Retain core geometry in LOD2
                }

                LOD[] lods = new LOD[3];
                lods[0] = new LOD(lod0Threshold, lod0Renderers.ToArray()) { fadeTransitionWidth = 0.25f };
                lods[1] = new LOD(lod1Threshold, lod1Renderers.ToArray()) { fadeTransitionWidth = 0.25f };
                lods[2] = new LOD(lod2Threshold, lod2Renderers.ToArray()) { fadeTransitionWidth = 0.25f };

                lodGroup.SetLODs(lods);
                lodGroup.animateCrossFading = true;
                lodGroup.RecalculateBounds();
                lodGroupsCreated++;
            }

            Debug.Log($"[WorldOptimizationManager] Dynamically constructed {lodGroupsCreated} smooth 3-stage LODGroup components.");
        }

        private bool IsProtectedEntity(GameObject go)
        {
            if (go == null) return true;
            string n = go.name.ToLower();

            if (n.Contains("horse") || n.Contains("player") || n.Contains("king") ||
                n.Contains("boss") || n.Contains("npc") || n.Contains("chest") ||
                n.Contains("weapon") || n.Contains("friendly") || go.CompareTag("Player"))
            {
                return true;
            }

            Transform p = go.transform.parent;
            while (p != null)
            {
                string pName = p.name.ToLower();
                if (pName.Contains("horse") || pName.Contains("player") || pName.Contains("king") ||
                    pName.Contains("boss") || pName.Contains("npc") || pName.Contains("chest") ||
                    pName.Contains("weapon") || pName.Contains("friendly") || p.CompareTag("Player"))
                {
                    return true;
                }
                p = p.parent;
            }

            return false;
        }
    }
}
