using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Items
{
    /// <summary>
    /// Central catalogue of every runtime-generated item in the project.
    /// Canonical items are built on-demand via ScriptableObject.CreateInstance,
    /// then cached so every instance refers to the same definition.
    /// </summary>
    public static class ItemDatabase
    {
        private static readonly Dictionary<string, ItemData> cache = new Dictionary<string, ItemData>();

        public static ItemData Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (cache.TryGetValue(id, out ItemData existing) && existing != null)
            {
                return existing;
            }

            ItemData built = Build(id);
            if (built != null)
            {
                cache[id] = built;
            }
            return built;
        }

        private static ItemData Build(string id)
        {
            switch (id)
            {
                // ── Campaign Relic (Forest Biome) ─────────────────────
                case "relic_tree_seed":
                    return With(ItemData.Create(id, "Seed of the Ancient Tree", "Permanent campaign relic (+25 Max HP). A glowing seed of primordial woodland power.", ItemCategory.Relic, ItemRarity.Relic, "🌱", 0, false, 1), i =>
                    {
                        i.isRelic = true;
                        i.flatHpBonus = 25f;
                    });

                case "relic_fae_heart":
                    return With(ItemData.Create(id, "Heart of the Fae King", "Permanent campaign relic (+30 Max HP). A fragment of the ancient forest's lifeforce.", ItemCategory.Relic, ItemRarity.Relic, "💚", 0, false, 1), i =>
                    {
                        i.isRelic = true;
                        i.flatHpBonus = 30f;
                    });

                case "accessory_fairy_queen_crown":
                    return With(ItemData.Create(id, "Fairy Queen Crown", "A crown blessed by the Queen of the Fairies (+20 HP, +15 Stamina, +5% Speed).", ItemCategory.Ring, ItemRarity.Legendary, "👑", 220, false, 1), i =>
                    {
                        i.flatHpBonus = 20f;
                        i.flatStaminaBonus = 15f;
                        i.moveSpeedBonusPercent = 0.05f;
                    });

                case "material_fairy_dust":
                    return With(ItemData.Create(id, "Fairy Dust", "Glowing magical powder (+15 Damage & +10% Speed).", ItemCategory.Consumable, ItemRarity.Legendary, "✨", 180, false, 1), i =>
                    {
                        i.flatDamageBonus = 15f;
                        i.moveSpeedBonusPercent = 0.10f;
                        i.healAmount = 50f;
                    });

                case "key_crystal_fairy_gate":
                    return With(ItemData.Create(id, "Chave Cristalina", "Chave magica de cristal que abre a Ponte Real do Reino das Fadas.", ItemCategory.QuestItem, ItemRarity.Epic, "🔑", 100, false, 1), i =>
                    {
                    });

                // ── Tree Boss Unique Equipment ───────────────────────
                case "weapon_rootbreaker_axe":
                    return With(ItemData.Create(id, "Rootbreaker Axe", "Heavy greataxe carved from corrupted elder wood (+30 Damage).", ItemCategory.Weapon, ItemRarity.Epic, "🪓", 120, false, 1), i =>
                    {
                        i.flatDamageBonus = 30f;
                    });

                case "amulet_bark_guardian":
                    return With(ItemData.Create(id, "Bark Guardian Amulet", "Ancient wooden talisman (+40 Max HP).", ItemCategory.Amulet, ItemRarity.Epic, "🛡️", 120, false, 1), i =>
                    {
                        i.flatHpBonus = 40f;
                    });

                // ── Weapons (Flat Damage) ───────────────────────────
                case "weapon_rusty_blade":
                    return With(ItemData.Create(id, "Rusty Blade", "A pitted old blade (+5 Damage).", ItemCategory.Weapon, ItemRarity.Common, "🗡️", 12, false, 1), i =>
                    {
                        i.flatDamageBonus = 5f;
                    });

                case "weapon_hunter_sword":
                    return With(ItemData.Create(id, "Hunter's Sword", "Balanced blade favored by forest scouts (+10 Damage).", ItemCategory.Weapon, ItemRarity.Rare, "⚔️", 35, false, 1), i =>
                    {
                        i.flatDamageBonus = 10f;
                    });

                case "weapon_guardian_blade":
                    return With(ItemData.Create(id, "Guardian Blade", "Heavy broadsword of ancient keep sentinels (+15 Damage).", ItemCategory.Weapon, ItemRarity.Rare, "🗡️", 55, false, 1), i =>
                    {
                        i.flatDamageBonus = 15f;
                    });

                case "weapon_ember_edge":
                    return With(ItemData.Create(id, "Ember Edge", "A blade that never quite stops smoldering (+25 Damage).", ItemCategory.Weapon, ItemRarity.Epic, "🔥", 90, false, 1), i =>
                    {
                        i.flatDamageBonus = 25f;
                    });

                case "weapon_dawnbringer":
                    return With(ItemData.Create(id, "Dawnbringer", "Legendary greatsword said to cut through corruption (+40 Damage).", ItemCategory.Weapon, ItemRarity.Legendary, "⚔️", 200, false, 1), i =>
                    {
                        i.flatDamageBonus = 40f;
                    });

                // ── Amulets ─────────────────────────────────────────
                case "amulet_vitality":
                    return With(ItemData.Create(id, "Amulet of Vitality", "A polished jade talisman (+25 Max HP).", ItemCategory.Amulet, ItemRarity.Rare, "📿", 45, false, 1), i =>
                    {
                        i.flatHpBonus = 25f;
                    });

                case "amulet_strength":
                    return With(ItemData.Create(id, "Amulet of Strength", "Heavy iron medallion (+5 Damage).", ItemCategory.Amulet, ItemRarity.Rare, "📿", 45, false, 1), i =>
                    {
                        i.flatDamageBonus = 5f;
                    });

                case "amulet_endurance":
                    return With(ItemData.Create(id, "Amulet of Endurance", "Woven tendon charm (+20 Max Stamina).", ItemCategory.Amulet, ItemRarity.Rare, "📿", 45, false, 1), i =>
                    {
                        i.flatStaminaBonus = 20f;
                    });

                // ── Rings ───────────────────────────────────────────
                case "ring_vigor":
                    return With(ItemData.Create(id, "Ring of Vigor", "A simple band that steadies the heart (+15 Max HP).", ItemCategory.Ring, ItemRarity.Rare, "💍", 40, false, 1), i =>
                    {
                        i.flatHpBonus = 15f;
                    });

                case "ring_swiftness":
                    return With(ItemData.Create(id, "Ring of Swiftness", "Light as a feather (+8% Move Speed).", ItemCategory.Ring, ItemRarity.Rare, "💍", 40, false, 1), i =>
                    {
                        i.moveSpeedBonusPercent = 0.08f;
                    });

                case "ring_of_shadows":
                    return With(ItemData.Create(id, "Legendary Ring of Shadows", "Grants active stealth [R] (10s duration, 60s cooldown).", ItemCategory.Ring, ItemRarity.Legendary, "🌑", 250, false, 1), i =>
                    {
                        i.useCooldown = 60f;
                    });

                // ── Ancient Colossus Mini-Boss Unique Drops ────────────
                case "ring_ancient_colossus":
                    return With(ItemData.Create(id, "Ancient Colossus Ring", "Heavy stone ring infused with mountain earth (+35 Max HP).", ItemCategory.Ring, ItemRarity.Epic, "💍", 150, false, 1), i =>
                    {
                        i.flatHpBonus = 35f;
                    });

                case "amulet_colossus_stone":
                    return With(ItemData.Create(id, "Colossus Stone Amulet", "Chiseled granite neckpiece (+10 Damage & +25 Max Stamina).", ItemCategory.Amulet, ItemRarity.Epic, "📿", 150, false, 1), i =>
                    {
                        i.flatDamageBonus = 10f;
                        i.flatStaminaBonus = 25f;
                    });

                // ── Belts ───────────────────────────────────────────
                case "belt_sturdy_leather":
                    return With(ItemData.Create(id, "Sturdy Leather Belt", "Thick tanned hide, reinforced with iron studs (+20 Max Stamina).", ItemCategory.Belt, ItemRarity.Rare, "🎗️", 40, false, 1), i =>
                    {
                        i.flatStaminaBonus = 20f;
                    });

                case "belt_swift_stride":
                    return With(ItemData.Create(id, "Belt of Swift Stride", "Woven from wind-touched fibers (+6% Move Speed).", ItemCategory.Belt, ItemRarity.Rare, "🎗️", 40, false, 1), i =>
                    {
                        i.moveSpeedBonusPercent = 0.06f;
                    });

                case "belt_colossus_hide":
                    return With(ItemData.Create(id, "Colossus Hide Belt", "Cut from ancient stone-hardened leather (+25 Max HP).", ItemCategory.Belt, ItemRarity.Epic, "🎗️", 150, false, 1), i =>
                    {
                        i.flatHpBonus = 25f;
                    });

                // ── Consumables ─────────────────────────────────────
                case "small_potion":
                    return With(ItemData.Create(id, "Small Potion", "A faint fae brew. Restores 20 HP.", ItemCategory.Consumable, ItemRarity.Common, "🧪", 5), i =>
                    {
                        i.healAmount = 20f;
                        i.useCooldown = 8f;
                    });

                case "health_potion":
                    return With(ItemData.Create(id, "Health Potion", "Restores 50 HP.", ItemCategory.Consumable, ItemRarity.Common, "🧪", 10), i =>
                    {
                        i.healAmount = 50f;
                        i.useCooldown = 15f;
                    });

                case "greater_health_potion":
                    return With(ItemData.Create(id, "Greater Health Potion", "Restores 100 HP.", ItemCategory.Consumable, ItemRarity.Rare, "🧪", 25), i =>
                    {
                        i.healAmount = 100f;
                        i.useCooldown = 20f;
                    });

                case "antidote_potion":
                    return With(ItemData.Create(id, "Antidote Potion", "Restores 30 HP and cleanses harmful spores.", ItemCategory.Consumable, ItemRarity.Common, "🟩", 8), i =>
                    {
                        i.healAmount = 30f;
                        i.cleansesDebuffs = true;
                        i.useCooldown = 10f;
                    });

                case "stamina_potion":
                    return With(ItemData.Create(id, "Stamina Potion", "Fully restores stamina.", ItemCategory.Consumable, ItemRarity.Common, "💧", 10), i =>
                    {
                        i.restoresStaminaFully = true;
                        i.useCooldown = 30f;
                    });

                default:
                    Debug.LogWarning($"[ItemDatabase] Unknown item id requested: '{id}'");
                    return null;
            }
        }

        private static ItemData With(ItemData data, System.Action<ItemData> configure)
        {
            configure?.Invoke(data);
            return data;
        }
    }
}
