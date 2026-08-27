using UnityEngine;

namespace Roguelite.Data
{
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Roguelite/Data/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName = "Greatsword";
        
        [Header("Light Attack")]
        public float lightDamage = 25f;
        public float lightAttackRange = 2.5f;
        public float lightAttackAngle = 100f;
        public float lightAttackCooldown = 0.4f;
        public float lightKnockbackForce = 5f;
        public float lightStaminaCost = 10f;
        
        [Header("Charged Attack")]
        public float chargedDamage = 60f;
        public float chargedAttackRange = 4.0f;
        public float chargedAttackAngle = 160f;
        public float chargeTimeRequired = 1.0f;
        public float chargedKnockbackForce = 12f;
        public float chargedStaminaCost = 30f;
    }
}
