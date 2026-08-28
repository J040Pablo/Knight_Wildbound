using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Roguelite.Environment;
using Roguelite.Player;

namespace Roguelite.Core
{
    public class PlayerSpawnManager : MonoBehaviour
    {
        public static PlayerSpawnManager Instance { get; private set; }

        [Header("Debug Visualization")]
        [SerializeField] private bool enableSpawnDebugGizmos = true;

        private List<Vector3> debugInvalidTestPoints = new List<Vector3>();
        private Vector3 debugLastValidPlayerSpawn = Vector3.zero;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool SpawnPlayer(GameObject player)
        {
            if (player == null)
            {
                Debug.LogError("[PlayerSpawnManager] Cannot spawn player: player GameObject is null!");
                return false;
            }

            debugInvalidTestPoints.Clear();

            // Ensure PlayerSpawnTracker is attached for logging
            PlayerSpawnTracker tracker = player.GetComponent<PlayerSpawnTracker>();
            if (tracker == null)
            {
                tracker = player.AddComponent<PlayerSpawnTracker>();
            }

            // CRITICAL: Force PhysX spatial trees (BVH) to sync immediately after procedural environment creation
            Physics.SyncTransforms();

            // 1. Temporarily disable CharacterController & Colliders on player during position calculation
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
            List<Collider> disabledColliders = new List<Collider>();
            foreach (var c in playerColliders)
            {
                if (c.enabled)
                {
                    c.enabled = false;
                    disabledColliders.Add(c);
                }
            }

            // 2. Locate scene PlayerSpawnPoint node
            PlayerSpawnPoint spawnPointNode = Object.FindFirstObjectByType<PlayerSpawnPoint>();

            Vector3 targetPosition = Vector3.zero;
            Quaternion targetRotation = Quaternion.identity;
            float clearanceRadius = 0.5f;

            if (spawnPointNode != null)
            {
                targetPosition = spawnPointNode.GetSpawnPosition();
                targetRotation = spawnPointNode.GetSpawnRotation();
                clearanceRadius = spawnPointNode.RequiredClearanceRadius;
            }
            else
            {
                Debug.LogWarning("[PlayerSpawnManager] No PlayerSpawnPoint component found in scene. Using default ground search.");
                targetPosition = new Vector3(0, 0.5f, 8.0f);
            }

            if (tracker != null) tracker.LogInitial(targetPosition);

            // 3. Position validation & spiral search fallback
            if (!ValidatePlayerPosition(targetPosition, clearanceRadius, out Vector3 validGroundPos))
            {
                debugInvalidTestPoints.Add(targetPosition);
                Debug.LogWarning($"[PlayerSpawnManager] Primary spawn point at {targetPosition} failed validation. Searching nearby spiral positions...");

                if (TryFindNearestValidPosition(targetPosition, clearanceRadius, out validGroundPos))
                {
                    Debug.Log($"[PlayerSpawnManager] Found fallback spawn position at {validGroundPos}");
                }
                else
                {
                    Debug.LogError($"[PlayerSpawnManager] Critical: Could not find valid clear ground around {targetPosition}. Forcing ground floor snapping.");
                    if (TryGetGroundSurfaceHit(targetPosition, out RaycastHit fallbackHit))
                    {
                        validGroundPos = fallbackHit.point + Vector3.up * 0.1f;
                    }
                    else
                    {
                        validGroundPos = new Vector3(0, 0.1f, 8.0f);
                    }
                }
            }

            debugLastValidPlayerSpawn = validGroundPos;

            // 4. Update position & rotation
            player.transform.position = validGroundPos;
            player.transform.rotation = targetRotation;

            if (tracker != null) tracker.LogAfterSpawnManager(player.transform.position);

            // Force PhysX transform sync after repositioning
            Physics.SyncTransforms();

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.ResetVelocity();

            // Re-enable player colliders & CharacterController
            foreach (var c in disabledColliders)
            {
                if (c != null) c.enabled = true;
            }

            Vector3 posBeforeCCEnable = player.transform.position;
            if (cc != null) cc.enabled = true;
            Vector3 posAfterCCEnable = player.transform.position;

            if (tracker != null) tracker.LogCCDetails(cc, posBeforeCCEnable, posAfterCCEnable);

            Debug.Log($"[PlayerSpawnManager] Player successfully spawned at ({validGroundPos.x:F2}, {validGroundPos.y:F2}, {validGroundPos.z:F2}) in scene '{SceneManager.GetActiveScene().name}'");
            return true;
        }

