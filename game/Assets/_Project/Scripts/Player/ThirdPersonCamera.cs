using UnityEngine;
using Roguelite.UI;
using Roguelite.Core;

namespace Roguelite.Player
{
    /// <summary>
    /// AAA-Quality 3rd Person Over-The-Shoulder Camera Controller (Elden Ring / Genshin / BOTW Standard).
    /// Features:
    /// 1. Zero input lag 1:1 raw mouse orbit responsiveness (lockstep rotation & position calculation).
    /// 2. Smooth target pivot tracking without position/rotation phase misalignment.
    /// 3. Smooth obstacle collision distance interpolation to eliminate camera popping.
    /// 4. Decoupled, non-filtered locomotion head bobbing & screen shake.
    /// 5. Zero per-frame memory allocation.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        public Transform target;
        [SerializeField] private float defaultHeightOffset = 1.3f;
        [SerializeField] private float defaultDistance = 6.0f;
        [SerializeField] private float mountedHeightOffset = 0.5f;
        [SerializeField] private float mountedDistance = 7.0f;
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
        public float followSmoothTime = 0.03f; // Ultra-smooth pivot follow without lag
        public float collisionSmoothTime = 0.05f; // Smooth obstacle avoidance without distance popping
        [SerializeField] private LayerMask collisionLayers;

        [Header("Locomotion Head Bobbing")]
        [SerializeField] private CameraBobbing cameraBobbing;

        public bool IsMounted { get; set; } = false;

        public float HeightOffset => IsMounted ? mountedHeightOffset : defaultHeightOffset;
        public float MountedHeightOffset => mountedHeightOffset;
        public float DefaultHeightOffset => defaultHeightOffset;
        public float Distance => distance;

        public void SetMountedCameraOffset()
        {
            IsMounted = true;
            distance = mountedDistance;
            currentCollisionDistance = mountedDistance;
            pivotDampVelocity = Vector3.zero;
            collisionDistVelocity = 0f;

            if (target != null)
            {
                currentPivotPos = target.position + Vector3.up * mountedHeightOffset;
            }

        }

        public void RestorePlayerCameraOffset()
        {
            IsMounted = false;
            distance = defaultDistance;
            currentCollisionDistance = defaultDistance;
            pivotDampVelocity = Vector3.zero;
            collisionDistVelocity = 0f;

            if (target != null)
            {
                currentPivotPos = target.position + Vector3.up * defaultHeightOffset;
            }

        }

        private Camera cachedCamera;
        private Transform cachedTargetRoot;
        
        // Pivot and collision distance smooth damp states
        private Vector3 currentPivotPos;
        private Vector3 pivotDampVelocity;
        private float currentCollisionDistance;
        private float collisionDistVelocity;
        private bool isInitialized = false;

        private float shakeTimer = 0f;
        private float shakeIntensity = 0f;

        // Non-allocating raycast buffer to prevent per-frame GC allocations
        private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        public void TriggerShake(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeTimer = duration;
        }

