using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Player
{
    public class MageCombatBehavior : ICombatBehavior
    {
        private PlayerCombat playerCombat;
        private PlayerStats playerStats;

        public void Initialize(PlayerCombat combat, PlayerStats stats)
        {
            playerCombat = combat;
            playerStats = stats;
        }

        public void UpdateBehavior() { }

        public void ExecuteBasicAttack(Vector3 aimDirection)
        {
            // Magic Bolt
            Vector3 spawnPos = playerCombat.transform.position + aimDirection * 1.0f + Vector3.up * 1.2f;
            GameObject boltObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boltObj.name = "MagicBolt";
            boltObj.transform.position = spawnPos;
            boltObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            SphereCollider col = boltObj.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            MagicProjectile proj = boltObj.AddComponent<MagicProjectile>();
            float speed = 22.0f * playerCombat.ProjectileSpeedMultiplier;
            float damage = playerCombat.BaseDamage * playerCombat.MagicDamageMultiplier * 1.2f;
            proj.Initialize(playerCombat.gameObject, aimDirection, damage, speed, false, 0f, 4.0f, new Color(0.6f, 0.3f, 1.0f));
        }

        public void ExecuteChargedAttack(Vector3 aimDirection, float chargeRatio)
        {
            // Fireball AoE
            Vector3 spawnPos = playerCombat.transform.position + aimDirection * 1.2f + Vector3.up * 1.2f;
            GameObject fireballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireballObj.name = "Fireball_Charged";
            fireballObj.transform.position = spawnPos;
            float scale = 1.0f + chargeRatio * 0.6f;
            fireballObj.transform.localScale = new Vector3(scale, scale, scale);

            SphereCollider col = fireballObj.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            MagicProjectile proj = fireballObj.AddComponent<MagicProjectile>();
            float speed = 16.0f * playerCombat.ProjectileSpeedMultiplier;
            float damage = playerCombat.BaseDamage * playerCombat.MagicDamageMultiplier * (1.8f + chargeRatio * 1.2f);
            float aoeRadius = 3.5f * playerCombat.SpellAreaMultiplier;

            proj.Initialize(playerCombat.gameObject, aimDirection, damage, speed, true, aoeRadius, 9.0f, new Color(1.0f, 0.4f, 0.1f));
        }
    }
}
