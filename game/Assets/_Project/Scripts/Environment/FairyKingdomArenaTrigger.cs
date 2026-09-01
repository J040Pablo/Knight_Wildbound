using System;
using UnityEngine;
using Roguelite.Core;
using Roguelite.Enemy;
using Roguelite.Wave;

namespace Roguelite.Environment
{
    public class FairyKingdomArenaTrigger : MonoBehaviour
    {
        public static bool IsArenaLocked { get; private set; } = false;
        public static event Action OnFairyBossActivated;

        private bool hasTriggered = false;
        private GameObject activeBarrierWall;

        private void Awake()
        {
            IsArenaLocked = false;
            hasTriggered = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            if (PlayerDetectionUtility.IsPlayerCollider(other))
            {
                TriggerFairyCourtBattle();
            }
        }

        public void TriggerFairyCourtBattle()
        {
            if (hasTriggered) return;

            hasTriggered = true;
            IsArenaLocked = true;

            // Raise entrance barrier wall
            SpawnEntranceBarrier();

            // Display boss fight intro banner
            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.TriggerBanner("👑 RAINHA DAS FADAS — Guardiã do Reino Encantado!");
            }

            // Activate Fairy Queen Boss Fight
            FairyQueenAI queen = FindFirstObjectByType<FairyQueenAI>();
            if (queen != null)
            {
                queen.TriggerBossFight();
            }

            OnFairyBossActivated?.Invoke();
        }

        private void SpawnEntranceBarrier()
        {
            if (activeBarrierWall != null) return;

            activeBarrierWall = new GameObject("FairyCourt_EntranceBarrier");
            activeBarrierWall.transform.position = transform.position;

            // Create crystal barrier wall pillars across entrance
            for (int i = -3; i <= 3; i++)
            {
                Vector3 pillarPos = transform.position + new Vector3(i * 4f, 0, 0);
                float terrainY = SceneEnvironmentBuilder.GetTerrainHeightY(pillarPos.x, pillarPos.z);
                pillarPos.y = terrainY;

                GameObject crystalPillar = WorldPlaceholderFactory.Build(PlaceholderAssetKey.GlowingCrystal, activeBarrierWall.transform, new Color(0.9f, 0.2f, 0.95f), 2.2f);
                crystalPillar.transform.position = pillarPos;
                BoxCollider col = crystalPillar.AddComponent<BoxCollider>();
                col.size = new Vector3(3.8f, 10f, 3.8f);
            }
        }

        public void UnlockArena()
        {
            IsArenaLocked = false;
            if (activeBarrierWall != null)
            {
                Destroy(activeBarrierWall);
                activeBarrierWall = null;
            }
        }

        public static void ResetState()
        {
            IsArenaLocked = false;
            FairyKingdomArenaTrigger[] triggers = FindObjectsByType<FairyKingdomArenaTrigger>(FindObjectsSortMode.None);
            foreach (var t in triggers)
            {
                if (t != null)
                {
                    t.hasTriggered = false;
                    t.UnlockArena();
                }
            }
        }
    }
}
