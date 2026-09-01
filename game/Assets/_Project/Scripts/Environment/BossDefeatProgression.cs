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

        public void ExecuteWorldTreeDefeatSequence()
        {
            if (hasTriggered) return;
            hasTriggered = true;
            StartCoroutine(ExecuteBossDefeatSequence());
        }

        private IEnumerator ExecuteBossDefeatSequence()
        {
            // 1. Brief delay for death animation start
            yield return new WaitForSeconds(1.2f);

            // 2. Camera focus & Ground Shake
            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null)
            {
                cam.TriggerShake(1.2f, 2.0f);
            }

            // 3. Status Banner Message
            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.TriggerBanner("🌿 O CORAÇÃO DA FLORESTA FOI PURIFICADO!\nO caminho para além da floresta foi aberto...");
            }

            // 4. Weaken Root Barrier visuals
            if (exitBarrier == null) exitBarrier = FindFirstObjectByType<BiomeExitBarrier>();
            if (exitBarrier != null)
            {
                exitBarrier.ApplyBossDefeatedWeakening();
            }

            // 5. Unlock Barrier Health & set 300 HP
            if (barrierHealth == null) barrierHealth = FindFirstObjectByType<BarrierHealth>();
            if (barrierHealth != null)
            {
                barrierHealth.SetMaxHealth(300f);
                barrierHealth.UnlockBarrier();
            }
        }
    }
}
