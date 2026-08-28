using UnityEngine;
using Roguelite.Core;

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
            // Raycast downward from +10m height above marker to snap precisely to ground
            Vector3 castOrigin = transform.position + Vector3.up * 10.0f;
            if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, 25.0f))
            {
                return hit.point + Vector3.up * groundOffset;
            }

            return transform.position + Vector3.up * groundOffset;
        }

        public Quaternion GetSpawnRotation()
        {
            return transform.rotation;
        }

        public bool ValidateSpawnPoint(out string message)
        {
            Vector3 spawnPos = GetSpawnPosition();

            if (PlayerSpawnManager.Instance != null)
            {
                if (PlayerSpawnManager.Instance.ValidatePlayerPosition(spawnPos, requiredClearanceRadius, out Vector3 validPos))
                {
                    message = $"Spawn point '{spawnPointLabel}' at {validPos} is valid and clear.";
                    return true;
                }
            }

            message = $"Spawn point '{spawnPointLabel}' at {spawnPos} failed position validation!";
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.1f, 0.9f, 0.2f, 0.8f); // Bright GREEN for valid player spawn node
            Vector3 pos = GetSpawnPosition();

            // Draw standing player capsule gizmo
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
