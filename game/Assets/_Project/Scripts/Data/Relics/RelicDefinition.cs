using UnityEngine;

namespace Roguelite.Data
{
    public enum RelicEffectType
    {
        None,
        AncientSeedRegen,
        DesertSandDodge,
        FrostCoreChill,
        PoisonHeartAura,
        LavaCrownBurn,
        RuneManaEfficiency
    }

    [CreateAssetMenu(fileName = "NewRelicDefinition", menuName = "Roguelite/Data/Relic Definition")]
    public class RelicDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string relicId = "relic_ancient_seed";
        public string relicName = "Seed of the Ancient Tree";
        [TextArea(2, 4)]
        public string description = "Grants passive health regeneration and increased move speed.";
        public Sprite icon;

        [Header("Stat Bonuses")]
        public float bonusHealth = 25f;
        public float bonusDamage = 5f;
        public float bonusMoveSpeed = 0.5f;

        [Header("Passive Effect")]
        public RelicEffectType passiveEffect = RelicEffectType.AncientSeedRegen;
    }
}
