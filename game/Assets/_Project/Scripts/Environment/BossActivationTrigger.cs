using System;
using UnityEngine;
using Roguelite.Core;
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
            hasTriggered = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            if (PlayerDetectionUtility.IsPlayerCollider(other))
            {
                TriggerBossFight();
            }
        }

        public void TriggerBossFight()
        {
            if (hasTriggered) return;

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
            BossActivationTrigger[] triggers = FindObjectsByType<BossActivationTrigger>(FindObjectsSortMode.None);
            foreach (var t in triggers)
            {
                if (t != null) t.hasTriggered = false;
            }
        }
    }
}
