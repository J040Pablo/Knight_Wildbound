using System.Collections;
using UnityEngine;
using Roguelite.Enemy;
using Roguelite.Wave;
using Roguelite.Player;

namespace Roguelite.Environment
{
    public class BossDefeatProgression : MonoBehaviour
    {
        private HollowTreeBossAI bossAI;
        private BiomeExitBarrier exitBarrier;
        private BarrierHealth barrierHealth;

        private bool hasTriggered = false;

        private void Start()
        {
            FindComponents();
        }

        public void Initialize(HollowTreeBossAI boss, BiomeExitBarrier barrier, BarrierHealth health)
        {
            bossAI = boss;
            exitBarrier = barrier;
            barrierHealth = health;
        }

        private void FindComponents()
        {
            if (bossAI == null) bossAI = FindFirstObjectByType<HollowTreeBossAI>();
            if (exitBarrier == null) exitBarrier = FindFirstObjectByType<BiomeExitBarrier>();
            if (barrierHealth == null) barrierHealth = FindFirstObjectByType<BarrierHealth>();
        }

        private void Update()
        {
            if (hasTriggered) return;

            if (bossAI == null) FindComponents();

            if (bossAI != null && bossAI.IsDead)
            {
                hasTriggered = true;
                StartCoroutine(ExecuteBossDefeatSequence());
            }
        }

        private IEnumerator ExecuteBossDefeatSequence()
        {
            Debug.Log("[BossDefeatProgression] Hollow Tree Boss defeated! Starting progression sequence...");

            // 1. Brief delay for death animation start
            yield return new WaitForSeconds(1.2f);

            // 2. Camera focus & Ground Shake
            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null)
            {
                cam.TriggerShake(0.8f, 1.8f);
            }

            // 3. Status Banner Message
            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.TriggerBanner("✨ THE PATH AHEAD HAS BEEN OPENED.");
            }

            // 4. Weaken Root Barrier visuals
            if (exitBarrier == null) exitBarrier = FindFirstObjectByType<BiomeExitBarrier>();
            if (exitBarrier != null)
            {
                exitBarrier.ApplyBossDefeatedWeakening();
            }

            // 5. Unlock Barrier Health to receive attacks
            if (barrierHealth == null) barrierHealth = FindFirstObjectByType<BarrierHealth>();
            if (barrierHealth != null)
            {
                barrierHealth.UnlockBarrier();
            }
        }
    }
}
