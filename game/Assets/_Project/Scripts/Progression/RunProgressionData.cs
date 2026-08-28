using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Progression
{
    [Serializable]
    public class RunProgressionData
    {
        public int saveVersion = 1;
        public ClassType currentClass = ClassType.None;
        public int currentLevel = 1;
        public int currentLevelXP = 0;
        public int totalXP = 0;

        public MasteryTier path1Tier = MasteryTier.None;
        public MasteryTier path2Tier = MasteryTier.None;
        public MasteryTier path3Tier = MasteryTier.None;

        public List<AbilityId> unlockedAbilities = new List<AbilityId>();
        public List<string> unlockedUpgradeTitles = new List<string>();
    }
}
