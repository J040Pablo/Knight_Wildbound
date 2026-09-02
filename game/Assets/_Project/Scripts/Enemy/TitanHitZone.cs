using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Enemy
{
    public enum TitanHitZoneType
    {
        LeftLeg,
        RightLeg,
        Torso,
        LeftArm,
        RightArm,
        Head,
        NapeCrystal
    }

    /// <summary>
    /// Component attached to specific colliders of the Ancient Stone Titan.
    /// Routes hit detection and applies zone-specific damage reduction rules:
    /// - Legs & Arms: 0.30x damage.
    /// - Torso & Head: 0.10x damage (90% body armor reduction).
    /// - Nape Crystal: Full critical damage + crystal exposure progression.
    /// </summary>
    public class TitanHitZone : MonoBehaviour
    {
        public AncientStoneTitanAI parentTitan;
        public TitanHitZoneType zoneType = TitanHitZoneType.Torso;
        public float damageMultiplier = 0.10f;

        public void Initialize(AncientStoneTitanAI titan, TitanHitZoneType type)
        {
            parentTitan = titan;
            zoneType = type;

            switch (zoneType)
            {
                case TitanHitZoneType.LeftLeg:
                case TitanHitZoneType.RightLeg:
                case TitanHitZoneType.LeftArm:
                case TitanHitZoneType.RightArm:
                    damageMultiplier = 0.30f;
                    break;

                case TitanHitZoneType.Torso:
                case TitanHitZoneType.Head:
                    damageMultiplier = 0.10f;
                    break;

                case TitanHitZoneType.NapeCrystal:
                    damageMultiplier = 2.0f;
                    break;
            }
        }

        public void ProcessHit(DamageInfo info)
        {
            if (parentTitan == null || parentTitan.IsDead) return;

            DamageInfo modifiedInfo = info;
            modifiedInfo.amount = info.amount * damageMultiplier;

            parentTitan.ProcessZoneDamage(modifiedInfo, zoneType);
        }
    }
}
