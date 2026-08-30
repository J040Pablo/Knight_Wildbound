using UnityEngine;

namespace Roguelite.Player.Mage
{
    [CreateAssetMenu(fileName = "NewMageAbility", menuName = "Roguelite/Mage/Mage Ability Definition")]
    public class MageAbilityDefinition : ScriptableObject
    {
        [Header("Classification")]
        public MageSpellId spellId = MageSpellId.None;
        public string spellName = "New Spell";
        [TextArea(2, 4)]
        public string description = "Spell Description";
        public MageBuildType buildType = MageBuildType.Elemental;
        public MageTier tier = MageTier.N1;
        public bool isCharged = false;

        [Header("Combat Parameters")]
        public float damageMultiplier = 1.0f;
        public float chargeTimeRequired = 1.0f;
        public float cooldown = 0.5f;
        public float projectileSpeed = 22.0f;
        public float areaRadius = 3.5f;
        public float duration = 4.0f;
        public float knockbackForce = 5.0f;
        public float staminaCost = 10.0f;

        [Header("Visual & VFX")]
        public Color primaryColor = Color.cyan;
        public Color secondaryColor = Color.white;
        public Sprite spellIcon;

        [Header("Special Tuning Parameters")]
        public float customValue1 = 0f; // e.g. chain chance or slow amount or pierce count
        public float customValue2 = 0f; // e.g. DoT dps or pull force
    }
}
