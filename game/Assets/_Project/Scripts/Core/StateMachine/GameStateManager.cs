using System;
using UnityEngine;

namespace Roguelite.Core.StateMachine
{
    public class GameStateManager : MonoBehaviour
    {
        private static GameStateManager instance;
        public static GameStateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<GameStateManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GameStateManager");
                        instance = go.AddComponent<GameStateManager>();
                    }
                }
                return instance;
            }
        }

        public GameState CurrentState { get; private set; } = GameState.Gameplay;
        public GameState PreviousState { get; private set; } = GameState.Gameplay;

        public event Action<GameState, GameState> OnGameStateChanged;

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
            ApplyStateProperties(CurrentState);
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            PreviousState = CurrentState;
            CurrentState = newState;

            ApplyStateProperties(newState);

            // Debug.Log($"[GameStateManager] State changed: {PreviousState} -> {CurrentState}");
            OnGameStateChanged?.Invoke(PreviousState, CurrentState);
        }

        public void ToggleState(GameState targetState)
        {
            if (CurrentState == targetState)
            {
                SetState(PreviousState == targetState ? GameState.Gameplay : PreviousState);
            }
            else
            {
                SetState(targetState);
            }
        }

        public bool IsState(GameState state)
        {
            return CurrentState == state;
        }

        public bool IsGameplayActive()
        {
            return CurrentState == GameState.Gameplay || CurrentState == GameState.BossFight;
        }

        private void ApplyStateProperties(GameState state)
        {
            switch (state)
            {
                case GameState.Gameplay:
                case GameState.BossFight:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Time.timeScale = 0f;
                    break;

                case GameState.Inventory:
                case GameState.Mastery:
                case GameState.Dialogue:
                case GameState.Menu:
                case GameState.Transition:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Time.timeScale = 1f;
                    break;
            }
        }
    }
}
