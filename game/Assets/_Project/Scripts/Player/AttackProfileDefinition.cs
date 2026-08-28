using UnityEngine;

namespace Roguelite.Player
{
    [CreateAssetMenu(fileName = "NewAttackProfile", menuName = "Roguelite/Combat/Attack Profile Definition")]
    public class AttackProfileDefinition : ScriptableObject
    {
        [Header("Profile Info")]
        public string attackName = "Basic Attack";
        public float damageMultiplier = 1.0f;
        public float attackRange = 2.5f;
        public float attackAngle = 120f;
        public float knockbackForce = 4.0f;
        public float staminaCost = 10f;
        public float cooldown = 0.4f;

        [Header("Special Modifiers")]
        public bool launchesEnergyWave = false;
        public bool hasHyperArmor = false;
        public bool causesKnockdown = false;

        [Header("Visual Effects")]
        public GameObject visualVfxPrefab;
    }
}
