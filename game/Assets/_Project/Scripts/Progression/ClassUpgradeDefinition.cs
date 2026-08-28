using UnityEngine;
using Roguelite.Player;

namespace Roguelite.Progression
{
    [CreateAssetMenu(fileName = "NewClassUpgrade", menuName = "Roguelite/Progression/Class Upgrade Definition")]
    public class ClassUpgradeDefinition : ScriptableObject
    {
        [Header("Classification")]
        public ClassType classType = ClassType.Knight;
        public MasteryPath path = MasteryPath.Path1;
        public MasteryTier tier = MasteryTier.N1;

        [Header("Display Info")]
        public string upgradeTitle = "Upgrade Title";
        [TextArea(2, 4)]
        public string description = "Upgrade Description";
        public string visualPreviewText = "Visual: Description";

        [Header("Stat Bonuses")]
        public float moveSpeedBonusPercent = 0f;
        public float attackDamageBonusPercent = 0f;
        public float maxHpBonusFlat = 0f;
        public float attackSpeedBonusPercent = 0f;

        [Header("Attack Names & Passives for UI")]
        public string basicAttackName = "";
        public string chargedAttackName = "";
        public string specialPassiveName = "";

        [Header("Combat Overrides")]
        public AttackProfileDefinition basicAttack;
        public AttackProfileDefinition chargedAttack;
        public AbilityDefinition specialAbility;
    }
}
