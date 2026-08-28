using UnityEngine;

namespace Roguelite.Player
{
    public interface IClassCombatBehavior
    {
        void BasicAttack(Vector3 aimDirection);
        void ChargedAttack(Vector3 aimDirection, float chargeRatio);
        bool CanUseAbility();
        void UseAbility();
    }
}