        public bool ValidatePlayerPosition(Vector3 testPos, float clearanceRadius, out Vector3 validGroundPos)
        {
            validGroundPos = testPos;

            // Use RaycastAll to find floor ground surface (Y ≈ 0.0), ignoring high archways / ceilings
            if (TryGetGroundSurfaceHit(testPos, out RaycastHit groundHit))
            {
                validGroundPos = groundHit.point + Vector3.up * 0.1f;
            }
            else
            {
                return false; // No ground floor collider beneath test position
            }

            // WorldBoundary inclusion check
            WorldBoundary boundary = Object.FindFirstObjectByType<WorldBoundary>();
            if (boundary != null)
            {
                if (!boundary.IsPositionInsideBoundary(validGroundPos, 0.5f))
                {
                    return false; // Outside explicit world boundary limits
                }
            }

            // Standing Capsule Clearance Check (ignoring trigger volumes and ground colliders)
            Vector3 capsuleBottom = validGroundPos + Vector3.up * 0.4f;
            Vector3 capsuleTop = validGroundPos + Vector3.up * 1.6f;

            Collider[] overlaps = Physics.OverlapCapsule(capsuleBottom, capsuleTop, clearanceRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var col in overlaps)
            {
                if (col.isTrigger) continue;

                bool isGround = col.CompareTag("Ground") ||
                                col.gameObject.name.EndsWith("Ground") ||
                                col.gameObject.name == "Terrain" ||
                                col.gameObject.name == "Ground";

                if (!isGround)
                {
                    // Ignore self character controller or player capsule
                    if (col.GetComponent<PlayerController>() != null || col.GetComponent<CharacterController>() != null) continue;

                    // Solid obstacle or wall overlap detected
                    return false;
                }
            }

            return true;
        }

        private bool TryGetGroundSurfaceHit(Vector3 position, out RaycastHit groundHit)
        {
            groundHit = default;
            Vector3 rayStart = new Vector3(position.x, position.y + 25.0f, position.z);

            RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 50.0f, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return false;

            RaycastHit bestHit = default;
            float bestYDiff = float.MaxValue;
            bool found = false;

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.point.y < -15.0f) continue; // Skip void pit

                bool isGround = hit.collider.CompareTag("Ground") ||
                                hit.collider.gameObject.name.EndsWith("Ground") ||
                                hit.collider.gameObject.name == "Terrain" ||
                                hit.collider.gameObject.name == "Ground" ||
                                hit.normal.y > 0.6f;

                if (!isGround) continue;

                // Reject elevated archways, pillars, or ceilings (in Ruins area Z < 35, Y must be <= 3.0f)
                if (position.z < 35.0f && hit.point.y > 3.0f) continue;

                // Find ground hit closest to ground level (Y = 0)
                float yDiff = Mathf.Abs(hit.point.y - 0.0f);
                if (yDiff < bestYDiff)
                {
                    bestYDiff = yDiff;
                    bestHit = hit;
                    found = true;
                }
            }

            if (found)
            {
                groundHit = bestHit;
                return true;
            }
            return false;
        }

        private bool TryFindNearestValidPosition(Vector3 centerPos, float clearanceRadius, out Vector3 validGroundPos)
        {
            float[] searchRadii = new float[] { 1.0f, 2.5f, 4.0f, 6.0f };
            int pointsPerRadius = 8;

            foreach (float r in searchRadii)
            {
                for (int i = 0; i < pointsPerRadius; i++)
                {
                    float angle = (i / (float)pointsPerRadius) * Mathf.PI * 2f;
                    Vector3 candidate = centerPos + new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);

                    if (ValidatePlayerPosition(candidate, clearanceRadius, out validGroundPos))
                    {
                        return true;
                    }

                    debugInvalidTestPoints.Add(candidate);
                }
            }

            validGroundPos = centerPos;
            return false;
        }

        private void OnDrawGizmos()
        {
            if (!enableSpawnDebugGizmos) return;

            Gizmos.color = new Color(1.0f, 0.2f, 0.2f, 0.7f);
            foreach (var pt in debugInvalidTestPoints)
            {
                Gizmos.DrawWireSphere(pt, 0.4f);
            }

            if (debugLastValidPlayerSpawn != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(debugLastValidPlayerSpawn + Vector3.up * 0.2f, 0.5f);
            }
        }
    }
}
