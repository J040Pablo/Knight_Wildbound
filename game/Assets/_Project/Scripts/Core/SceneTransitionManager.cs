using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Roguelite.Player;

namespace Roguelite.Core
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        public const string SCENE_RUINS = "01_Ruins";
        public const string SCENE_FOREST = "02_Forest";
        public const string SCENE_BOSS = "03_ForestBoss";

        private bool isTransitioning = false;
        private float fadeAlpha = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            if (isTransitioning) return;
            StartCoroutine(PerformTransition(sceneName));
        }

        private IEnumerator PerformTransition(string sceneName)
        {
            isTransitioning = true;

            // 1. Save current player state & disable player controls
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null && GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.SavePlayerState(stats);
            }

            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) pc.enabled = false;

            // 2. Fade to black
            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                fadeAlpha = Mathf.Clamp01(elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            fadeAlpha = 1f;

            // 3. Load scene asynchronously
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null)
            {
                while (!op.isDone)
                {
                    yield return null;
                }
            }

            // Give scene setup a frame
            yield return new WaitForEndOfFrame();

            // 4. Ensure SpawnManager validates & places player safely in new scene
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                var pController = FindFirstObjectByType<PlayerController>();
                if (pController != null) playerObj = pController.gameObject;
            }

            if (playerObj != null && SpawnManager.Instance != null)
            {
                SpawnManager.Instance.SpawnPlayer(playerObj);
            }

            // Re-enable player controller
            if (pc != null) pc.enabled = true;
            else
            {
                var newPC = FindFirstObjectByType<PlayerController>();
                if (newPC != null) newPC.enabled = true;
            }

            // Center camera
            Camera mainCam = Camera.main;
            if (mainCam != null && playerObj != null)
            {
                var tpCam = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpCam != null) tpCam.target = playerObj.transform;
            }

            // 5. Fade back in
            elapsed = 0f;
            while (elapsed < duration)
            {
                fadeAlpha = Mathf.Clamp01(1f - (elapsed / duration));
                elapsed += Time.deltaTime;
                yield return null;
            }
            fadeAlpha = 0f;
            isTransitioning = false;
        }

        private void OnGUI()
        {
            if (fadeAlpha > 0f)
            {
                GUI.color = new Color(0, 0, 0, fadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
    }
}
