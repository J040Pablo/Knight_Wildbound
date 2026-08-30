using UnityEngine;
using Roguelite.Combat;
using Roguelite.Combat.StatusEffects;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Warlock
{
    public class CurseMarkSpell : MageSpell
    {
        public override void Cast(Vector3 aimDirection, float chargeRatio)
        {
            if (IsOnCooldown) return;
            StartCooldown();

            Vector3 spawnPos = GetSpawnPosition(aimDirection);
            Vector3 travelDir = GetTargetDirection(spawnPos);

            GameObject boltObj = MageObjectPool.Instance.GetPrimitiveSphere("CurseBolt", Definition.primaryColor, new Vector3(0.4f, 0.4f, 0.4f));
            boltObj.transform.position = spawnPos;

            MagicProjectile proj = boltObj.GetComponent<MagicProjectile>();
            if (proj == null) proj = boltObj.AddComponent<MagicProjectile>();

            float damage = CalculateDamage(chargeRatio);
            proj.Initialize(playerCombat.gameObject, travelDir, damage, 24.0f, false, 0f, 2.0f, Definition.primaryColor);

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            RaycastHit[] hits = Physics.RaycastAll(spawnPos, travelDir, 35f, mask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == playerCombat.gameObject || hit.collider.transform.IsChildOf(playerCombat.transform)) continue;

                EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
                if (enemy == null) enemy = hit.collider.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    var receiver = GetStatusReceiver(enemy);
                    if (receiver != null)
                    {
                        receiver.ApplyEffect(new CurseStatusEffect(6.0f, 0.15f, 0.15f), playerCombat.gameObject, 5.0f);
                    }
                    break;
                }
            }
        }
    }
}
