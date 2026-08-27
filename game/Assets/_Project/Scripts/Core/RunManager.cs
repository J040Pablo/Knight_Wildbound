using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Roguelite.Player;

namespace Roguelite.Core
{
    public enum RunState
    {
        InRun,
        Victory,
        GameOver
    }

    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        public RunState State { get; private set; } = RunState.InRun;
        public float RunTimeSeconds { get; private set; } = 0f;
        public int TotalKills { get; private set; } = 0;

        private PlayerStats playerStats;

        public event Action<RunState> OnRunStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Time.timeScale = 1.0f; // Ensure time is unpaused on run start
            playerStats = FindFirstObjectByType<PlayerStats>();

            if (playerStats != null)
            {
                playerStats.OnDeath += HandlePlayerDeath;
            }

            if (GameSessionManager.Instance != null)
            {
                RunTimeSeconds = GameSessionManager.Instance.RunTimeSeconds;
                TotalKills = GameSessionManager.Instance.TotalKills;
            }
        }

        private void Update()
        {
            if (State == RunState.InRun)
            {
                RunTimeSeconds += Time.deltaTime;
                if (GameSessionManager.Instance != null)
                {
                    GameSessionManager.Instance.RunTimeSeconds = RunTimeSeconds;
                }
            }
        }

        public void RegisterKill()
        {
            TotalKills++;
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.TotalKills = TotalKills;
            }
        }

        private void HandlePlayerDeath()
        {
            if (State != RunState.InRun) return;

            State = RunState.GameOver;
            OnRunStateChanged?.Invoke(State);
        }

        public void TriggerVictory()
        {
            if (State != RunState.InRun) return;

            State = RunState.Victory;
            OnRunStateChanged?.Invoke(State);
        }

        public void RestartRun()
        {
            Time.timeScale = 1.0f;
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.ResetSession();
            }

            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadScene(SceneTransitionManager.SCENE_RUINS);
            }
            else
            {
                SceneManager.LoadScene(SceneTransitionManager.SCENE_RUINS);
            }
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1.0f;
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.ResetSession();
            }
            SceneManager.LoadScene(0);
        }
    }
}
