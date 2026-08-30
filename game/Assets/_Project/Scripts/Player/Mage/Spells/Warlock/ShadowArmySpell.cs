using UnityEngine;

namespace Roguelite.Player.Mage.Spells.Warlock
{
    public class ShadowArmySpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 portalPos = playerCombat.GetReticleTargetWorldPosition() + Vector3.up * 0.1f;

            // Dark Portal ground VFX
            MageVFXHelper.CreatePortalRing(portalPos, Vector3.up, Definition.primaryColor, 3.0f, 3.0f);

            float spiritDmg = CalculateDamage(chargeRatio) * 0.5f;

            // Spawn 3 Shadow Spirits
            for (int i = 0; i < 3; i++)
            {
                Vector3 spawnOffset = Quaternion.Euler(0, i * 120f, 0) * Vector3.forward * 0.8f;
                GameObject spiritObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spiritObj.name = "ShadowSpirit_Summon";
                spiritObj.transform.position = portalPos + spawnOffset + Vector3.up * 0.8f;
                spiritObj.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);

                var col = spiritObj.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                var rend = spiritObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = Definition.primaryColor;
                }

                MageShadowSpirit spiritComp = spiritObj.AddComponent<MageShadowSpirit>();
                spiritComp.Initialize(playerCombat.gameObject, spiritDmg, 6.0f);
            }
        }
    }
}
