using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Player.Mage.Spells.Cosmic
{
    public class SupernovaSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 travelDir = GetTargetDirection(spawnPos);

            float scale = 0.9f + chargeRatio * 1.0f;

            GameObject novaObj = MageObjectPool.Instance.GetPrimitiveSphere("Supernova_Charged", Definition.primaryColor, new Vector3(scale, scale, scale));
            novaObj.transform.position = spawnPos;

            MagicProjectile proj = novaObj.GetComponent<MagicProjectile>();
            if (proj == null) proj = novaObj.AddComponent<MagicProjectile>();

            float damage = CalculateDamage(chargeRatio);
            float radius = 4.0f + chargeRatio * 1.5f;

            proj.Initialize(playerCombat.gameObject, travelDir, damage, 20.0f, true, radius, 9.0f, Definition.primaryColor);
        }
    }
}
