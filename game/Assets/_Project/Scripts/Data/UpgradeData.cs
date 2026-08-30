using UnityEngine;

namespace Roguelite.Data
{
    public enum UpgradeCategory
    {
        Universal,
        Knight,
        Mage,
        Druid
    }

    public enum UpgradeType
    {
        AttackDamagePercent,
        MoveSpeedPercent,
        MaxHealthFlat,
        AttackSpeedPercent,
        MaxStaminaPercent,
        CritChancePercent,
        MagicDamagePercent,
        ProjectileSpeedPercent,
        SpellAreaPercent,
        NatureRecoveryPercent,
        XPBoostPercent
    }

    [CreateAssetMenu(fileName = "NewUpgradeData", menuName = "Roguelite/Data/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        public string upgradeTitle = "+20% Damage";
        [TextArea(2, 4)]
        public string description = "Increases all attack damage by 20%.";
        public UpgradeType type = UpgradeType.AttackDamagePercent;
        public UpgradeCategory category = UpgradeCategory.Universal;
        public float statValue = 0.20f; // e.g. 0.20 for +20%, 20 for flat 20 HP
        public Sprite iconSprite;
    }
}
