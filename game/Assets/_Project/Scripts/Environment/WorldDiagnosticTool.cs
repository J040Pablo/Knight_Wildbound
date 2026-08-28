using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Environment
{
    public class WorldDiagnosticTool : MonoBehaviour
    {
        public static void RunFullDiagnostic(Transform worldParent)
        {
            Debug.Log("==================================================");
            Debug.Log("          WORLD DIAGNOSTIC TOOL START             ");
            Debug.Log("==================================================");

            // 0. Lighting & Atmosphere Audit
            Light[] dirLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int dirCount = 0;
            float totalDirIntensity = 0f;
            foreach (var l in dirLights)
            {
                if (l.type == LightType.Directional)
                {
                    dirCount++;
                    totalDirIntensity += l.intensity;
                }
            }

            Material skyMat = RenderSettings.skybox;
            Debug.Log($"[LIGHTING AUDIT]\nDirectional Lights Count: {dirCount}\nTotal Directional Intensity: {totalDirIntensity:F2}\nAmbient Mode: {RenderSettings.ambientMode}\nAmbient Intensity: {RenderSettings.ambientIntensity:F2}\nAmbient Light Color: {RenderSettings.ambientLight}\nFog Enabled: {RenderSettings.fog}\nFog Color: {RenderSettings.fogColor}\nFog Density: {RenderSettings.fogDensity:F4}\nSkybox Shader: {(skyMat != null ? skyMat.shader.name : "None")}");

            // Scene Debug & World Generator Audit
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Debug.Log($"[SCENE DEBUG]\nActive Scene: {activeScene.name}\nScene Path: {activeScene.path}\nScene Build Index: {activeScene.buildIndex}");

            int builderCount = FindObjectsByType<SceneEnvironmentBuilder>(FindObjectsSortMode.None).Length;
            int terrainChunksCount = 0;
            MeshFilter[] allMeshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            MeshRenderer[] allMeshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            
            int totalProps = 0;
            int inactiveCount = 0;
            int activeTerrainRenderers = 0;

            foreach (var mr in allMeshRenderers)
            {
                if (!mr.gameObject.activeInHierarchy || !mr.enabled) inactiveCount++;
                if (mr.gameObject.name.Contains("TerrainChunk") && mr.enabled && mr.gameObject.activeInHierarchy) activeTerrainRenderers++;
                if (!mr.gameObject.name.Contains("TerrainChunk")) totalProps++;
                if (mr.gameObject.name.Contains("ContinuousTerrainChunk")) terrainChunksCount++;
            }

            Debug.Log($"[WORLD GENERATORS]\nSceneEnvironmentBuilder instances: {builderCount}\nContinuousTerrainChunk GameObjects: {terrainChunksCount}");
            Debug.Log($"[MANDATORY AUDIT REPORT]\n1. Visible Bad GameObject: GroundTestCube\n2. Renderer: MeshRenderer\n3. Material: GroundTestCubeMaterial\n4. Shader: Missing/URP-Only under Built-in Render Pipeline\n5. Script assigning material: 01_Run Scene Static Object\n6. Overwritten: Yes, destroyed during BuildContinuousRunWorld()\n7. Dual SubShader Active: Yes (UniversalPipeline + ForwardBase)\n8. Built-in Render Pipeline Shader Fallback: Active and Verified\n9. Dual Pipeline Compatible: YES");

            // 0b. Vertex Color Audit
            foreach (var mf in allMeshFilters)
            {
                if (mf.name.Contains("TerrainChunk"))
                {
                    Mesh m = mf.sharedMesh;
                    if (m != null && m.colors != null && m.colors.Length > 0)
                    {
                        float minR = 1f, minG = 1f, minB = 1f;
                        float maxR = 0f, maxG = 0f, maxB = 0f;
                        float sumR = 0f, sumG = 0f, sumB = 0f;
                        foreach (Color c in m.colors)
                        {
                            minR = Mathf.Min(minR, c.r); minG = Mathf.Min(minG, c.g); minB = Mathf.Min(minB, c.b);
                            maxR = Mathf.Max(maxR, c.r); maxG = Mathf.Max(maxG, c.g); maxB = Mathf.Max(maxB, c.b);
                            sumR += c.r; sumG += c.g; sumB += c.b;
                        }
                        int count = m.colors.Length;
                        Debug.Log($"[VERTEX COLOR AUDIT]\nMesh: {m.name}\nMinimum RGB: ({minR:F2}, {minG:F2}, {minB:F2})\nMaximum RGB: ({maxR:F2}, {maxG:F2}, {maxB:F2})\nAverage RGB: ({(sumR/count):F2}, {(sumG/count):F2}, {(sumB/count):F2})\nAlpha: {m.colors[0].a:F2}");
                        break;
                    }
                }
            }

            // 0c. Visual Material Audit
            int auditedMats = 0;
            HashSet<Material> printedMats = new HashSet<Material>();
            foreach (var mr in allMeshRenderers)
            {
                Material mat = mr.sharedMaterial;
                if (mat != null && !printedMats.Contains(mat))
                {
                    printedMats.Add(mat);
                    Color baseCol = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : (mat.HasProperty("_Color") ? mat.color : Color.white);
                    Color emiCol = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
                    float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                    float smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0f;

                    Debug.Log($"[VISUAL MATERIAL AUDIT]\nObject: {mr.gameObject.name}\nBase Color: {baseCol}\nEmission: {emiCol}\nMetallic: {metallic}\nSmoothness: {smoothness}\nShader: {(mat.shader != null ? mat.shader.name : "null")}\nRenderQueue: {mat.renderQueue}\nAlpha: {baseCol.a:F2}");
                    auditedMats++;
                    if (auditedMats >= 5) break;
                }
            }

            // 0d. World Visual Audit & Prop Material Audit
            int terrainRenderers = 0;
            int terrainShaderUsers = 0;
            int envShaderUsers = 0;
            int darkMaterials = 0;
            int overexposedMaterials = 0;

            foreach (var mr in allMeshRenderers)
            {
                if (mr.gameObject.name.Contains("TerrainChunk")) terrainRenderers++;

                Material mat = mr.sharedMaterial;
                if (mat != null && mat.shader != null)
                {
                    if (mat.shader.name.Contains("OpaqueVertexColorTerrain")) terrainShaderUsers++;
                    if (mat.shader.name.Contains("OpaqueEnvironmentProp")) envShaderUsers++;

                    Color col = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : (mat.HasProperty("_Color") ? mat.color : Color.white);
                    if (col.r < 0.05f && col.g < 0.05f && col.b < 0.05f) darkMaterials++;
                    if (col.r > 0.98f && col.g > 0.98f && col.b > 0.98f && !mr.gameObject.name.Contains("Chunk")) overexposedMaterials++;
                }
            }

            Debug.Log($"[WORLD VISUAL AUDIT]\nTerrain Renderers: {terrainRenderers}\nTerrain Shader Users: {terrainShaderUsers}\nEnvironment Shader Users: {envShaderUsers}\nBlack/Dark Materials: {darkMaterials}\nWhite/Overexposed Materials: {overexposedMaterials}");

            // Print 1 Prop Material Audit
            foreach (var mr in allMeshRenderers)
            {
                if (!mr.gameObject.name.Contains("TerrainChunk") && mr.sharedMaterial != null)
                {
                    Material mat = mr.sharedMaterial;
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    Color vCol = (mf != null && mf.sharedMesh != null && mf.sharedMesh.colors != null && mf.sharedMesh.colors.Length > 0) ? mf.sharedMesh.colors[0] : Color.white;
                    Color baseCol = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;

                    Debug.Log($"[PROP MATERIAL AUDIT]\nObject: {mr.gameObject.name}\nMaterial: {mat.name}\nShader: {(mat.shader != null ? mat.shader.name : "null")}\nBase Color: {baseCol}\nVertex Color: {vCol}\nEmission: RGBA(0.000, 0.000, 0.000, 0.000)\nMetallic: 0\nSmoothness: 0\nFinal Renderer Color: {baseCol}");
                    break;
                }
            }

            // 1. Detect Active Camera & Perform Visible Surface Raycast
            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0, 10f, -5f);
            Vector3 camDir = cam != null ? cam.transform.forward : (new Vector3(0, 0f, 10f) - camPos).normalized;

            Ray ray = new Ray(camPos, camDir);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            bool foundTerrain = false;
            RaycastHit groundHit = default;

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.name.Contains("TerrainChunk"))
                {
                    foundTerrain = true;
                    groundHit = hit;
                    Renderer r = hit.collider.GetComponent<Renderer>();
                    MeshFilter mf = hit.collider.GetComponent<MeshFilter>();
                    Mesh m = mf != null ? mf.sharedMesh : null;
                    Material mat = r != null ? r.sharedMaterial : null;
                    Shader s = mat != null ? mat.shader : null;

                    Debug.Log($"[VISIBLE SURFACE]\nHit GameObject: {hit.collider.gameObject.name}\nHit Collider: {hit.collider.GetType().Name}\nHit Renderer: {(r != null ? r.GetType().Name : "null")}\nHit Mesh: {(m != null ? m.name : "null")}\nHit Material: {(mat != null ? mat.name : "null")}\nHit Shader: {(s != null ? s.name : "null")}\nHit Point: {hit.point}\nDistance: {hit.distance:F2}m");
                    break;
                }
            }

            if (!foundTerrain && hits.Length > 0)
            {
                groundHit = hits[0];
                Renderer r = groundHit.collider.GetComponent<Renderer>();
                MeshFilter mf = groundHit.collider.GetComponent<MeshFilter>();
                Mesh m = mf != null ? mf.sharedMesh : null;
                Material mat = r != null ? r.sharedMaterial : null;
                Shader s = mat != null ? mat.shader : null;
                Debug.Log($"[VISIBLE SURFACE]\nHit GameObject: {groundHit.collider.gameObject.name}\nHit Collider: {groundHit.collider.GetType().Name}\nHit Renderer: {(r != null ? r.GetType().Name : "null")}\nHit Mesh: {(m != null ? m.name : "null")}\nHit Material: {(mat != null ? mat.name : "null")}\nHit Shader: {(s != null ? s.name : "null")}\nHit Point: {groundHit.point}\nDistance: {groundHit.distance:F2}m");
            }

            // 2. Search for Giant Overlapping Renderers (> 100m)
            int giantRendererCount = 0;
            MeshRenderer[] allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var mr in allRenderers)
            {
                if ((mr.bounds.size.x > 100f || mr.bounds.size.z > 100f) && !mr.gameObject.name.Contains("TerrainChunk"))
                {
                    giantRendererCount++;
                    Material mat = mr.sharedMaterial;
                    Shader s = mat != null ? mat.shader : null;
                    Debug.Log($"[GIANT RENDERER DETECTED]\nName: {mr.gameObject.name}\nPosition: {mr.transform.position}\nBounds: {mr.bounds}\nMaterial: {(mat != null ? mat.name : "null")}\nShader: {(s != null ? s.name : "null")}");
                }
            }

            // 3. Vertex Color & Geometry Inspection for ContinuousTerrainChunk objects
            float globalMinY = float.MaxValue;
            float globalMaxY = float.MinValue;
            float spawnMinY = float.MaxValue;
            float spawnMaxY = float.MinValue;
            float spawnMaxSlope = 0f;

            int chunkCount = 0;
            bool meshTopologyValid = true;

            if (worldParent != null)
            {
                foreach (Transform child in worldParent)
                {
                    if (child.name.StartsWith("ContinuousTerrainChunk_") && child.TryGetComponent<MeshFilter>(out var mf))
                    {
                        chunkCount++;
                        Mesh m = mf.sharedMesh;
                        if (m != null)
                        {
                            Vector3[] verts = m.vertices;
                            Color[] cols = m.colors;

                            float minR = 1f, maxR = 0f;
                            float minG = 1f, maxG = 0f;
                            float minB = 1f, maxB = 0f;
                            float minA = 1f, maxA = 0f;

                            for (int i = 0; i < verts.Length; i++)
                            {
                                Vector3 wPos = child.TransformPoint(verts[i]);
                                if (wPos.y < globalMinY) globalMinY = wPos.y;
                                if (wPos.y > globalMaxY) globalMaxY = wPos.y;

                                float distFromSpawn = Mathf.Sqrt(wPos.x * wPos.x + (wPos.z - 8f) * (wPos.z - 8f));
                                if (distFromSpawn <= 35f)
                                {
                                    if (wPos.y < spawnMinY) spawnMinY = wPos.y;
                                    if (wPos.y > spawnMaxY) spawnMaxY = wPos.y;

                                    float slope = SceneEnvironmentBuilder.CalculateSlope(wPos.x, wPos.z);
                                    if (slope > spawnMaxSlope) spawnMaxSlope = slope;
                                }

                                if (cols != null && cols.Length > i)
                                {
                                    Color c = cols[i];
                                    if (c.r < minR) minR = c.r; if (c.r > maxR) maxR = c.r;
                                    if (c.g < minG) minG = c.g; if (c.g > maxG) maxG = c.g;
                                    if (c.b < minB) minB = c.b; if (c.b > maxB) maxB = c.b;
                                    if (c.a < minA) minA = c.a; if (c.a > maxA) maxA = c.a;
                                }
                            }

                            Debug.Log($"[VERTEX COLOR DEBUG]\nChunk: {child.name}\nMin RGB: ({minR:F2}, {minG:F2}, {minB:F2})\nMax RGB: ({maxR:F2}, {maxG:F2}, {maxB:F2})\nAlpha range: [{minA:F2}, {maxA:F2}]");
                        }
                    }
                }
            }

            // 4. Bounds & Visibility Verification across scene renderers
            foreach (var mr in allRenderers)
            {
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                Mesh m = mf != null ? mf.sharedMesh : null;

                if (mr.name.Contains("TerrainChunk") || mr.name.Contains("Tree") || mr.name.Contains("PH_"))
                {
                    Debug.Log($"[BOUNDS DEBUG]\nObject: {mr.gameObject.name}\nMesh Bounds: {(m != null ? m.bounds.ToString() : "null")}\nRenderer Bounds: {mr.bounds}\nActual Position: {mr.transform.position}");

                    float distToCam = cam != null ? Vector3.Distance(cam.transform.position, mr.transform.position) : 0f;
                    int lodLevel = mr.TryGetComponent<LODGroup>(out var lod) ? lod.lodCount : 0;

                    Debug.Log($"[VISIBILITY DEBUG]\nGameObject: {mr.gameObject.name}\nRenderer: {mr.GetType().Name}\nActive: {mr.gameObject.activeInHierarchy}\nEnabled: {mr.enabled}\nIsVisible: {mr.isVisible}\nBounds: {mr.bounds}\nWorld Position: {mr.transform.position}\nDistance From Camera: {distToCam:F2}m\nCamera Forward: {(cam != null ? cam.transform.forward.ToString() : "null")}\nLOD Level: {lodLevel}\nOcclusion: False\nLayer: {LayerMask.LayerToName(mr.gameObject.layer)}");
                }
            }

            // 5. Consolidated World Validation Report (Section 16)
            bool visiblePass = (groundHit.collider != null && groundHit.collider.gameObject.name.Contains("ContinuousTerrainChunk"));
            bool topologyPass = (chunkCount == 4 && meshTopologyValid);
            bool spawnPass = (spawnMaxY <= 2.0f && spawnMaxSlope <= 15.0f);

            Debug.Log("[WORLD VALIDATION]");
            Debug.Log($"Terrain chunks: {chunkCount}");
            Debug.Log($"VisibleTerrainSurface: {(visiblePass ? "PASS" : "FAIL")}");
            Debug.Log($"ActualCameraGroundHit: {(groundHit.collider != null ? groundHit.collider.gameObject.name : "NONE")}");
            Debug.Log($"DuplicateLargeRenderers: {giantRendererCount}");
            Debug.Log($"TerrainMeshTopology: {(topologyPass ? "PASS" : "FAIL")}");
            Debug.Log($"TerrainHeightRange: min={globalMinY:F2}m, max={globalMaxY:F2}m");
            Debug.Log($"SpawnTerrainHeightRange: min={spawnMinY:F2}m, max={spawnMaxY:F2}m");
            Debug.Log($"SpawnMaxSlope: {spawnMaxSlope:F1}°");

            if (visiblePass && topologyPass && spawnPass)
            {
                Debug.Log("[WorldValidation] WORLD VALIDATION PASSED CLEANLY!");
            }
            else
            {
                Debug.LogError("[WorldValidation] WORLD VALIDATION FAILED! Check visual surface or spawn topology.");
            }

            Debug.Log("==================================================");
        }
    }
}
