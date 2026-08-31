using UnityEngine;
using Roguelite.UI;
using Roguelite.Core;

namespace Roguelite.Player
{
    /// <summary>
    /// High-performance 3rd Person Over-The-Shoulder Camera Controller.
    /// Features zero-allocation per-frame execution, non-allocating Physics queries,
    /// dynamic obstacle collision avoidance, terrain grounding clamp, and smooth orbit looking.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        public Transform target;
        [SerializeField] private float heightOffset = 1.3f;
        [SerializeField] private float mountedHeightOffset = 2.6f;
        [SerializeField] private float rightOffset = 1.6f;

        [Header("Distance & Angles")]
        [SerializeField] private float distance = 6.0f;
        [SerializeField] private float minDistance = 2.0f;
        [SerializeField] private float maxDistance = 8.0f;
        public float pitch = 15.0f; // Vertical angle
        public float yaw = 0.0f;    // Horizontal angle

        [Header("Sensitivity & Limits")]
        public float mouseSensitivity = 2.5f;
        public float minPitch = -75.0f;
        public float maxPitch = 85.0f;

        [Header("Smoothing & Collision")]
        public float smoothSpeed = 16f;
        [SerializeField] private LayerMask collisionLayers;

        public bool IsMounted { get; set; } = false;

        private Camera cachedCamera;
        private Transform cachedTargetRoot;
        private Vector3 currentVelocity;
        private float shakeTimer = 0f;
        private float shakeIntensity = 0f;

        // Non-allocating raycast buffer to prevent per-frame GC allocations
        private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        public void TriggerShake(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeTimer = duration;
        }

        private void Start()
        {
            cachedCamera = GetComponent<Camera>();
            if (cachedCamera != null)
            {
                cachedCamera.nearClipPlane = 0.05f;
                cachedCamera.farClipPlane = 350f; // Extended 350m far clip plane
                cachedCamera.useOcclusionCulling = false; // Disabled unbaked Occlusion Culling to prevent CPU hitching
            }

            if (collisionLayers == 0)
            {
                collisionLayers = ~LayerMask.GetMask("Ignore Raycast", "UI", "Player", "Water");
            }

            CacheTargetReferences();
        }

        public void CacheTargetReferences()
        {
            if (target != null)
            {
                cachedTargetRoot = target.root;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Block camera orbit when menu or UI mode is active (Zero string allocation)
            if (MasteryScreenUI.IsAnyMenuOpen) return;
            if (InputStateManager.Instance != null && InputStateManager.Instance.CurrentMode == InputMode.UI) return;

            // Cache target root if updated dynamically (e.g., mounting horse)
            if (cachedTargetRoot != target.root)
            {
                cachedTargetRoot = target.root;
            }

            // Mouse orbit look input
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            rotation.Normalize();

            // Dynamic height offset based on mounted state
            float activeHeight = IsMounted ? mountedHeightOffset : heightOffset;

            // Pivot point calculated with right and height offset for Over-The-Shoulder view
            Vector3 targetPivot = target.position + Vector3.up * activeHeight;
            Vector3 shoulderOffset = rotation * Vector3.right * rightOffset;
            Vector3 pivotWithShoulder = targetPivot + shoulderOffset;

            // Clamp desired camera distance within min/max bounds
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            // Desired camera position before collision check
            Vector3 desiredCamPos = pivotWithShoulder - (rotation * Vector3.forward * distance);

            // Non-allocating Camera Obstacle Collision Avoidance
            float currentDistance = distance;
            Vector3 rayDir = (desiredCamPos - targetPivot);
            float maxRayDist = rayDir.magnitude;

            if (maxRayDist > 0.001f)
            {
                Ray ray = new Ray(targetPivot, rayDir / maxRayDist);
                int hitCount = Physics.SphereCastNonAlloc(ray, 0.30f, hitBuffer, maxRayDist, collisionLayers);
                float closestHitDist = maxRayDist;

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = hitBuffer[i];
                    Collider col = hit.collider;
                    if (col == null || col.isTrigger) continue;

                    // Fast non-allocating hierarchy & tag filter
                    Transform colTransform = col.transform;
                    if (colTransform == target || colTransform == cachedTargetRoot || colTransform.root == cachedTargetRoot || col.CompareTag("Player"))
                    {
                        continue;
                    }

                    if (hit.distance < closestHitDist)
                    {
                        closestHitDist = hit.distance;
                    }
                }

                if (closestHitDist < maxRayDist)
                {
                    currentDistance = Mathf.Clamp(closestHitDist - 0.15f, minDistance, distance);
                }
            }

            Vector3 finalCamPos = pivotWithShoulder - (rotation * Vector3.forward * currentDistance);

            // Camera Screen Shake Offset
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity * Mathf.Clamp01(shakeTimer);
                finalCamPos += shakeOffset;
            }

            // Ground Collision Clamp: Camera MUST NEVER clip below terrain surface!
            float groundH = Roguelite.Environment.SceneEnvironmentBuilder.GetTerrainHeightY(finalCamPos.x, finalCamPos.z);
            if (finalCamPos.y < groundH + 0.8f)
            {
                finalCamPos.y = groundH + 0.8f;
            }

            // Zero-alloc Smooth Damping follow
            transform.position = Vector3.SmoothDamp(transform.position, finalCamPos, ref currentVelocity, 1.0f / smoothSpeed);
            transform.rotation = rotation;
        }

        public Vector3 GetForwardVector()
        {
            Vector3 forward = transform.forward;
            forward.y = 0;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        public Vector3 GetRightVector()
        {
            Vector3 right = transform.right;
            right.y = 0;
            return right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
        }
    }
}
