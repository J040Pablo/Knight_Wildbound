using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Items;
using Roguelite.Player;

namespace Roguelite.Progression
{
    /// <summary>
    /// Central manager for permanent campaign Boss Relics.
    /// Relics are account-level progression bonuses and do NOT occupy inventory or equipment slots.
    /// </summary>
    public class RelicManager : MonoBehaviour
    {
        private static RelicManager instance;
        private static bool applicationIsQuitting = false;

        public static RelicManager Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<RelicManager>();
                    if (instance == null && !applicationIsQuitting)
                    {
                        GameObject go = new GameObject("RelicManager");
                        instance = go.AddComponent<RelicManager>();
                    }
                }
                return instance;
            }
        }

        private void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private readonly HashSet<string> collectedRelics = new HashSet<string>();

        public event Action<ItemData> OnRelicCollected;

        public int CollectedRelicCount => collectedRelics.Count;

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

        public bool HasRelic(string relicId)
        {
            if (string.IsNullOrEmpty(relicId)) return false;
            return collectedRelics.Contains(relicId);
        }

        /// <summary>
        /// Collects a campaign relic (single-shot enforcement). Automatically applies its
        /// permanent passive stat bonus to the PlayerStats and notifies UI listeners.
        /// </summary>
        public bool CollectRelic(string relicId)
        {
            if (string.IsNullOrEmpty(relicId)) return false;
            if (HasRelic(relicId))
            {
                Debug.Log($"[RelicManager] Relic '{relicId}' already collected. Ignoring duplicate.");
                return false;
            }

            collectedRelics.Add(relicId);
            ItemData relicItem = ItemDatabase.Get(relicId);

            ApplyRelicPassive(relicId);

            Debug.Log($"[RelicManager] 🏆 CAMPAIGN RELIC OBTAINED: '{relicItem?.itemName ?? relicId}'");
            OnRelicCollected?.Invoke(relicItem);
            return true;
        }

        private void ApplyRelicPassive(string relicId)
        {
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();

            switch (relicId)
            {
                case "relic_tree_seed": // Forest Biome Relic: +25 Max HP
                    if (stats != null)
                    {
                        stats.ModifyMaxHP(25f);
                    }
                    break;

                /* TODO FUTURE BIOME RELICS:
                case "relic_stone_heart":   // Mountain Biome: +50 Max HP
                case "relic_desert_fang":   // Desert Biome: +15 Flat Damage
                case "relic_ice_crown":     // Ice Biome: +20 Max Stamina
                case "relic_kraken_eye":    // Swamp Biome: +5% Move Speed
                case "relic_minotaur_horn": // Labyrinth Biome: +20 Flat Damage
                */

                default:
                    break;
            }
        }

        public List<string> GetCollectedRelicIds()
        {
            return new List<string>(collectedRelics);
        }

        public void LoadSavedRelics(IEnumerable<string> relicIds)
        {
            if (relicIds == null) return;
            foreach (var id in relicIds)
            {
                if (!string.IsNullOrEmpty(id) && !collectedRelics.Contains(id))
                {
                    collectedRelics.Add(id);
                    ApplyRelicPassive(id);
                }
            }
        }

        public void ResetRelics()
        {
            collectedRelics.Clear();
        }
    }
}
