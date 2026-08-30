using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Player.Mage.Spells.Cosmic
{
    public class StarSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 travelDir = GetTargetDirection(spawnPos);

            GameObject starObj = MageObjectPool.Instance.GetPrimitiveSphere("StarProjectile", Definition.primaryColor, new Vector3(0.45f, 0.45f, 0.45f));
            starObj.transform.position = spawnPos;

            MagicProjectile proj = starObj.GetComponent<MagicProjectile>();
            if (proj == null) proj = starObj.AddComponent<MagicProjectile>();

            float damage = CalculateDamage(chargeRatio);
            float speed = Definition != null ? Definition.projectileSpeed : 24.0f;

            proj.Initialize(playerCombat.gameObject, travelDir, damage, speed, false, 0f, 4.0f, Definition.primaryColor);
        }
    }
}
