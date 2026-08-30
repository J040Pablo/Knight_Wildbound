using UnityEngine;

namespace Roguelite.Player.Mage
{
    [CreateAssetMenu(fileName = "NewMageBuild", menuName = "Roguelite/Mage/Mage Build Definition")]
    public class MageBuildDefinition : ScriptableObject
    {
        [Header("Build Info")]
        public MageBuildType buildType = MageBuildType.Elemental;
        public string buildName = "Elemental Mage";
        [TextArea(2, 4)]
        public string fantasyDescription = "Master of elemental magic evolving from Fire to Lightning and Ice.";
        public Color themeColor = new Color(1.0f, 0.5f, 0.1f);

        [Header("N1 Tier Spells")]
        public MageAbilityDefinition n1Basic;
        public MageAbilityDefinition n1Charged;

        [Header("N2 Tier Spells")]
        public MageAbilityDefinition n2Basic;
        public MageAbilityDefinition n2Charged;

        [Header("N3 Tier Spells")]
        public MageAbilityDefinition n3Basic;
        public MageAbilityDefinition n3Charged;

        public MageAbilityDefinition GetAbility(MageTier tier, bool isCharged)
        {
            switch (tier)
            {
                case MageTier.N1:
                    return isCharged ? n1Charged : n1Basic;
                case MageTier.N2:
                    return isCharged ? n2Charged : n2Basic;
                case MageTier.N3:
                    return isCharged ? n3Charged : n3Basic;
                default:
                    return isCharged ? n1Charged : n1Basic;
            }
        }
    }
}
