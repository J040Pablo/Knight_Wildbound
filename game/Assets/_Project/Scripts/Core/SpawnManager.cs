using System.Collections.Generic;
using UnityEngine;
using Roguelite.Environment;
using Roguelite.Player;

namespace Roguelite.Core
{
    public class SpawnManager : MonoBehaviour
    {
        public static SpawnManager Instance { get; private set; }

        [Header("Debug Visualization")]
        [SerializeField] private bool enableSpawnDebugGizmos = true;

        private List<Vector3> debugInvalidTestPoints = new List<Vector3>();
        private Vector3 debugLastValidPlayerSpawn = Vector3.zero;

        [Header("Sub Spawners")]
        [SerializeField] private Roguelite.Core.Managers.EnemySpawner enemySpawner;
        [SerializeField] private Roguelite.Core.Managers.EventSpawner eventSpawner;
        [SerializeField] private Roguelite.Core.Managers.ChestSpawner chestSpawner;
        [SerializeField] private Roguelite.Core.Managers.BossSpawner bossSpawner;

        public Roguelite.Core.Managers.EnemySpawner EnemySpawner => GetSubSpawner(ref enemySpawner);
        public Roguelite.Core.Managers.EventSpawner EventSpawner => GetSubSpawner(ref eventSpawner);
        public Roguelite.Core.Managers.ChestSpawner ChestSpawner => GetSubSpawner(ref chestSpawner);
        public Roguelite.Core.Managers.BossSpawner BossSpawner => GetSubSpawner(ref bossSpawner);

        private T GetSubSpawner<T>(ref T field) where T : Component
        {
            if (field == null)
            {
                field = GetComponentInChildren<T>();
                if (field == null)
                {
                    GameObject child = new GameObject(typeof(T).Name);
                    child.transform.SetParent(transform);
                    field = child.AddComponent<T>();
                }
            }
            return field;
        }

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
            if (player == null) return false;

            debugInvalidTestPoints.Clear();
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
                Debug.LogWarning("[SpawnManager] No PlayerSpawnPoint component found in scene. Using ground center search.");
                targetPosition = new Vector3(0, 1.0f, 0);
            }

            // Perform position validation & spiral search fallback if needed
            if (!ValidateSpawnPosition(targetPosition, clearanceRadius, out Vector3 validGroundPos))
            {
                debugInvalidTestPoints.Add(targetPosition);
                Debug.LogWarning($"[SpawnManager] Primary spawn point at {targetPosition} failed validation. Searching nearby spiral positions...");

                if (TryFindNearestValidPosition(targetPosition, clearanceRadius, out validGroundPos))
                {
                    Debug.Log($"[SpawnManager] Successfully found fallback spawn position at {validGroundPos}");
                }
                else
                {
                    Debug.LogError($"[SpawnManager] Critical: Could not find valid ground position around {targetPosition}. Forcing position.");
                    validGroundPos = targetPosition;
                }
            }

            debugLastValidPlayerSpawn = validGroundPos;

            // Safely update player position without CharacterController conflicts
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = validGroundPos;
            player.transform.rotation = targetRotation;

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.ResetVelocity();

            if (cc != null) cc.enabled = true;

