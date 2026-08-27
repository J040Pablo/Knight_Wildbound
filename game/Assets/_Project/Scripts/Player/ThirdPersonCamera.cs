using UnityEngine;

namespace Roguelite.Player
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        public Transform target;
        public Vector3 targetOffset = new Vector3(0, 1.8f, 0);

        [Header("Distance & Angles")]
        public float distance = 7.0f;
        public float pitch = 25.0f; // vertical angle
        public float yaw = 0.0f;    // horizontal angle
        
        [Header("Sensitivity & Limits")]
        public float mouseSensitivity = 3.0f;
        public float minPitch = -10.0f;
        public float maxPitch = 60.0f;

        [Header("Smoothing")]
        public float smoothSpeed = 10f;

        private void LateUpdate()
        {
            if (target == null) return;

            // Handle Mouse Orbit input when Right Mouse Button is held OR continuously in gameplay
            if (Input.GetMouseButton(1) || Input.GetMouseButton(0) || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity * 50f * Time.deltaTime;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * 50f * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // Calculate Position & Rotation
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 targetPosition = target.position + targetOffset;
            Vector3 desiredPosition = targetPosition - (rotation * Vector3.forward * distance);

            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
            transform.LookAt(targetPosition);
        }

        public Vector3 GetForwardVector()
        {
            Vector3 forward = transform.forward;
            forward.y = 0;
            return forward.normalized;
        }

        public Vector3 GetRightVector()
        {
            Vector3 right = transform.right;
            right.y = 0;
            return right.normalized;
        }
    }
}
