using UnityEngine;
using Roguelite.Combat;
using Roguelite.Player.Mage;

namespace Roguelite.Player
{
    public class MageCombatBehavior : ICombatBehavior
    {
        private PlayerCombat playerCombat;
        private PlayerStats playerStats;
        private MageAbilityController abilityController;

        public void Initialize(PlayerCombat combat, PlayerStats stats)
        {
            playerCombat = combat;
            playerStats = stats;

            if (playerCombat != null)
            {
                abilityController = playerCombat.GetComponent<MageAbilityController>();
                if (abilityController == null)
                {
                    abilityController = playerCombat.gameObject.AddComponent<MageAbilityController>();
                }
                abilityController.Initialize(playerCombat, playerStats);
            }
        }

        public void UpdateBehavior()
        {
            if (abilityController != null)
            {
                abilityController.UpdateController();
            }
        }

        public void UpdateChargeFeedback(float chargeRatio)
        {
            if (abilityController != null)
            {
                abilityController.UpdateChargeFeedback(chargeRatio);
            }
        }

        public void StopChargeFeedback()
        {
            if (abilityController != null)
            {
                abilityController.StopChargeFeedback();
            }
        }

        public void ExecuteBasicAttack(Vector3 aimDirection)
        {
            if (abilityController != null)
            {
                abilityController.ExecuteBasicAttack(aimDirection);
            }
        }

        public void ExecuteChargedAttack(Vector3 aimDirection, float chargeRatio)
        {
            if (abilityController != null)
            {
                abilityController.ExecuteChargedAttack(aimDirection, chargeRatio);
            }
        }
    }
}
