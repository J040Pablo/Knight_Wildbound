using UnityEngine;

namespace Roguelite.Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        public Transform target;
        [SerializeField] private float heightOffset = 1.3f;
        [SerializeField] private float rightOffset = 1.8f;

        [Header("Distance & Angles")]
        [SerializeField] private float distance = 6.0f;
        [SerializeField] private float minDistance = 2.0f;
        [SerializeField] private float maxDistance = 8.0f;
        public float pitch = 15.0f; // Vertical angle
        public float yaw = 0.0f;    // Horizontal angle

        [Header("Sensitivity & Limits")]
        public float mouseSensitivity = 2.5f;
        public float minPitch = -20.0f;
        public float maxPitch = 70.0f;

        [Header("Smoothing & Collision")]
        public float smoothSpeed = 14f;
        [SerializeField] private LayerMask collisionLayers;

        private Vector3 currentVelocity;

        private void Start()
        {
            // Lock and hide cursor for modern 3D action gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (collisionLayers == 0)
            {
                collisionLayers = ~LayerMask.GetMask("Ignore Raycast", "UI", "Player", "Water");
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Maintain cursor lock state
            if (Cursor.lockState != CursorLockMode.Locked && !UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            // Mouse orbit look input
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

            // Pivot point calculated with right and height offset for Over-The-Shoulder view
            Vector3 targetPivot = target.position + Vector3.up * heightOffset;
            Vector3 shoulderOffset = rotation * Vector3.right * rightOffset;
            Vector3 pivotWithShoulder = targetPivot + shoulderOffset;

            // Desired camera position before collision check
            Vector3 desiredCamPos = pivotWithShoulder - (rotation * Vector3.forward * distance);

            // Camera Obstacle Collision Avoidance
            float currentDistance = distance;
            Ray ray = new Ray(targetPivot, desiredCamPos - targetPivot);
            float maxRayDist = Vector3.Distance(targetPivot, desiredCamPos);

            if (Physics.SphereCast(ray, 0.35f, out RaycastHit hit, maxRayDist, collisionLayers))
            {
                if (!hit.collider.isTrigger && hit.collider.gameObject.tag != "Player" && !hit.collider.name.EndsWith("Ground"))
                {
                    currentDistance = Mathf.Clamp(hit.distance - 0.2f, minDistance, distance);
                }
            }

            Vector3 finalCamPos = pivotWithShoulder - (rotation * Vector3.forward * currentDistance);

            // Smooth Damping follow
            transform.position = Vector3.SmoothDamp(transform.position, finalCamPos, ref currentVelocity, 1.0f / smoothSpeed);

            // Aim look target centered on shoulder line ahead so reticle aligns to screen center
            Vector3 lookTarget = pivotWithShoulder + (rotation * Vector3.forward * 20.0f);
            Vector3 lookDir = lookTarget - transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            }
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
