using UnityEngine;

namespace Roguelite.Data
{
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "Roguelite/Data/Character Data")]
    public class CharacterData : ScriptableObject
    {
        public string characterName = "Knight";
        public string description = "A sturdy melee warrior with sword attacks and high defense.";
        
        [Header("Base Stats")]
        public float baseMaxHP = 100f;
        public float baseMaxStamina = 100f;
        public float staminaRegenRate = 25f;
        public float baseMoveSpeed = 7f;
        public float sprintSpeedMultiplier = 1.5f;
        public float dodgeDistance = 6f;
        public float dodgeStaminaCost = 25f;
        public float dodgeCooldown = 0.8f;
        public float jumpForce = 8f;
        
        [Header("Combat Base Stats")]
        public float baseAttackDamage = 25f;
        public float baseAttackSpeed = 1f; // Attack interval multiplier (1 = normal)
        public float baseCritChance = 0.1f; // 10%
        public float critDamageMultiplier = 2.0f;
        
        [Header("Visuals")]
        public Color characterColor = new Color(0.2f, 0.5f, 0.9f);
    }
}
