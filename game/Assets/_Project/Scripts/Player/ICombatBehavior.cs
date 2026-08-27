using UnityEngine;

namespace Roguelite.Player
{
    public interface ICombatBehavior
    {
        void Initialize(PlayerCombat playerCombat, PlayerStats playerStats);
        void ExecuteBasicAttack(Vector3 aimDirection);
        void ExecuteChargedAttack(Vector3 aimDirection, float chargeRatio);
        void UpdateBehavior();
    }
}
