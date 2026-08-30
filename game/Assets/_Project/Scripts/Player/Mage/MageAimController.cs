using UnityEngine;
using Roguelite.Player;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage
{
    public class MageAimController : MonoBehaviour
    {
        [Header("Aim Settings")]
        [SerializeField] private float maxTargetDistance = 50.0f;
        [SerializeField] private LayerMask targetableLayers = ~0;

        [Header("Debug")]
        [SerializeField] private bool showAimDebug = false;

        private Camera mainCamera;

        public float MaxTargetDistance => maxTargetDistance;

        private void Awake()
        {
            mainCamera = Camera.main;
            InitializeLayerMask();
        }

        private void InitializeLayerMask()
        {
            if (targetableLayers == ~0 || targetableLayers == 0)
            {
                targetableLayers = ~LayerMask.GetMask("Player", "Ignore Raycast", "UI", "Water", "PlayerHitbox");
            }
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (showAimDebug)
            {
                Vector3 pt = GetAimPoint();
                Vector3 origin = GetOriginPosition();
                
                if (mainCamera != null)
                {
                    Ray camRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                    Debug.DrawRay(camRay.origin, camRay.direction * maxTargetDistance, Color.cyan);
                }

                Debug.DrawLine(origin, pt, Color.green);
            }
        }

        public Vector3 GetOriginPosition()
        {
            MountSystem mount = GetComponent<MountSystem>();
            if (mount == null) mount = GetComponentInParent<MountSystem>();
            float yOffset = (mount != null && mount.IsPlayerMounted) ? 2.2f : 1.2f;
            return transform.position + Vector3.up * yOffset;
        }

        public Vector3 GetAimPoint()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            if (mainCamera != null)
            {
                // Center-screen ray from camera lens directly through HUD reticle
                Ray cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                InitializeLayerMask();
                RaycastHit[] hits = Physics.RaycastAll(cameraRay, maxTargetDistance, targetableLayers);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    if (IsPlayerOrChildCollider(hit.collider)) continue;

                    EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
                    if (enemy == null) enemy = hit.collider.GetComponentInParent<EnemyBase>();

                    bool isEnemy = enemy != null && !enemy.IsDead;
                    bool isEnemyLayer = hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy") || hit.collider.gameObject.layer == LayerMask.NameToLayer("Boss") || hit.collider.gameObject.layer == LayerMask.NameToLayer("Destructible");

                    if (hit.collider.isTrigger && !isEnemy && !isEnemyLayer) continue;

                    // Exact 3D point in world under center-screen crosshair
                    return hit.point;
                }

                // If no collider hit within maxTargetDistance, return point at maxTargetDistance along camera reticle ray
                return cameraRay.origin + cameraRay.direction * maxTargetDistance;
            }

            return transform.position + transform.forward * maxTargetDistance + Vector3.up * 1.2f;
        }

        public Vector3 GetGroundAimPoint()
        {
            Vector3 point = GetAimPoint();
            if (Physics.Raycast(point + Vector3.up * 10.0f, Vector3.down, out RaycastHit groundHit, 30.0f, targetableLayers))
            {
                if (!IsPlayerOrChildCollider(groundHit.collider))
                {
                    return groundHit.point;
                }
            }
            return point;
        }

        public Vector3 GetAimDirection()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera.transform.forward;
            }
            return transform.forward;
        }

        public Vector3 GetDirectionToTarget(Vector3 spawnPosition)
        {
            Vector3 aimPoint = GetAimPoint();
            Vector3 dir = (aimPoint - spawnPosition);
            if (dir.sqrMagnitude < 0.0001f)
            {
                return GetAimDirection();
            }
            return dir.normalized;
        }

        private bool IsPlayerOrChildCollider(Collider col)
        {
            if (col == null) return true;
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform) || transform.IsChildOf(col.transform)) return true;
            if (col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) return true;
            
            string n = col.gameObject.name.ToLower();
            if (n.Contains("player") || n.Contains("hitbox")) return true;

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = GetOriginPosition();
            Vector3 aimPoint = GetAimPoint();

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, maxTargetDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(aimPoint, 0.4f);
            Gizmos.DrawLine(origin, aimPoint);
        }
    }
}
