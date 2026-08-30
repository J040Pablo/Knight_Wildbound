using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Environment
{
    /// <summary>
    /// Handles the Forest Density Pass across all 6 forest sub-regions (Z: 60 to 620).
    /// Spawns trees, stumps, logs, bushes, ferns, emerging roots, mossy rocks, and organic clusters.
    /// Protects main paths, combat zones, and structure footprints from clutter.
    /// </summary>
    public class ForestEnvironmentSpawner : MonoBehaviour
    {
        private Transform parentTransform;
        private System.Func<float, float, float> heightSampler;
        private System.Func<float, float> pathOffsetSampler;

        private readonly List<(Vector3 pos, float radius)> spawnedPropBounds = new List<(Vector3, float)>();

        public void Initialize(
            Transform parent,
            System.Func<float, float, float> terrainHeightFunc,
            System.Func<float, float> pathXOffsetFunc)
        {
            parentTransform = parent;
            heightSampler = terrainHeightFunc;
            pathOffsetSampler = pathXOffsetFunc;
            spawnedPropBounds.Clear();
        }

        public void PopulateForestDensity()
        {
            if (parentTransform == null || heightSampler == null || pathOffsetSampler == null) return;

            // 1. Continuous Forest Density Pass (Z: 60 to 600)
            float startZ = 60f;
            float endZ = 600f;
            float stepZ = 8f;

            for (float z = startZ; z < endZ; z += stepZ)
            {
                float pathX = pathOffsetSampler(z);

                // Populate left wilderness (X: pathX - 110m to pathX - 14m)
                PopulateSideZone(z, pathX - 110f, pathX - 14f);

                // Populate right wilderness (X: pathX + 14m to pathX + 110m)
                PopulateSideZone(z, pathX + 14f, pathX + 110f);
            }

            // 2. Organic Environment Cluster Pass
            PopulateForestClusters();
        }

        private void PopulateSideZone(float z, float minX, float maxX)
        {
            int densityCount = Random.Range(3, 7);

            for (int i = 0; i < densityCount; i++)
            {
                float x = Random.Range(minX, maxX);
                Vector3 pos = new Vector3(x, 0, z + Random.Range(-3f, 3f));

                if (!ValidatePosition(pos, 1.2f)) continue;

                float noise = Mathf.PerlinNoise(x * 0.04f + 50f, z * 0.04f + 50f);
                float terrainY = heightSampler(x, pos.z);
                pos.y = terrainY;

                // Pick prop based on noise and random weighted distribution
                PlaceholderAssetKey key;
                float scale = Random.Range(0.85f, 1.35f);
                float radius = 1.4f;

                if (noise > 0.68f)
                {
                    key = (i % 3 == 0) ? PlaceholderAssetKey.TreeDeciduous : (i % 3 == 1 ? PlaceholderAssetKey.TreePine : PlaceholderAssetKey.TreeAncient);
                    radius = 3.2f;
                }
                else if (noise > 0.45f)
                {
                    key = (i % 4) switch
                    {
                        0 => PlaceholderAssetKey.MossStone,
                        1 => PlaceholderAssetKey.TreeStump,
                        2 => PlaceholderAssetKey.Fern,
                        _ => PlaceholderAssetKey.BushLarge
                    };
                    radius = 1.5f;
                }
                else if (noise > 0.25f)
                {
                    key = (i % 4) switch
                    {
                        0 => PlaceholderAssetKey.FallenLog,
                        1 => PlaceholderAssetKey.RootEmerging,
                        2 => PlaceholderAssetKey.TreeDeadSmall,
                        _ => PlaceholderAssetKey.RockMedium
                    };
                    radius = 1.2f;
                }
                else
                {
                    key = (i % 3 == 0) ? PlaceholderAssetKey.Fern : (i % 3 == 1 ? PlaceholderAssetKey.GrassClump : PlaceholderAssetKey.MushroomGroup);
                    radius = 0.8f;
                }

                GameObject obj = WorldPlaceholderFactory.Build(key, parentTransform, null, scale);
                if (obj != null)
                {
                    obj.transform.position = pos;
                    obj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                    RegisterProp(pos, radius);
                }
            }
        }

        private void PopulateForestClusters()
        {
            // Define organic cluster centers along the forest journey
            float[] clusterZs = { 90f, 140f, 185f, 230f, 290f, 335f, 395f, 440f, 495f, 545f, 585f };

            foreach (float z in clusterZs)
            {
                float pathX = pathOffsetSampler(z);

                // Cluster on Left
                Vector3 leftCenter = new Vector3(pathX - Random.Range(22f, 45f), 0, z + Random.Range(-6f, 6f));
                ClusterType leftType = (ClusterType)Random.Range(0, 5);
                EnvironmentClusterSpawner.SpawnCluster(leftType, leftCenter, 9f, parentTransform, heightSampler, ValidatePosition);

                // Cluster on Right
                Vector3 rightCenter = new Vector3(pathX + Random.Range(22f, 45f), 0, z + Random.Range(-6f, 6f));
                ClusterType rightType = (ClusterType)Random.Range(0, 5);
                EnvironmentClusterSpawner.SpawnCluster(rightType, rightCenter, 9f, parentTransform, heightSampler, ValidatePosition);
            }
        }

        public bool ValidatePosition(Vector3 pos, float radius)
        {
            // Protect main road corridor (10m clearance around pathX)
            float pathX = pathOffsetSampler(pos.z);
            if (Mathf.Abs(pos.x - pathX) < 10.5f) return false;

            // Protect Ruins Spawn Sanctuary (Z < 60)
            if (pos.z < 60f) return false;

            // Protect Boss Arena Center Floor (Z: 620 to 700, |X| < 35m)
            if (pos.z >= 620f && pos.z <= 700f && Mathf.Abs(pos.x) < 35f) return false;

            // Protection against overlap with existing props
            foreach (var b in spawnedPropBounds)
            {
                float sqrDist = (pos - b.pos).sqrMagnitude;
                float minReq = radius + b.radius;
                if (sqrDist < minReq * minReq) return false;
            }

            return true;
        }

        public void RegisterProp(Vector3 pos, float radius)
        {
            spawnedPropBounds.Add((pos, radius));
        }
    }
}
