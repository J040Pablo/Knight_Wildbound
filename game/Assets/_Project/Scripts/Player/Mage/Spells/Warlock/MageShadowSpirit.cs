using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage.Spells.Warlock
{
    public class MageShadowSpirit : MonoBehaviour
    {
        private GameObject owner;
        private float damage = 15f;
        private float lifetime = 6.0f;
        private float attackTimer = 0f;

        public void Initialize(GameObject ownerObj, float spiritDamage, float duration)
        {
            owner = ownerObj;
            damage = spiritDamage;
            lifetime = duration;

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            attackTimer -= Time.deltaTime;

            // Seek nearest enemy
            EnemyBase nearest = FindNearestEnemy();
            if (nearest != null && !nearest.IsDead)
            {
                Vector3 dir = (nearest.transform.position - transform.position);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Vector3 dirNorm = dir.normalized;
                    transform.position += dirNorm * 7.0f * Time.deltaTime;
                    if (dirNorm.sqrMagnitude > 0.0001f)
                    {
                        Quaternion rot = Quaternion.LookRotation(dirNorm, Vector3.up);
                        rot.Normalize();
                        transform.rotation = rot;
                    }
                }

                if (Vector3.Distance(transform.position, nearest.transform.position) <= 1.8f && attackTimer <= 0f)
                {
                    attackTimer = 1.0f;
                    DamageInfo info = new DamageInfo(damage, dir.normalized, 3.0f, false, owner);
                    nearest.TakeDamage(info);
                    MageVFXHelper.CreateImpactExplosion(nearest.transform.position + Vector3.up * 1.0f, 0.6f, new Color(0.5f, 0.1f, 0.6f), 0.2f);
                }
            }
        }

        private EnemyBase FindNearestEnemy()
        {
            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            Collider[] hits = Physics.OverlapSphere(transform.position, 15.0f, mask);
            EnemyBase closest = null;
            float minDst = 999f;

            foreach (var col in hits)
            {
                if (col == null || (owner != null && (col.gameObject == owner || col.transform.IsChildOf(owner.transform))) || col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) continue;

                EnemyBase enemy = col.GetComponent<EnemyBase>();
                if (enemy == null) enemy = col.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    float dst = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dst < minDst)
                    {
                        minDst = dst;
                        closest = enemy;
                    }
                }
            }
            return closest;
        }
    }
}
