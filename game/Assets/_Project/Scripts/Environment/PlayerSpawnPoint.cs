using UnityEngine;

namespace Roguelite.Environment
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float requiredClearanceRadius = 0.8f;
        [SerializeField] private float groundOffset = 0.1f;
        [SerializeField] private string spawnPointLabel = "PlayerSpawn";

        public float RequiredClearanceRadius => requiredClearanceRadius;
        public string SpawnPointLabel => spawnPointLabel;

        public Vector3 GetSpawnPosition()
        {
            return transform.position + Vector3.up * groundOffset;
        }

        public Quaternion GetSpawnRotation()
        {
            return transform.rotation;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.1f, 0.9f, 0.2f, 0.8f); // Bright GREEN for valid player spawn
            Vector3 pos = GetSpawnPosition();

            // Draw player standing capsule gizmo
            Gizmos.DrawWireSphere(pos + Vector3.up * 0.5f, requiredClearanceRadius);
            Gizmos.DrawWireSphere(pos + Vector3.up * 1.5f, requiredClearanceRadius);
            Gizmos.DrawLine(pos + Vector3.up * 0.5f + Vector3.left * requiredClearanceRadius, pos + Vector3.up * 1.5f + Vector3.left * requiredClearanceRadius);
            Gizmos.DrawLine(pos + Vector3.up * 0.5f + Vector3.right * requiredClearanceRadius, pos + Vector3.up * 1.5f + Vector3.right * requiredClearanceRadius);

            // Draw forward direction arrow
            Gizmos.color = Color.green;
            Gizmos.DrawRay(pos + Vector3.up * 1.0f, transform.forward * 1.5f);
        }
    }
}
