using UnityEngine;
using Roguelite.Data;
using Roguelite.Enemy;

namespace Roguelite.Core.Managers
{
    public class BossSpawner : MonoBehaviour
    {
        public GameObject SpawnBoss(BossDefinition bossDef, Vector3 position)
        {
            if (bossDef == null)
            {
                Debug.LogWarning("[BossSpawner] Missing boss definition!");
                return null;
            }

            Vector3 validPos = GetValidPosition(position);
            GameObject bossGO = null;

            if (bossDef.bossPrefab != null)
            {
                bossGO = Instantiate(bossDef.bossPrefab, validPos, Quaternion.identity);
            }
            else
            {
                bossGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bossGO.name = bossDef.bossName;
                bossGO.transform.position = validPos;
                bossGO.transform.localScale = new Vector3(3f, 4f, 3f);

                Renderer r = bossGO.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.6f, 0.1f, 0.1f);

                HollowTreeBossAI bossAI = bossGO.AddComponent<HollowTreeBossAI>();
            }

            // Debug.Log($"[BossSpawner] Spawned boss '{bossDef.bossName}' at {validPos}");
            return bossGO;
        }

        private Vector3 GetValidPosition(Vector3 preferredPos)
        {
            Vector3 rayStart = preferredPos + Vector3.up * 15f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 30f))
            {
                return hit.point + Vector3.up * 0.1f;
            }
            return preferredPos;
        }
    }
}
