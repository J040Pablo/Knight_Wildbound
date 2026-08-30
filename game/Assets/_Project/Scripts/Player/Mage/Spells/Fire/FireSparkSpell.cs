using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Player.Mage.Spells.Fire
{
    public class FireSparkSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 travelDir = GetTargetDirection(spawnPos);

            GameObject sparkObj = MageObjectPool.Instance.GetPrimitiveSphere("FireSpark", Definition.primaryColor, new Vector3(0.45f, 0.45f, 0.45f));
            sparkObj.transform.position = spawnPos;

            MagicProjectile proj = sparkObj.GetComponent<MagicProjectile>();
            if (proj == null) proj = sparkObj.AddComponent<MagicProjectile>();

            float damage = CalculateDamage(chargeRatio);
            float speed = Definition != null ? Definition.projectileSpeed : 26.0f;

            proj.Initialize(playerCombat.gameObject, travelDir, damage, speed, true, 1.4f, 4.0f, Definition.primaryColor);
        }
    }
}
