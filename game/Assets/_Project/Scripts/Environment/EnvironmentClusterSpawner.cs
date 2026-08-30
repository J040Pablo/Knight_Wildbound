using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Environment
{
    public enum ClusterType
    {
        DenseTreeGrove,
        MossyRockOutcrop,
        ShrubThicket,
        RootTimberTangle,
        FoliageGlade
    }

    public static class EnvironmentClusterSpawner
    {
        public static List<GameObject> SpawnCluster(
            ClusterType type,
            Vector3 center,
            float radius,
            Transform parent,
            System.Func<float, float, float> getTerrainHeight,
            System.Func<Vector3, float, bool> validatePosition)
        {
            List<GameObject> spawned = new List<GameObject>();
            int elementCount = Random.Range(6, 14);

            switch (type)
            {
                case ClusterType.DenseTreeGrove:
                    SpawnTreeGrove(center, radius, elementCount, parent, getTerrainHeight, validatePosition, spawned);
                    break;
                case ClusterType.MossyRockOutcrop:
                    SpawnRockOutcrop(center, radius, elementCount, parent, getTerrainHeight, validatePosition, spawned);
                    break;
                case ClusterType.ShrubThicket:
                    SpawnShrubThicket(center, radius, elementCount, parent, getTerrainHeight, validatePosition, spawned);
                    break;
                case ClusterType.RootTimberTangle:
                    SpawnRootTimberTangle(center, radius, elementCount, parent, getTerrainHeight, validatePosition, spawned);
                    break;
                case ClusterType.FoliageGlade:
                    SpawnFoliageGlade(center, radius, elementCount, parent, getTerrainHeight, validatePosition, spawned);
                    break;
            }

            return spawned;
        }

        private static void SpawnTreeGrove(
            Vector3 center, float radius, int count, Transform parent,
            System.Func<float, float, float> getTerrainHeight,
            System.Func<Vector3, float, bool> validatePosition,
            List<GameObject> spawned)
        {
            // Center hero tree / large tree
            TrySpawnProp(PlaceholderAssetKey.TreeDeciduous, center, 1.4f, parent, getTerrainHeight, validatePosition, spawned, 3.5f);

            for (int i = 0; i < count; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 pos = center + new Vector3(circle.x, 0, circle.y);

                PlaceholderAssetKey key = (i % 4) switch
                {
                    0 => PlaceholderAssetKey.TreePine,
                    1 => PlaceholderAssetKey.TreeDeciduous,
                    2 => PlaceholderAssetKey.TreeStump,
                    _ => PlaceholderAssetKey.FallenLog
                };

                float scale = (key == PlaceholderAssetKey.FallenLog || key == PlaceholderAssetKey.TreeStump) ? 1.0f : Random.Range(0.85f, 1.35f);
                float minClearance = (key == PlaceholderAssetKey.TreePine || key == PlaceholderAssetKey.TreeDeciduous) ? 3.0f : 1.2f;

                TrySpawnProp(key, pos, scale, parent, getTerrainHeight, validatePosition, spawned, minClearance);

                // Add companion ground detail near tree
                if (Random.value < 0.6f)
                {
                    Vector3 detailPos = pos + new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
                    PlaceholderAssetKey detailKey = Random.value < 0.5f ? PlaceholderAssetKey.Fern : PlaceholderAssetKey.MossStone;
                    TrySpawnProp(detailKey, detailPos, Random.Range(0.8f, 1.2f), parent, getTerrainHeight, validatePosition, spawned, 0.8f);
                }
            }
        }

        private static void SpawnRockOutcrop(
            Vector3 center, float radius, int count, Transform parent,
            System.Func<float, float, float> getTerrainHeight,
            System.Func<Vector3, float, bool> validatePosition,
            List<GameObject> spawned)
        {
            // Core boulder
            TrySpawnProp(PlaceholderAssetKey.RockBoulder, center, 1.5f, parent, getTerrainHeight, validatePosition, spawned, 2.5f);

            for (int i = 0; i < count; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 pos = center + new Vector3(circle.x, 0, circle.y);

                PlaceholderAssetKey key = (i % 4) switch
                {
                    0 => PlaceholderAssetKey.MossStone,
                    1 => PlaceholderAssetKey.RockMedium,
                    2 => PlaceholderAssetKey.RockPebble,
                    _ => PlaceholderAssetKey.RockShelf
                };

                float scale = Random.Range(0.8f, 1.4f);
                TrySpawnProp(key, pos, scale, parent, getTerrainHeight, validatePosition, spawned, 1.0f);

                if (Random.value < 0.4f)
                {
                    Vector3 fernPos = pos + new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f));
                    TrySpawnProp(PlaceholderAssetKey.Fern, fernPos, Random.Range(0.8f, 1.2f), parent, getTerrainHeight, validatePosition, spawned, 0.6f);
                }
            }
        }

        private static void SpawnShrubThicket(
            Vector3 center, float radius, int count, Transform parent,
            System.Func<float, float, float> getTerrainHeight,
            System.Func<Vector3, float, bool> validatePosition,
            List<GameObject> spawned)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 pos = center + new Vector3(circle.x, 0, circle.y);

                PlaceholderAssetKey key = (i % 4) switch
                {
                    0 => PlaceholderAssetKey.BushLarge,
                    1 => PlaceholderAssetKey.BushSmall,
                    2 => PlaceholderAssetKey.Fern,
                    _ => PlaceholderAssetKey.FlowerCluster
                };

                float scale = Random.Range(0.9f, 1.3f);
                TrySpawnProp(key, pos, scale, parent, getTerrainHeight, validatePosition, spawned, 0.8f);
            }
        }

        private static void SpawnRootTimberTangle(
            Vector3 center, float radius, int count, Transform parent,
            System.Func<float, float, float> getTerrainHeight,
            System.Func<Vector3, float, bool> validatePosition,
            List<GameObject> spawned)
        {
            TrySpawnProp(PlaceholderAssetKey.TreeDeadGiant, center, 1.3f, parent, getTerrainHeight, validatePosition, spawned, 3.0f);

            for (int i = 0; i < count; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 pos = center + new Vector3(circle.x, 0, circle.y);

                PlaceholderAssetKey key = (i % 4) switch
                {
                    0 => PlaceholderAssetKey.RootEmerging,
                    1 => PlaceholderAssetKey.FallenLog,
                    2 => PlaceholderAssetKey.TreeDeadSmall,
                    _ => PlaceholderAssetKey.MushroomGroup
                };

                float scale = Random.Range(0.8f, 1.25f);
                TrySpawnProp(key, pos, scale, parent, getTerrainHeight, validatePosition, spawned, 1.0f);
            }
        }

        private static void SpawnFoliageGlade(
            Vector3 center, float radius, int count, Transform parent,
            System.Func<float, float, float> getTerrainHeight,
            System.Func<Vector3, float, bool> validatePosition,
            List<GameObject> spawned)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 pos = center + new Vector3(circle.x, 0, circle.y);

                PlaceholderAssetKey key = (i % 4) switch
                {
                    0 => PlaceholderAssetKey.GrassClump,
                    1 => PlaceholderAssetKey.Fern,
                    2 => PlaceholderAssetKey.FlowerCluster,
                    _ => PlaceholderAssetKey.MushroomGroup
                };

                float scale = Random.Range(0.8f, 1.4f);
                TrySpawnProp(key, pos, scale, parent, getTerrainHeight, validatePosition, spawned, 0.6f);
            }
        }

        private static bool TrySpawnProp(
            PlaceholderAssetKey key, Vector3 worldPos, float scale, Transform parent,
            System.Func<float, float, float> getTerrainHeight,
            System.Func<Vector3, float, bool> validatePosition,
            List<GameObject> spawned, float minClearance)
        {
            float terrainY = getTerrainHeight(worldPos.x, worldPos.z);
            Vector3 pos = new Vector3(worldPos.x, terrainY, worldPos.z);

            if (validatePosition != null && !validatePosition(pos, minClearance)) return false;

            Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            rot.Normalize();
            GameObject obj = WorldPlaceholderFactory.Build(key, parent, null, scale);
            if (obj != null)
            {
                obj.transform.position = pos;
                obj.transform.rotation = rot;
                spawned.Add(obj);
                return true;
            }
            return false;
        }
    }
}
