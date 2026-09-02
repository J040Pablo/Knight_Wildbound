using UnityEngine;
using Roguelite.Enemy;
using Roguelite.Inventory;

namespace Roguelite.Combat
{
    /// <summary>
    /// Small, focused home for on-hit/on-kill relic effects that need to react to combat
    /// events without touching the core damage pipeline. Called from the single universal
    /// choke points that already exist for each trigger type (currently just EnemyBase.Die()
    /// for on-kill effects) rather than from every individual class's attack code.
    /// </summary>
    public static class OnHitRelicEffects
    {
        private const float BLOOMHEART_INTERNAL_COOLDOWN = 3.5f;
        private static float lastBloomheartTriggerTime = -999f;

        /// <summary>
        /// Bloomheart (Giant Toxic Mushroom relic): if equipped and the player landed the
        /// killing blow, spawns a growing spore-mushroom at the kill site that blooms into a
        /// slow + damage burst for nearby enemies and a brief haste buff for the player.
        /// Gated by a short internal cooldown so it reads as a distinct "moment" rather than
        /// firing (and spawning VFX) on every single kill during a crowd clear.
        /// </summary>
        public static void TryTriggerBloomheart(EnemyBase deadEnemy, GameObject killer)
        {
            if (deadEnemy == null || killer == null) return;
            if (!IsPlayerSource(killer)) return;
            if (EquipmentManager.Instance == null || !EquipmentManager.Instance.IsBloomheartEquipped()) return;
            if (Time.time - lastBloomheartTriggerTime < BLOOMHEART_INTERNAL_COOLDOWN) return;

            lastBloomheartTriggerTime = Time.time;

            GameObject bloomObj = new GameObject("BloomheartBloomVFX");
            bloomObj.transform.position = deadEnemy.transform.position;
            BloomheartBloomVFX vfx = bloomObj.AddComponent<BloomheartBloomVFX>();
            vfx.Initialize(killer, radius: 4.5f, damage: 12f, slowPercent: 0.5f, slowDuration: 3f, hasteAmount: 0.25f, hasteDuration: 2.5f);
        }

        private static bool IsPlayerSource(GameObject source)
        {
            if (source == null) return false;
            if (source.CompareTag("Player")) return true;
            if (source.layer == LayerMask.NameToLayer("Player")) return true;
            return source.GetComponent<Roguelite.Player.PlayerStats>() != null;
        }
    }
}
