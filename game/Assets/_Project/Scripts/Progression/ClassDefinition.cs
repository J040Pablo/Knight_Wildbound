using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Progression
{
    [CreateAssetMenu(fileName = "NewClassDefinition", menuName = "Roguelite/Progression/Class Definition")]
    public class ClassDefinition : ScriptableObject
    {
        [Header("Class Type")]
        public ClassType classType = ClassType.Knight;
        public string className = "Knight";

        [Header("Path Names")]
        public string path1Name = "Helmet";
        public string path1Abbrev = "HEL";

        public string path2Name = "Sword";
        public string path2Abbrev = "SW";

        public string path3Name = "Armor";
        public string path3Abbrev = "ARM";

        [Header("Upgrade Definitions")]
        public List<ClassUpgradeDefinition> upgrades = new List<ClassUpgradeDefinition>();

        public string GetPathAbbrev(MasteryPath path)
        {
            switch (path)
            {
                case MasteryPath.Path1: return string.IsNullOrEmpty(path1Abbrev) ? "P1" : path1Abbrev;
                case MasteryPath.Path2: return string.IsNullOrEmpty(path2Abbrev) ? "P2" : path2Abbrev;
                case MasteryPath.Path3: return string.IsNullOrEmpty(path3Abbrev) ? "P3" : path3Abbrev;
                default: return "P";
            }
        }

        public string GetPathName(MasteryPath path)
        {
            switch (path)
            {
                case MasteryPath.Path1: return string.IsNullOrEmpty(path1Name) ? "Path 1" : path1Name;
                case MasteryPath.Path2: return string.IsNullOrEmpty(path2Name) ? "Path 2" : path2Name;
                case MasteryPath.Path3: return string.IsNullOrEmpty(path3Name) ? "Path 3" : path3Name;
                default: return "Path";
            }
        }
    }
}
