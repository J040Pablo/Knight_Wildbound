using UnityEngine;
using Roguelite.Core.StateMachine;

namespace Roguelite.Core
{
    public enum InputMode
    {
        Gameplay,
        UI
    }

    public class InputStateManager : MonoBehaviour
    {
        private static InputStateManager instance;
        public static InputStateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<InputStateManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("InputStateManager");
                        instance = go.AddComponent<InputStateManager>();
                    }
                }
                return instance;
            }
        }

        public InputMode CurrentMode => GameStateManager.Instance != null && GameStateManager.Instance.IsGameplayActive() ? InputMode.Gameplay : InputMode.UI;

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
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            Debug.Log($"[InputStateManager] Synced with GameState: {newState}");
        }

        public void SetGameplayMode()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
            }
        }

        public void SetUIMode()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetState(GameState.Inventory);
            }
        }
    }
}

