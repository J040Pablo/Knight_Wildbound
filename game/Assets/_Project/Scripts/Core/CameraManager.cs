using UnityEngine;
using Roguelite.Player;

namespace Roguelite.Core
{
    public enum CameraOwnerType
    {
        Player,
        Horse,
        TitanClimb,
        Cutscene,
        Spectator
    }

    /// <summary>
    /// Centralized Camera Authority.
    /// Guarantees that the camera target NEVER leaves the Player transform.
    /// Enforces zero physical transform parenting and automatic recovery of player camera focus.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        private static CameraManager instance;
        public static CameraManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<CameraManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CameraManager");
                        instance = go.AddComponent<CameraManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Camera State")]
        [SerializeField] private CameraOwnerType currentOwner = CameraOwnerType.Player;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private ThirdPersonCamera activeCamera;

        public CameraOwnerType CurrentOwner => currentOwner;
        public Transform PlayerTransform => playerTransform;
        public ThirdPersonCamera ActiveCamera => activeCamera;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            EnsureReferences();
        }

        public void RegisterCamera(ThirdPersonCamera cam)
        {
            if (cam == null) return;
            activeCamera = cam;

            if (activeCamera.transform.parent != null)
            {
                activeCamera.transform.SetParent(null);
            }

            EnsurePlayerReference();
            if (playerTransform != null)
            {
                activeCamera.target = playerTransform;
            }
        }

        public void RegisterPlayer(Transform player)
        {
            if (player == null) return;
            playerTransform = player;

            if (activeCamera != null)
            {
                activeCamera.target = playerTransform;
            }
        }

        private void EnsurePlayerReference()
        {
            if (playerTransform == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    playerTransform = pc.transform;
                }
            }
        }

        private void EnsureReferences()
        {
            if (activeCamera == null)
            {
                activeCamera = FindFirstObjectByType<ThirdPersonCamera>();
            }

            if (activeCamera != null && activeCamera.transform.parent != null)
            {
                activeCamera.transform.SetParent(null);
            }

            EnsurePlayerReference();
        }

        /// <summary>
        /// Update camera state mode (e.g. Horse, TitanClimb) while permanently retaining Player target.
        /// </summary>
        public bool RequestOwnership(CameraOwnerType owner, Transform targetTransform = null, string callerInfo = "")
        {
            EnsureReferences();

            currentOwner = owner;

            if (activeCamera != null)
            {
                if (playerTransform != null)
                {
                    activeCamera.target = playerTransform;
                }

                if (owner == CameraOwnerType.Horse)
                {
                    activeCamera.SetMountedCameraOffset();
                }
                else
                {
                    activeCamera.RestorePlayerCameraOffset();
                }
            }

            return true;
        }

        /// <summary>
        /// Release camera state mode, returning to default Player mode.
        /// </summary>
        public void ReleaseOwnership(CameraOwnerType owner, string callerInfo = "")
        {
            if (currentOwner == owner)
            {
                ForceRestorePlayerCamera($"Released by {owner} ({callerInfo})");
            }
        }

        /// <summary>
        /// Force restore camera target and ownership back to Player.
        /// </summary>
        public void ForceRestorePlayerCamera(string reason = "Forced restore")
        {
            EnsureReferences();

            currentOwner = CameraOwnerType.Player;

            EnsurePlayerReference();

            if (activeCamera != null)
            {
                if (activeCamera.transform.parent != null)
                {
                    activeCamera.transform.SetParent(null);
                }

                if (playerTransform != null)
                {
                    activeCamera.target = playerTransform;
                }
                activeCamera.RestorePlayerCameraOffset();
            }
        }

        private void LateUpdate()
        {
            EnsureReferences();

            if (activeCamera != null)
            {
                // 1. Strict Ban on Physical Camera Parenting
                if (activeCamera.transform.parent != null)
                {
                    activeCamera.transform.SetParent(null);
                }

                // 2. Permanent Player Follow Target Enforcement
                if (playerTransform != null)
                {
                    if (activeCamera.target != playerTransform)
                    {
                        activeCamera.target = playerTransform;
                    }
                }
                else
                {
                    EnsurePlayerReference();
                    if (playerTransform != null)
                    {
                        activeCamera.target = playerTransform;
                    }
                }

                // 3. Runtime Watchdog checks for state synchronization
                MountSystem activeMount = MountSystem.ActiveMount;
                bool isPlayerMounted = activeMount != null && activeMount.IsPlayerMounted;

                if (!isPlayerMounted && (currentOwner == CameraOwnerType.Horse || activeCamera.IsMounted))
                {
                    ForceRestorePlayerCamera("Watchdog: Player is not mounted on horse");
                }
                else if (currentOwner == CameraOwnerType.TitanClimb)
                {
                    Enemy.TitanClimbNode climbNode = FindFirstObjectByType<Enemy.TitanClimbNode>();
                    if (climbNode == null || !climbNode.IsMounted)
                    {
                        ForceRestorePlayerCamera("Watchdog: Player not climbing Titan");
                    }
                }
            }
        }
    }
}