        private void Awake()
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
        }

        private void Start()
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            cachedCamera = GetComponent<Camera>();
            if (cachedCamera != null)
            {
                cachedCamera.nearClipPlane = 0.05f;
                cachedCamera.farClipPlane = 350f; // Extended 350m far clip plane
                cachedCamera.useOcclusionCulling = false; // Disabled unbaked Occlusion Culling to prevent CPU hitching
            }

            if (cameraBobbing == null)
            {
                cameraBobbing = GetComponent<CameraBobbing>();
                if (cameraBobbing == null)
                {
                    cameraBobbing = gameObject.AddComponent<CameraBobbing>();
                }
            }

            if (collisionLayers == 0)
            {
                collisionLayers = ~LayerMask.GetMask("Ignore Raycast", "UI", "Player", "Water");
            }

            CacheTargetReferences();
            currentCollisionDistance = distance;

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.RegisterCamera(this);
            }
        }

        public void CacheTargetReferences()
        {
            if (target != null)
            {
                cachedTargetRoot = target.root;
            }
            else
            {
                cachedTargetRoot = null;
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

            // 1. Raw Mouse Look Input (Zero artificial software lag)
            yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            // 2. Pivot Target Position Handling (Smooth follow player target)
            float activeHeight = IsMounted ? mountedHeightOffset : defaultHeightOffset;
            float activeDistance = IsMounted ? mountedDistance : defaultDistance;
            Vector3 rawTargetPivot = target.position + Vector3.up * activeHeight;

            if (!isInitialized)
            {
                currentPivotPos = rawTargetPivot;
                isInitialized = true;
            }
            else
            {
                currentPivotPos = Vector3.SmoothDamp(currentPivotPos, rawTargetPivot, ref pivotDampVelocity, followSmoothTime);
            }

            // 3. Shoulder Pivot & Ideal Camera Distance
            Vector3 shoulderOffset = rotation * Vector3.right * rightOffset;
            Vector3 pivotWithShoulder = currentPivotPos + shoulderOffset;

            distance = Mathf.Clamp(activeDistance, minDistance, maxDistance);
            Vector3 unconstrainedCamPos = pivotWithShoulder - (rotation * Vector3.forward * distance);

            // 4. Non-allocating Obstacle Collision Check & Distance Smoothing
            float targetCollisionDistance = distance;
            Vector3 rayDir = (unconstrainedCamPos - currentPivotPos);
            float maxRayDist = rayDir.magnitude;

            if (maxRayDist > 0.001f)
            {
                Ray ray = new Ray(currentPivotPos, rayDir / maxRayDist);
                int hitCount = Physics.SphereCastNonAlloc(ray, 0.30f, hitBuffer, maxRayDist, collisionLayers);
                float closestHitDist = maxRayDist;

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = hitBuffer[i];
                    Collider col = hit.collider;
                    if (col == null || col.isTrigger) continue;

                    // Fast non-allocating hierarchy & tag filter
                    Transform colTransform = col.transform;
                    if (colTransform == target || colTransform == cachedTargetRoot || colTransform.root == cachedTargetRoot || 
                        col.CompareTag("Player") || col.GetComponent<HorseController>() != null || col.GetComponent<MountSystem>() != null)
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
                    targetCollisionDistance = Mathf.Clamp(closestHitDist - 0.15f, minDistance, distance);
                }
            }

            // Smoothly interpolate collision distance to prevent camera popping when passing obstacles
            currentCollisionDistance = Mathf.SmoothDamp(currentCollisionDistance, targetCollisionDistance, ref collisionDistVelocity, collisionSmoothTime);

            Vector3 finalCamPos = pivotWithShoulder - (rotation * Vector3.forward * currentCollisionDistance);

            // 5. Update & Apply Locomotion Head Bobbing Offset
            Vector3 bobPosOffset = Vector3.zero;
            Quaternion bobRotOffset = Quaternion.identity;

            if (cameraBobbing != null)
            {
                cameraBobbing.UpdateBobbing(Time.deltaTime);
                Vector3 localBob = cameraBobbing.CurrentPositionOffset;
                bobPosOffset = (rotation * Vector3.right * localBob.x) + (rotation * Vector3.up * localBob.y) + (rotation * Vector3.forward * localBob.z);
                bobRotOffset = cameraBobbing.CurrentRotationOffset;
            }

            finalCamPos += bobPosOffset;

            // 6. Camera Screen Shake Offset (Independent from Locomotion Bobbing)
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity * Mathf.Clamp01(shakeTimer);
                finalCamPos += shakeOffset;
            }

            // 7. Ground Collision Clamp: Camera MUST NEVER clip below terrain surface!
            float groundH = Roguelite.Environment.SceneEnvironmentBuilder.GetTerrainHeightY(finalCamPos.x, finalCamPos.z);
            if (finalCamPos.y < groundH + 0.8f)
            {
                finalCamPos.y = groundH + 0.8f;
            }

            // 8. Lockstep Transform Update (Position & Rotation applied simultaneously)
            transform.position = finalCamPos;
            Quaternion finalRot = rotation * bobRotOffset;
            transform.rotation = finalRot;
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
