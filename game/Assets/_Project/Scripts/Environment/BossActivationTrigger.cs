using UnityEngine;
using System;
using Roguelite.Enemy;
using Roguelite.Wave;

namespace Roguelite.Environment
{
    public class BossActivationTrigger : MonoBehaviour
    {
        public static bool IsBossActivated { get; private set; } = false;
        public static event Action OnBossActivated;

        private bool hasTriggered = false;

        private void Awake()
        {
            IsBossActivated = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            if (other.CompareTag("Player") || other.GetComponent<Roguelite.Player.PlayerController>() != null)
            {
                TriggerBossFight();
            }
        }

        public void TriggerBossFight()
        {
            hasTriggered = true;
            IsBossActivated = true;

            // Notify EncounterManager banner
            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.TriggerBanner("⚠️ HOLLOW TREE BOSS FIGHT STARTED!");
            }

            // Ensure Hollow Tree Boss is active & targeted
            HollowTreeBossAI boss = FindFirstObjectByType<HollowTreeBossAI>();
            if (boss != null)
            {
                boss.enabled = true;
            }

            OnBossActivated?.Invoke();
        }

        public static void ResetState()
        {
            IsBossActivated = false;
        }
    }
}