            Debug.Log($"[SpawnManager] Player successfully spawned at {validGroundPos} in scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'");
            return true;
        }

        public bool SpawnHorse(GameObject horse)
        {
            if (horse == null) return false;

            HorseSpawnPoint horseSpawnNode = Object.FindFirstObjectByType<HorseSpawnPoint>();
            Vector3 targetPosition = new Vector3(-6f, 0.5f, 55f);
            Quaternion targetRotation = Quaternion.identity;
            float clearanceRadius = 1.2f;

            if (horseSpawnNode != null)
            {
                targetPosition = horseSpawnNode.GetSpawnPosition();
                targetRotation = horseSpawnNode.GetSpawnRotation();
                clearanceRadius = horseSpawnNode.RequiredClearanceRadius;
            }

            if (!ValidateSpawnPosition(targetPosition, clearanceRadius, out Vector3 validGroundPos))
            {
                if (!TryFindNearestValidPosition(targetPosition, clearanceRadius, out validGroundPos))
                {
                    validGroundPos = targetPosition;
                }
            }

            CharacterController cc = horse.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            horse.transform.position = validGroundPos;
            horse.transform.rotation = targetRotation;

            if (cc != null) cc.enabled = true;

            Debug.Log($"[SpawnManager] Companion Horse successfully spawned at {validGroundPos}");
            return true;
        }

        public bool ValidateSpawnPosition(Vector3 testPos, float clearanceRadius, out Vector3 validGroundPos)
        {
            validGroundPos = testPos;

            // 1. Ground Raycast downward from 10m height
            Vector3 rayStart = new Vector3(testPos.x, testPos.y + 10.0f, testPos.z);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, 25.0f))
            {
                if (groundHit.point.y < -15.0f) return false; // Below world pit
                validGroundPos = groundHit.point + Vector3.up * 0.1f;
            }
            else
            {
                return false; // No ground collider beneath test position
            }

            // 2. WorldBoundary inclusion check
            WorldBoundary boundary = Object.FindFirstObjectByType<WorldBoundary>();
            if (boundary != null)
            {
                if (!boundary.IsPositionInsideBoundary(validGroundPos))
                {
                    return false; // Outside explicit world boundary
                }
            }

            // 3. Physics Capsule Collision check (check standing space)
            Vector3 capsuleBottom = validGroundPos + Vector3.up * 0.4f;
            Vector3 capsuleTop = validGroundPos + Vector3.up * 1.6f;

            Collider[] overlaps = Physics.OverlapCapsule(capsuleBottom, capsuleTop, clearanceRadius);
            foreach (var col in overlaps)
            {
                bool isGround = col.CompareTag("Ground") || col.gameObject.name.EndsWith("Ground") || col.gameObject.name == "Terrain" || col.gameObject.name == "Ground";
                if (!col.isTrigger && !isGround)
                {
                    if (col.GetComponent<CharacterController>() != null) continue;
                    return false; // Obstacle or wall overlap found
                }
            }

            return true;
        }

        public bool ValidateEnemySpawnPosition(Vector3 testPos, float clearanceRadius, Vector3 playerPos, List<Vector3> existingEnemyPositions, out Vector3 validGroundPos, float minPlayerDist = 50.0f, float minEnemyDist = 2.0f)
        {
            validGroundPos = testPos;

            // 1. Basic Ground & Boundary & Solid Obstacle Validation
            if (!ValidateSpawnPosition(testPos, clearanceRadius, out validGroundPos))
            {
                return false;
            }

            // 2. Distance check from Player (minimum 50 meters safe sanctuary radius)
            if (playerPos != Vector3.zero && Vector3.Distance(validGroundPos, playerPos) < minPlayerDist)
            {
                return false;
            }

            // 3. Distance check between enemies (minimum 2 meters anti-stacking check)
            if (existingEnemyPositions != null)
            {
                foreach (var enemyPos in existingEnemyPositions)
                {
                    if (Vector3.Distance(validGroundPos, enemyPos) < minEnemyDist)
                    {
                        return false; // Too close to another enemy
                    }
                }
            }

            return true;
        }

        public Vector3 GetValidEnemySpawnPosition(Vector3 preferredPos, Vector3 playerPos, List<Vector3> existingEnemyPositions, float clearanceRadius = 0.8f)
        {
            if (ValidateEnemySpawnPosition(preferredPos, clearanceRadius, playerPos, existingEnemyPositions, out Vector3 validPos))
            {
                return validPos;
            }

            // Spiral search around preferred position for valid uncrowded ground
            float[] searchRadii = new float[] { 2.0f, 4.0f, 6.0f, 9.0f };
            int pointsPerRadius = 8;

            foreach (float r in searchRadii)
            {
                for (int i = 0; i < pointsPerRadius; i++)
                {
                    float angle = (i / (float)pointsPerRadius) * Mathf.PI * 2f;
                    Vector3 candidate = preferredPos + new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);

                    if (ValidateEnemySpawnPosition(candidate, clearanceRadius, playerPos, existingEnemyPositions, out validPos))
                    {
                        return validPos;
                    }
                }
            }

            // Fallback ground raycast if spiral search was tightly constrained
            Vector3 rayStart = new Vector3(preferredPos.x, preferredPos.y + 10f, preferredPos.z);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 25f))
            {
                return hit.point + Vector3.up * 0.1f;
            }

            return preferredPos;
        }

        private bool TryFindNearestValidPosition(Vector3 centerPos, float clearanceRadius, out Vector3 validGroundPos)
        {
            float[] searchRadii = new float[] { 1.5f, 3.0f, 5.0f, 8.0f };
            int pointsPerRadius = 8;

            foreach (float r in searchRadii)
            {
                for (int i = 0; i < pointsPerRadius; i++)
                {
                    float angle = (i / (float)pointsPerRadius) * Mathf.PI * 2f;
                    Vector3 candidate = centerPos + new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);

                    if (ValidateSpawnPosition(candidate, clearanceRadius, out validGroundPos))
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

            // Draw RED for tested invalid candidate points
            Gizmos.color = new Color(1.0f, 0.2f, 0.2f, 0.7f);
            foreach (var pt in debugInvalidTestPoints)
            {
                Gizmos.DrawWireSphere(pt, 0.4f);
            }

            // Draw GREEN for verified last player spawn
            if (debugLastValidPlayerSpawn != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(debugLastValidPlayerSpawn + Vector3.up * 0.2f, 0.5f);
            }
        }
    }
}
