using UnityEngine;
using Roguelite.Environment;
using Roguelite.Loot;

namespace Roguelite.Core.Managers
{
    public class ChestSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject chestPrefab;

        public GameObject SpawnChest(Vector3 position, ChestRarity rarity = ChestRarity.Common)
        {
            Vector3 validPos = GetValidPosition(position);
            GameObject chestGO = null;

            if (chestPrefab != null)
            {
                chestGO = Instantiate(chestPrefab, validPos, Quaternion.identity);
            }
            else
            {
                chestGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chestGO.name = "TreasureChest";
                chestGO.transform.position = validPos;
                chestGO.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);

                Renderer r = chestGO.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.85f, 0.65f, 0.2f);

                TreasureChest chestComp = chestGO.AddComponent<TreasureChest>();
                chestComp.chestRarity = rarity;
            }

            // Debug.Log($"[ChestSpawner] Spawned chest at {validPos}");
            return chestGO;
        }

        private Vector3 GetValidPosition(Vector3 preferredPos)
        {
            Vector3 rayStart = preferredPos + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 25f))
            {
                return hit.point + Vector3.up * 0.4f;
            }
            return preferredPos;
        }
    }
}
