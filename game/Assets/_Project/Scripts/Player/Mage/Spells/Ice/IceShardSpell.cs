using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Player.Mage.Spells.Ice
{
    public class IceShardSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 baseDir = GetTargetDirection(spawnPos);

            float[] angles = new float[] { -14f, 0f, 14f };
            float damage = CalculateDamage(chargeRatio) * 0.75f;

            foreach (float angle in angles)
            {
                Vector3 dir = Quaternion.Euler(0, angle, 0) * baseDir;
                Vector3 dirNorm = dir.sqrMagnitude > 0.001f ? dir.normalized : playerCombat.transform.forward;
                GameObject shardObj = MageObjectPool.Instance.GetPrimitiveSphere("IceShard", Definition.primaryColor, new Vector3(0.35f, 0.35f, 0.7f));
                shardObj.transform.position = spawnPos;

                Vector3 up = Mathf.Abs(Vector3.Dot(dirNorm, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
                Quaternion rot = Quaternion.LookRotation(dirNorm, up);
                rot.Normalize();
                shardObj.transform.rotation = rot;

                MagicProjectile proj = shardObj.GetComponent<MagicProjectile>();
                if (proj == null) proj = shardObj.AddComponent<MagicProjectile>();

                proj.Initialize(playerCombat.gameObject, dirNorm, damage, 20.0f, false, 0f, 3.0f, Definition.primaryColor);
            }
        }
    }
}
