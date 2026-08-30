using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Player
{
    public class DruidCombatBehavior : ICombatBehavior
    {
        private PlayerCombat playerCombat;
        private PlayerStats playerStats;

        private float blessingCooldownTimer = 0f;
        private float blessingTickTimer = 0f;
        private bool isBlessingActive = false;
        private int blessingTicksRemaining = 0;

        public void Initialize(PlayerCombat combat, PlayerStats stats)
        {
            playerCombat = combat;
            playerStats = stats;
        }

        public void UpdateBehavior()
        {
            // Nature's Blessing Passive Logic
            if (blessingCooldownTimer > 0)
            {
                blessingCooldownTimer -= Time.deltaTime;
            }

            if (!isBlessingActive && blessingCooldownTimer <= 0 && playerStats != null && !playerStats.IsDead)
            {
                if (playerStats.CurrentHP < playerStats.MaxHP * 0.5f)
                {
                    isBlessingActive = true;
                    blessingTicksRemaining = 5; // 5 ticks of healing
                    blessingTickTimer = 0.5f;
                }
            }

            if (isBlessingActive)
            {
                blessingTickTimer -= Time.deltaTime;
                if (blessingTickTimer <= 0)
                {
                    blessingTickTimer = 1.2f;
                    blessingTicksRemaining--;

                    float healAmount = 5.0f * playerCombat.HealingEfficiencyMultiplier;
                    playerStats.Heal(healAmount);

                    if (blessingTicksRemaining <= 0)
                    {
                        isBlessingActive = false;
                        blessingCooldownTimer = 14.0f; // 14s internal cooldown
                    }
                }
            }
        }

        public void ExecuteBasicAttack(Vector3 aimDirection)
        {
            // Nature Projectile
            Vector3 spawnPos = playerCombat.transform.position + aimDirection * 1.0f + Vector3.up * 1.2f;
            GameObject boltObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boltObj.name = "NatureProjectile";
            boltObj.transform.position = spawnPos;
            boltObj.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

            SphereCollider col = boltObj.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            MagicProjectile proj = boltObj.AddComponent<MagicProjectile>();
            float speed = 20.0f * playerCombat.ProjectileSpeedMultiplier;
            float damage = playerCombat.BaseDamage * 1.1f;
            proj.Initialize(playerCombat.gameObject, aimDirection, damage, speed, false, 0f, 3.0f, new Color(0.2f, 0.85f, 0.3f));
        }

        public void ExecuteChargedAttack(Vector3 aimDirection, float chargeRatio)
        {
            // Nature Burst (Damage + Slow + Self-Heal)
            Vector3 centerPos = playerCombat.transform.position + aimDirection * 2.0f;
            centerPos.y = 0.1f;

            float radius = 4.0f * playerCombat.SpellAreaMultiplier;
            float damage = playerCombat.BaseDamage * (1.4f + chargeRatio * 1.0f);

            // Create Nature Burst Indicator / Zone Visual
            GameObject zoneObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            zoneObj.name = "NatureBurstZone";
            zoneObj.transform.position = centerPos;
            zoneObj.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
            Object.Destroy(zoneObj.GetComponent<Collider>());

            Renderer r = zoneObj.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.2f, 0.8f, 0.3f, 0.5f);

            int mask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (mask == 0) mask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            Collider[] hits = Physics.OverlapSphere(centerPos, radius, mask);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null || hits[i].gameObject == playerCombat.gameObject || hits[i].transform.IsChildOf(playerCombat.transform)) continue;
                if (hits[i].CompareTag("Player") || hits[i].gameObject.layer == LayerMask.NameToLayer("Player")) continue;

                EnemyBase enemy = hits[i].GetComponent<EnemyBase>();
                if (enemy == null) enemy = hits[i].GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    DamageInfo info = new DamageInfo(damage, (enemy.transform.position - centerPos).normalized, 4.0f, false, playerCombat.gameObject);
                    enemy.TakeDamage(info);
                }
            }

            // Slight self-heal for Druid on burst launch
            float instantHeal = (8.0f + chargeRatio * 6.0f) * playerCombat.HealingEfficiencyMultiplier;
            playerStats.Heal(instantHeal);

            Object.Destroy(zoneObj, 0.6f);
        }
    }
}
