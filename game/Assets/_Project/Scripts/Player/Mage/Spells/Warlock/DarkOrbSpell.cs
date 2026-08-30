using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Player.Mage.Spells.Warlock
{
    public class DarkOrbSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 travelDir = GetTargetDirection(spawnPos);

            GameObject darkOrbObj = MageObjectPool.Instance.GetPrimitiveSphere("DarkOrb", Definition.primaryColor, new Vector3(0.5f, 0.5f, 0.5f));
            darkOrbObj.transform.position = spawnPos;

            MagicProjectile proj = darkOrbObj.GetComponent<MagicProjectile>();
            if (proj == null) proj = darkOrbObj.AddComponent<MagicProjectile>();

            float damage = CalculateDamage(chargeRatio);
            float speed = Definition != null ? Definition.projectileSpeed : 22.0f;

            proj.Initialize(playerCombat.gameObject, travelDir, damage, speed, true, 1.8f, 5.0f, Definition.primaryColor);
        }
    }
}
