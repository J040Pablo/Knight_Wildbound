using UnityEngine;
using Roguelite.UI;
using Roguelite.Core;

namespace Roguelite.Player
{
    /// <summary>
    /// Production-ready Camera Bobbing and Head Movement System.
    /// Features distinct motion harmonics for walking, sprinting, and horse riding
    /// inspired by Minecraft, Valheim, and Skyrim.
    /// Operates as a local camera component (non-singleton) with zero per-frame allocations.
    /// Functions completely independently from Camera Shake.
    /// </summary>
    public class CameraBobbing : MonoBehaviour
    {
        [Header("Intensity Multiplier (0.0 = Off, 0.5 = Weak, 1.0 = Normal, 1.5 = Strong)")]
        [Range(0f, 1.5f)] [SerializeField] private float intensityMultiplier = 1.0f;

        [Header("Walk Bob Profile")]
        [Range(0.03f, 0.06f)] [SerializeField] private float walkAmplitude = 0.045f;
        [Range(6f, 8f)]       [SerializeField] private float walkFrequency = 7.0f;

        [Header("Sprint Bob Profile (Aggressive Foot-Plant Harmonics)")]
        [Range(0.05f, 0.09f)] [SerializeField] private float sprintAmplitude = 0.070f;
        [Range(9f, 12f)]      [SerializeField] private float sprintFrequency = 10.5f;

        [Header("Horse Riding Bob Profile (Heavy Stride Drop & Bounce)")]
        [Range(0.08f, 0.15f)] [SerializeField] private float horseAmplitude = 0.110f;
        [Range(3f, 5f)]       [SerializeField] private float horseFrequency = 4.0f;

        [Header("Smoothing Parameters")]
        [SerializeField] private float transitionSpeed = 10.0f;

        // Public Intensity property for UI & Save system
        public float IntensityMultiplier
        {
            get => intensityMultiplier;
            set
            {
                intensityMultiplier = Mathf.Clamp(value, 0f, 2.0f);
                PlayerPrefs.SetFloat("CameraBobIntensityFloat", intensityMultiplier);
                PlayerPrefs.Save();
                OnIntensityChanged?.Invoke(intensityMultiplier);
            }
        }

        public static event System.Action<float> OnIntensityChanged;

        // Current calculated offsets for ThirdPersonCamera
        public Vector3 CurrentPositionOffset { get; private set; } = Vector3.zero;
        public Quaternion CurrentRotationOffset { get; private set; } = Quaternion.identity;

        // External suppression flag (e.g. cutscenes, dialogues, class selection)
        public bool IsSuppressedByInteraction { get; set; } = false;

        // Internal State tracking
        private float timer = 0f;
        private Vector3 rawPosOffset = Vector3.zero;
        private Vector3 targetPosOffset = Vector3.zero;
        private Vector3 posDampVelocity = Vector3.zero;

        private float currentPitchOffset = 0f;
        private float targetPitchOffset = 0f;
        private float pitchDampVelocity = 0f;

        private float currentRollOffset = 0f;
        private float targetRollOffset = 0f;
        private float rollDampVelocity = 0f;

        // Cached references
        private ThirdPersonCamera tpCam;
        private PlayerController playerCtrl;
        private PlayerStats playerStats;
        private CharacterController playerCC;

        private void Awake()
        {
            if (PlayerPrefs.HasKey("CameraBobIntensityFloat"))
            {
                intensityMultiplier = PlayerPrefs.GetFloat("CameraBobIntensityFloat", 1.0f);
            }
        }

        private void Start()
        {
            CacheReferences();
        }

        public void CacheReferences()
        {
            if (tpCam == null) tpCam = GetComponent<ThirdPersonCamera>() ?? GetComponentInParent<ThirdPersonCamera>();
            if (playerCtrl == null) playerCtrl = FindFirstObjectByType<PlayerController>();
            if (playerCtrl != null)
            {
                if (playerStats == null) playerStats = playerCtrl.GetComponent<PlayerStats>();
                if (playerCC == null) playerCC = playerCtrl.GetComponent<CharacterController>();
            }
        }

