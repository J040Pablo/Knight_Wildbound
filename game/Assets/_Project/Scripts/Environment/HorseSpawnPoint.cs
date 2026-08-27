using UnityEngine;

namespace Roguelite.Environment
{
    public class HorseSpawnPoint : MonoBehaviour
    {
        [Header("Horse Spawn Settings")]
        [SerializeField] private float requiredClearanceRadius = 1.4f;
        [SerializeField] private string spawnPointLabel = "HorseSpawn";

        public float RequiredClearanceRadius => requiredClearanceRadius;
        public string SpawnPointLabel => spawnPointLabel;

        public Vector3 GetSpawnPosition()
        {
            return transform.position;
        }

        public Quaternion GetSpawnRotation()
        {
            return transform.rotation;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.5f, 1.0f, 0.8f); // Bright BLUE for horse spawn
            Vector3 pos = transform.position + Vector3.up * 1.0f;

            // Draw horse bounding box gizmo
            Gizmos.DrawWireCube(pos, new Vector3(1.2f, 2.0f, 2.4f));

            // Draw forward direction arrow
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(pos, transform.forward * 2.0f);
        }
    }
}
