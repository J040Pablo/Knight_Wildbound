using UnityEngine;

namespace Roguelite.Combat
{
    public struct DamageInfo
    {
        public float amount;
        public Vector3 knockbackDirection;
        public float knockbackForce;
        public bool isCritical;
        public GameObject attacker;

        public DamageInfo(float amount, Vector3 knockbackDirection, float knockbackForce, bool isCritical = false, GameObject attacker = null)
        {
            this.amount = amount;
            this.knockbackDirection = knockbackDirection;
            this.knockbackForce = knockbackForce;
            this.isCritical = isCritical;
            this.attacker = attacker;
        }
    }
}