        public void UpdateBobbing(float deltaTime)
        {
            // 1. Suppression Checks: Disable during UI, menus, dead state, interactions, or intensity == 0
            bool isMenuOpen = MasteryScreenUI.IsAnyMenuOpen ||
                              (InputStateManager.Instance != null && InputStateManager.Instance.CurrentMode == InputMode.UI);
            bool isDead = playerStats != null && playerStats.IsDead;

            if (isMenuOpen || isDead || IsSuppressedByInteraction || intensityMultiplier <= 0.001f)
            {
                DampToZero(deltaTime);
                return;
            }

            if (tpCam == null || playerCtrl == null)
            {
                CacheReferences();
                if (tpCam == null)
                {
                    DampToZero(deltaTime);
                    return;
                }
            }

            bool isMounted = tpCam.IsMounted;
            float speed = 0f;
            bool isGrounded = true;
            bool isSprinting = false;

            if (isMounted)
            {
                Transform camTarget = tpCam.target;
                if (camTarget != null)
                {
                    CharacterController horseCC = camTarget.GetComponent<CharacterController>();
                    if (horseCC != null)
                    {
                        Vector3 vel = horseCC.velocity;
                        vel.y = 0;
                        speed = vel.magnitude;
                        isGrounded = horseCC.isGrounded;
                    }
                }
            }
            else if (playerCtrl != null)
            {
                isGrounded = playerCtrl.IsGrounded;
                isSprinting = playerCtrl.IsSprinting;
                if (playerCC != null)
                {
                    Vector3 vel = playerCC.velocity;
                    vel.y = 0;
                    speed = vel.magnitude;
                }
            }

            // If in air (jumping/falling) or stationary, damp bobbing smoothly to zero
            if (!isGrounded || speed < 0.2f)
            {
                DampToZero(deltaTime);
                return;
            }

            float targetAmp;
            float targetFreq;

            if (isMounted)
            {
                // Horse Riding Bobbing: Heavy double-harmonic back drop & bounce curve
                targetAmp = horseAmplitude;
                targetFreq = horseFrequency;

                float speedFactor = Mathf.Clamp01(speed / 6.0f);
                float mult = intensityMultiplier * speedFactor;

                timer += deltaTime * targetFreq;

                // Double harmonic: horse back drop (-abs(sin)) + trot rebound (0.3*sin(2t))
                float y = (-Mathf.Abs(Mathf.Sin(timer)) + 0.3f * Mathf.Sin(2f * timer)) * targetAmp * mult;
                float x = Mathf.Cos(timer * 0.5f) * targetAmp * 0.85f * mult;

                targetPosOffset = new Vector3(x, y, 0f);
                targetPitchOffset = (Mathf.Sin(timer) + 0.3f * Mathf.Sin(2f * timer)) * 1.5f * mult;
                targetRollOffset = Mathf.Cos(timer * 0.5f) * 1.0f * mult;
            }
            else if (isSprinting)
            {
                // Sprint Bobbing: Aggressive foot-plant impact double harmonic
                targetAmp = sprintAmplitude;
                targetFreq = sprintFrequency;

                float speedFactor = Mathf.Clamp01(speed / 5.0f);
                float mult = intensityMultiplier * speedFactor;

                timer += deltaTime * targetFreq;

                float y = (Mathf.Sin(timer) + 0.25f * Mathf.Sin(2f * timer)) * targetAmp * mult;
                float x = Mathf.Cos(timer * 0.5f) * targetAmp * 0.6f * mult;

                targetPosOffset = new Vector3(x, y, 0f);
                targetPitchOffset = Mathf.Sin(timer) * 0.45f * mult;
                targetRollOffset = Mathf.Cos(timer * 0.5f) * 0.35f * mult;
            }
            else
            {
                // Walk Bobbing: Smooth natural sinusoidal movement
                targetAmp = walkAmplitude;
                targetFreq = walkFrequency;

                float speedFactor = Mathf.Clamp01(speed / 3.0f);
                float mult = intensityMultiplier * speedFactor;

                timer += deltaTime * targetFreq;

                float y = Mathf.Sin(timer) * targetAmp * mult;
                float x = Mathf.Cos(timer * 0.5f) * targetAmp * 0.4f * mult;

                targetPosOffset = new Vector3(x, y, 0f);
                targetPitchOffset = Mathf.Sin(timer) * 0.25f * mult;
                targetRollOffset = Mathf.Cos(timer * 0.5f) * 0.20f * mult;
            }

            // Smoothly damp offsets to eliminate sudden snaps
            float smoothTime = 1.0f / Mathf.Max(0.1f, transitionSpeed);
            rawPosOffset = Vector3.SmoothDamp(rawPosOffset, targetPosOffset, ref posDampVelocity, smoothTime, float.PositiveInfinity, deltaTime);
            currentPitchOffset = Mathf.SmoothDamp(currentPitchOffset, targetPitchOffset, ref pitchDampVelocity, smoothTime, float.PositiveInfinity, deltaTime);
            currentRollOffset = Mathf.SmoothDamp(currentRollOffset, targetRollOffset, ref rollDampVelocity, smoothTime, float.PositiveInfinity, deltaTime);

            CurrentPositionOffset = rawPosOffset;
            CurrentRotationOffset = Quaternion.Euler(currentPitchOffset, 0f, currentRollOffset).normalized;
        }

        private void DampToZero(float deltaTime)
        {
            targetPosOffset = Vector3.zero;
            targetPitchOffset = 0f;
            targetRollOffset = 0f;

            float smoothTime = 1.0f / Mathf.Max(0.1f, transitionSpeed);
            rawPosOffset = Vector3.SmoothDamp(rawPosOffset, Vector3.zero, ref posDampVelocity, smoothTime, float.PositiveInfinity, deltaTime);
            currentPitchOffset = Mathf.SmoothDamp(currentPitchOffset, 0f, ref pitchDampVelocity, smoothTime, float.PositiveInfinity, deltaTime);
            currentRollOffset = Mathf.SmoothDamp(currentRollOffset, 0f, ref rollDampVelocity, smoothTime, float.PositiveInfinity, deltaTime);

            CurrentPositionOffset = rawPosOffset;
            CurrentRotationOffset = Quaternion.Euler(currentPitchOffset, 0f, currentRollOffset).normalized;
        }
    }
}
