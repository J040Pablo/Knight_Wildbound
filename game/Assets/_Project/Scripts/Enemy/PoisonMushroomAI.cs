using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Loot;

namespace Roguelite.Enemy
{
    public class PoisonMushroomAI : EnemyBase
    {
        [Header("Mushroom Settings")]
        [SerializeField] private float sporeRadius = 5.0f;
        [SerializeField] private float sporeDamage = 4.0f;
        [SerializeField] private float sporeCooldown = 5.0f;

        private float sporeTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
            if (enemyData != null)
            {
                MaxHP = 60f;
                CurrentHP = MaxHP;
            }
        }

        protected override void Start()
        {
            base.Start();
            if (meshRenderer != null)
            {
                meshRenderer.material.color = new Color(0.85f, 0.15f, 0.25f); // Toxic Crimson Red
            }
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || playerTransform == null || playerStats.IsDead || isAttacking || !SafeCanMove()) return;

            sporeTimer -= Time.deltaTime;
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist <= sporeRadius && sporeTimer <= 0f)
            {
                StartCoroutine(PerformSporeBurst());
            }
        }

        private IEnumerator PerformSporeBurst()
        {
            isAttacking = true;
            sporeTimer = sporeCooldown;

            // Telegraph Marker
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "SporeTelegraph";
            Collider mCol = marker.GetComponent<Collider>();
            if (mCol != null) Destroy(mCol);

            marker.transform.position = transform.position + new Vector3(0, 0.05f, 0);
            marker.transform.localScale = new Vector3(sporeRadius * 2f, 0.02f, sporeRadius * 2f);
            Renderer mR = marker.GetComponent<Renderer>();
            if (mR != null) mR.material.color = new Color(0.2f, 0.9f, 0.3f, 0.5f);

            yield return new WaitForSeconds(0.8f);
            Destroy(marker);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= sporeRadius)
                {
                    PoisonStatusEffect poison = playerStats.GetComponent<PoisonStatusEffect>();
                    if (poison == null) poison = playerStats.gameObject.AddComponent<PoisonStatusEffect>();
                    poison.ApplyPoison(6f, sporeDamage);
                }
            }

            yield return new WaitForSeconds(0.5f);
            isAttacking = false;
        }

        protected override void Die()
        {
            if (IsDead) return;

            // Spawn Toxic Explosion Cloud on death
            GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cloud.name = "ToxicExplosionCloud";
            cloud.transform.position = transform.position + new Vector3(0, 0.1f, 0);
            cloud.transform.localScale = new Vector3(sporeRadius * 2f, 0.1f, sporeRadius * 2f);

            Collider col = cloud.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer r = cloud.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.15f, 0.85f, 0.25f, 0.45f);

            Destroy(cloud, 6.0f);

            LootResult loot = LootTable.ForMushroom();
            LootDrop.SpawnFromResult(loot, transform.position);

            base.Die();
        }
    }
}
