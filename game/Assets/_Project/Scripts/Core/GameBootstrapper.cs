using UnityEngine;
using Roguelite.Player;
using Roguelite.Enemy;
using Roguelite.Environment;
using Roguelite.Wave;
using Roguelite.Progression;
using Roguelite.UI;

namespace Roguelite.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Bootstrap Configuration")]
        [SerializeField] private bool autoStartRunOnLaunch = false;

        private GameObject mainMenuObj;

        private void Awake()
        {
            EnsureCoreSingletonsExist();

            if (autoStartRunOnLaunch)
            {
                StartRunFromMenu();
            }
            else
            {
                // If in active adventure scene (01_Run / GameArena), setup game run immediately.
                // "MainScene" is the menu scene, so it must NOT be treated as a playable scene here.
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentScene.StartsWith("0") || currentScene == "GameArena")
                {
                    SetupGameRunHierarchy();
                }
                else
                {
                    ShowMainMenu();
                }
            }
        }

        private void EnsureCoreSingletonsExist()
        {
            if (GameSessionManager.Instance == null)
            {
                GameObject sessionObj = new GameObject("GameSessionManager");
                sessionObj.AddComponent<GameSessionManager>();
            }

            if (DialogueSystem.Instance == null)
            {
                GameObject diagObj = new GameObject("DialogueSystem");
                diagObj.AddComponent<DialogueSystem>();
            }

            if (PlayerSpawnManager.Instance == null)
            {
                GameObject spawnMgrObj = new GameObject("PlayerSpawnManager");
                spawnMgrObj.AddComponent<PlayerSpawnManager>();
            }
        }

        public void ShowMainMenu()
        {
            if (mainMenuObj == null)
            {
                mainMenuObj = new GameObject("MainMenuManager");
                mainMenuObj.AddComponent<MainMenuController>();
            }
        }

        public void StartRunFromMenu()
        {
            if (mainMenuObj != null)
            {
                Destroy(mainMenuObj);
            }

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.ResetSession();
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("01_Run");
        }

        public void SetupGameRunHierarchy()
        {
            // 1. Build Environment Layout for Active Scene
            GameObject envBuilderObj = new GameObject("SceneEnvironmentBuilder");
            SceneEnvironmentBuilder envBuilder = envBuilderObj.AddComponent<SceneEnvironmentBuilder>();

            // Force PhysX spatial tree sync immediately after procedural geometry creation
            Physics.SyncTransforms();

            // 2. Setup Player Character GameObject
            CharacterType selectedChar = GameSessionManager.Instance != null ? GameSessionManager.Instance.SelectedCharacter : CharacterType.Knight;

            GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = $"Player_{selectedChar}";
            playerObj.tag = "Player";

            // CRITICAL FIX: Immediately destroy primitive CapsuleCollider so CharacterController is the ONLY collider on playerObj!
            Collider primitiveCollider = playerObj.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                DestroyImmediate(primitiveCollider);
            }

            // Add Spawn Tracker for Debug Logging & Spheres
            PlayerSpawnTracker tracker = playerObj.AddComponent<PlayerSpawnTracker>();

            // Add Player Components
            CharacterController cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            PlayerStats stats = playerObj.AddComponent<PlayerStats>();
            PlayerController controller = playerObj.AddComponent<PlayerController>();
            PlayerCombat combat = playerObj.AddComponent<PlayerCombat>();
            InteractionSystem interaction = playerObj.AddComponent<InteractionSystem>();
            playerObj.AddComponent<PlayerFallRecovery>();

            // Delegate safe player placement to PlayerSpawnManager
            if (PlayerSpawnManager.Instance != null)
            {
                PlayerSpawnManager.Instance.SpawnPlayer(playerObj);
            }

            // Character Data Setup
            Roguelite.Data.CharacterData cData = ScriptableObject.CreateInstance<Roguelite.Data.CharacterData>();
            cData.characterName = selectedChar.ToString();

            var field = typeof(PlayerStats).GetField("characterData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(stats, cData);
            stats.RecalculateStats();

            // Equip Class Visuals & Behavior — only if player has selected character this run
            if (GameSessionManager.Instance != null && GameSessionManager.Instance.HasSelectedCharacter)
            {
                WeaponInteractable.SetupPlayerClassVisualsAndBehavior(playerObj, selectedChar, combat, stats);
            }

            // Restore saved session state if exists
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.ApplyPlayerState(stats);
            }

            // 3. Setup Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            ThirdPersonCamera tpCam = mainCam.gameObject.GetComponent<ThirdPersonCamera>();
            if (tpCam == null)
            {
                tpCam = mainCam.gameObject.AddComponent<ThirdPersonCamera>();
            }
            tpCam.target = playerObj.transform;

            // 4. Setup Core Managers
            GameObject runManagerObj = new GameObject("RunManager");
            runManagerObj.AddComponent<RunManager>();
            runManagerObj.AddComponent<UpgradeManager>();
            runManagerObj.AddComponent<EncounterManager>();

            // 5. Setup UI System
            GameObject uiCanvasObj = new GameObject("UIManager");
            uiCanvasObj.AddComponent<HUDController>();
            uiCanvasObj.AddComponent<LevelUpUI>();
            uiCanvasObj.AddComponent<WinLoseUI>();

            if (tracker != null)
            {
                tracker.LogAfterBootstrap(playerObj.transform.position);
            }
        }
    }
}
