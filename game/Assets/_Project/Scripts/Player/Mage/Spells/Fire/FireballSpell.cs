using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Player.Mage.Spells.Fire
{
    public class FireballSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 travelDir = GetTargetDirection(spawnPos);

            float scale = 0.8f + chargeRatio * 0.9f;

            GameObject fireballObj = MageObjectPool.Instance.GetPrimitiveSphere("Fireball_Charged", Definition.primaryColor, new Vector3(scale, scale, scale));
            fireballObj.transform.position = spawnPos;

            MagicProjectile proj = fireballObj.GetComponent<MagicProjectile>();
            if (proj == null) proj = fireballObj.AddComponent<MagicProjectile>();

            float damage = CalculateDamage(chargeRatio);
            float speed = Definition != null ? Definition.projectileSpeed : 18.0f;
            float radius = 3.5f + chargeRatio * 1.5f;

            proj.Initialize(playerCombat.gameObject, travelDir, damage, speed, true, radius, 8.0f, Definition.primaryColor);

            MageVFXHelper.CreateGroundRune(spawnPos, 1.0f, new Color(1.0f, 0.3f, 0.0f, 0.4f), 0.5f);
        }
    }
}
