using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Environment
{
    /// <summary>
    /// AAA Continuous Terrain Mesh Generator.
    /// Creates smooth, seamless, 3D heightmap meshes without rectangular slab gaps or sharp 90° edges.
    /// Vertex colors are procedurally applied based on slope, height, and path proximity:
    /// - Gentle grass (Green)
    /// - Dirt road / path (Warm Brown)
    /// - Rock cliffs (Slate Grey)
    /// - Shoreline / riverbed (Sandy Tan)
    /// </summary>
    public static class ContinuousTerrainGenerator
    {
        private static Shader terrainShader;

        public static GameObject CreateContinuousTerrainChunk(
            string name,
            Transform parent,
            Vector2 minXZ,
            Vector2 maxXZ,
            float gridStep,
            Func<float, float, float> heightFunc,
            Func<float, float> pathOffsetFunc)
        {
            GameObject chunk = new GameObject(name);
            chunk.transform.SetParent(parent, false);
            chunk.tag = "Ground";

            int numX = Mathf.RoundToInt((maxXZ.x - minXZ.x) / gridStep) + 1;
            int numZ = Mathf.RoundToInt((maxXZ.y - minXZ.y) / gridStep) + 1;

            Vector3[] vertices = new Vector3[numX * numZ];
            Vector3[] normals = new Vector3[numX * numZ];
            Vector2[] uvs = new Vector2[numX * numZ];
            Color[] colors = new Color[numX * numZ];
            int[] triangles = new int[(numX - 1) * (numZ - 1) * 6];

            // PEAK-Style Color Palette (Exact Specs from Prompt Section 2)
            Color mainGrass    = new Color(0.310f, 0.608f, 0.271f); // #4F9B45 (Rich Forest Green)
            Color darkGrass    = new Color(0.212f, 0.459f, 0.227f); // #36753A (Deep Shadow Green)
            Color lightGrass   = new Color(0.471f, 0.725f, 0.341f); // #78B957 (Fresh Meadow Green)

            Color dirtColor    = new Color(0.604f, 0.388f, 0.247f); // #9A633F (Warm Earthy Brown)
            Color defaultPath  = new Color(0.722f, 0.475f, 0.271f); // #B87945 (Warm Sand/Dirt Trail)

            Color darkRock     = new Color(0.349f, 0.388f, 0.416f); // #59636A (Dark Slate Rock)
            Color lightRock    = new Color(0.522f, 0.549f, 0.541f); // #858C8A (Light Stylized Stone)

            Color waterShallow = new Color(0.349f, 0.725f, 0.776f); // #59B9C6 (Bright Cyan Blue)
            Color waterDeep    = new Color(0.196f, 0.541f, 0.643f); // #328AA4 (Deep Stylized Blue)

            // 1. Generate Vertices and Colors
            for (int zIdx = 0; zIdx < numZ; zIdx++)
            {
                float worldZ = (float)System.Math.Round(minXZ.y + zIdx * gridStep, 3);
                float pathX = pathOffsetFunc != null ? pathOffsetFunc(worldZ) : 0f;

                for (int xIdx = 0; xIdx < numX; xIdx++)
                {
                    float worldX = (float)System.Math.Round(minXZ.x + xIdx * gridStep, 3);
                    float worldY = heightFunc(worldX, worldZ);
                    int vIdx = zIdx * numX + xIdx;

                    // Enforce flat tutorial sky (no terrain ceilings or spawn walls)
                    if (worldZ < 80f)
                    {
                        worldY = Mathf.Clamp(worldY, 0f, 1.5f);
                    }

                    vertices[vIdx] = new Vector3(worldX, worldY, worldZ);
                    uvs[vIdx] = new Vector2(worldX * 0.05f, worldZ * 0.05f);

                    // Compute slope using height differences
                    float hL = heightFunc(worldX - gridStep, worldZ);
                    float hR = heightFunc(worldX + gridStep, worldZ);
                    float hD = heightFunc(worldX, worldZ - gridStep);
                    float hU = heightFunc(worldX, worldZ + gridStep);

                    float dx = (hR - hL) / (2f * gridStep);
                    float dz = (hU - hD) / (2f * gridStep);
                    float slopeAngle = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;

                    float distToPath = Mathf.Abs(worldX - pathX);

                    Color pathColor = defaultPath;
                    Color rockColor = darkRock;
                    Color grassColor;

                    if (worldZ < 80f)
                    {
                        // 1. Ruins / Tutorial: Rich Green & Warm Stone
                        grassColor = mainGrass;
                    }
                    else if (worldZ < 160f)
                    {
                        // 2. Forest Entrance: Bright Meadow Green
                        grassColor = lightGrass;
                    }
                    else if (worldZ < 280f)
                    {
                        // 3. Deep Forest: Deeper Forest Green
                        grassColor = darkGrass;
                    }
                    else if (worldZ < 380f)
                    {
                        // 4. River & Lake Region: Lush Fresh Green
                        grassColor = lightGrass;
                    }
                    else if (worldZ < 480f)
                    {
                        // 5. Stone Valley: Slate Dark Rock with Green Patches
                        grassColor = mainGrass;
                    }
                    else if (worldZ < 580f)
                    {
                        // 6. Ancient Grove: Deeper Green
                        grassColor = darkGrass;
                    }
                    else
                    {
                        // 7. Boss Area: Corrupted Warm Red Tones
                        grassColor = new Color(0.48f, 0.20f, 0.18f);
                        pathColor = new Color(0.35f, 0.15f, 0.14f);
                    }

                    // Biome-Based Material & Texture Color Blending
                    if (worldZ < 65f && distToPath > 15f && worldY > 0.4f)
                    {
                        // Ruins Light Rock accent
                        colors[vIdx] = lightRock;
                    }
                    else if (worldY < -0.2f)
                    {
                        // Riverbed & Shoreline Sand
                        float sandBlend = Mathf.Clamp01(-worldY * 0.5f);
                        colors[vIdx] = Color.Lerp(dirtColor, waterShallow, sandBlend);
                    }
                    else if (slopeAngle > 28f)
                    {
                        // Steep Rock Cliffs (Dark Slate Rock #59636A)
                        float rockFactor = Mathf.Clamp01((slopeAngle - 28f) / 12f);
                        colors[vIdx] = Color.Lerp(grassColor, darkRock, rockFactor);
                    }
                    else if (distToPath < 7.0f)
                    {
                        // Main Dirt Path / Trail (Warm Dirt Trail #B87945)
                        float pathBlend = Mathf.Clamp01((7.0f - distToPath) / 7.0f);
                        colors[vIdx] = Color.Lerp(grassColor, pathColor, pathBlend);
                    }
                    else
                    {
                        // Open Grass Meadows with Subtle Color Variation
                        float noise = (Mathf.PerlinNoise(worldX * 0.08f, worldZ * 0.08f) - 0.5f) * 0.03f;
                        colors[vIdx] = new Color(
                            Mathf.Clamp01(grassColor.r + noise),
                            Mathf.Clamp01(grassColor.g + noise * 1.1f),
                            Mathf.Clamp01(grassColor.b + noise * 0.4f)
                        );
                    }
                }
            }

            // 2. Generate Triangles
            int triIdx = 0;
            for (int zIdx = 0; zIdx < numZ - 1; zIdx++)
            {
                for (int xIdx = 0; xIdx < numX - 1; xIdx++)
                {
                    int botLeft = zIdx * numX + xIdx;
                    int botRight = botLeft + 1;
                    int topLeft = (zIdx + 1) * numX + xIdx;
                    int topRight = topLeft + 1;

                    triangles[triIdx++] = botLeft;
                    triangles[triIdx++] = topLeft;
                    triangles[triIdx++] = botRight;

                    triangles[triIdx++] = botRight;
                    triangles[triIdx++] = topLeft;
                    triangles[triIdx++] = topRight;
                }
            }

            // 3. Generate Skirt Vertices (Solid 50m vertical side walls)
            List<Vector3> allVerts = new List<Vector3>(vertices);
            List<Color> allColors = new List<Color>(colors);
            List<Vector2> allUVs = new List<Vector2>(uvs);
            List<int> allTris = new List<int>(triangles);

            float bedrockY = -50.0f; // Fixed solid bedrock floor Y height across all chunks
            Color skirtColor = new Color(0.12f, 0.14f, 0.16f); // Dark bedrock rock wall

            Func<int, int, int> GetVIdx = (x, z) => z * numX + x;

            void AddSkirtEdge(int x1, int z1, int x2, int z2)
            {
                int top1 = GetVIdx(x1, z1);
                int top2 = GetVIdx(x2, z2);

                int bot1 = allVerts.Count;
                allVerts.Add(new Vector3(allVerts[top1].x, bedrockY, allVerts[top1].z));
                allColors.Add(skirtColor);
                allUVs.Add(allUVs[top1]);

                int bot2 = allVerts.Count;
                allVerts.Add(new Vector3(allVerts[top2].x, bedrockY, allVerts[top2].z));
                allColors.Add(skirtColor);
                allUVs.Add(allUVs[top2]);

                allTris.Add(top1);
                allTris.Add(top2);
                allTris.Add(bot1);

                allTris.Add(bot1);
                allTris.Add(top2);
                allTris.Add(bot2);
            }

            // Bottom Edge (Z = 0)
            for (int x = 0; x < numX - 1; x++) AddSkirtEdge(x, 0, x + 1, 0);
            // Top Edge (Z = numZ - 1)
            for (int x = 0; x < numX - 1; x++) AddSkirtEdge(x + 1, numZ - 1, x, numZ - 1);
            // Left Edge (X = 0)
            for (int z = 0; z < numZ - 1; z++) AddSkirtEdge(0, z + 1, 0, z);
            // Right Edge (X = numX - 1)
            for (int z = 0; z < numZ - 1; z++) AddSkirtEdge(numX - 1, z, numX - 1, z + 1);

            // 4. Solid Bedrock Bottom Cap Floor (-50m Y floor)
            int bCap1 = allVerts.Count;
            allVerts.Add(new Vector3(minXZ.x, bedrockY, minXZ.y));
            allColors.Add(skirtColor);
            allUVs.Add(Vector2.zero);

            int bCap2 = allVerts.Count;
            allVerts.Add(new Vector3(maxXZ.x, bedrockY, minXZ.y));
            allColors.Add(skirtColor);
            allUVs.Add(Vector2.zero);

            int bCap3 = allVerts.Count;
            allVerts.Add(new Vector3(minXZ.x, bedrockY, maxXZ.y));
            allColors.Add(skirtColor);
            allUVs.Add(Vector2.zero);

            int bCap4 = allVerts.Count;
            allVerts.Add(new Vector3(maxXZ.x, bedrockY, maxXZ.y));
            allColors.Add(skirtColor);
            allUVs.Add(Vector2.zero);

            allTris.Add(bCap1);
            allTris.Add(bCap3);
            allTris.Add(bCap2);

            allTris.Add(bCap2);
            allTris.Add(bCap3);
            allTris.Add(bCap4);

            Mesh mesh = new Mesh();
            mesh.name = $"{name}_Mesh";
            mesh.vertices = allVerts.ToArray();
            mesh.triangles = allTris.ToArray();
            mesh.colors = allColors.ToArray();
            mesh.uv = allUVs.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter mf = chunk.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
            Material mat = GetDefaultTerrainMaterial();
            mr.sharedMaterial = mat;

            MeshCollider mc = chunk.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            return chunk;
        }

        private static Material GetDefaultTerrainMaterial()
        {
            // 1. Detect active Render Pipeline
            var pipelineAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            string pipelineName = pipelineAsset != null ? pipelineAsset.GetType().Name : "Built-in Render Pipeline";
            Debug.Log($"[RENDER PIPELINE] Current active pipeline: {pipelineName}");

            // 2. Load standard Built-in 3D shader baseline (NO custom HLSL / URP shaders!)
            Shader shader = Shader.Find("Standard")
                         ?? Shader.Find("Mobile/Diffuse")
                         ?? Shader.Find("Legacy Shaders/Diffuse")
                         ?? Shader.Find("Unlit/Color");

            bool found = shader != null;
            Debug.Log($"[SHADER DEBUG]\nRequested: Built-in Standard\nFound: {found}\nShader name actually loaded: {(found ? shader.name : "null")}");

            if (shader == null)
            {
                Debug.LogError("[SHADER ERROR] No standard Built-in 3D terrain shader could be loaded!");
                return null;
            }

            Color richGreen = new Color(0.25f, 0.50f, 0.22f, 1.0f);
            Material mat = new Material(shader);
            mat.name = "ContinuousTerrainMaterial";
            mat.color = richGreen;

            // Enforce OPAQUE Geometry Queue (2000) & Enable ZWrite
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry; // 2000
            mat.SetOverrideTag("RenderType", "Opaque");

            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 1);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", richGreen);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", richGreen);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);

            Debug.Log($"[MATERIAL TRACE]\nTime: {Time.time:F2}s\nGameObject: Chunk Generation Template\nShader: {shader.name}\nColor: {richGreen}\nSource Script: ContinuousTerrainGenerator.GetDefaultTerrainMaterial");

            return mat;
        }
    }
}
