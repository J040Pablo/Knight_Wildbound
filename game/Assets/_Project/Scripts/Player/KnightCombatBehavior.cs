using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;
using Roguelite.Progression;

namespace Roguelite.Player
{
    public class KnightCombatBehavior : ICombatBehavior, IClassCombatBehavior
    {
        private PlayerCombat playerCombat;
        private PlayerStats playerStats;

        public void Initialize(PlayerCombat combat, PlayerStats stats)
        {
            playerCombat = combat;
            playerStats = stats;
        }

        public void UpdateBehavior() { }

        public void BasicAttack(Vector3 aimDirection)
        {
            ExecuteBasicAttack(aimDirection);
        }

        public void ChargedAttack(Vector3 aimDirection, float chargeRatio)
        {
            ExecuteChargedAttack(aimDirection, chargeRatio);
        }

        public bool CanUseAbility()
        {
            SpecialAbilitySystem abilitySystem = playerCombat != null ? playerCombat.GetComponent<SpecialAbilitySystem>() : null;
            return abilitySystem != null && abilitySystem.CanUseAbility();
        }

        public void UseAbility()
        {
            SpecialAbilitySystem abilitySystem = playerCombat != null ? playerCombat.GetComponent<SpecialAbilitySystem>() : null;
            if (abilitySystem != null)
            {
                abilitySystem.TriggerAbility(playerCombat.transform.forward);
            }
        }

        public void ExecuteBasicAttack(Vector3 aimDirection)
        {
            if (playerCombat == null) return;

            AttackProfileDefinition profile = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentBasicAttack : null;

            float damageMultiplier = profile != null ? profile.damageMultiplier : 1.0f;
            float attackDamage = playerCombat.BaseDamage * damageMultiplier;
            float attackRange = profile != null ? profile.attackRange : 2.4f;
            float knockback = profile != null ? profile.knockbackForce : 4.0f;

            Collider[] hits = Physics.OverlapSphere(playerCombat.transform.position + aimDirection * 1.2f, attackRange);
            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable damageable = hits[i].GetComponent<IDamageable>();
                if (damageable == null) damageable = hits[i].GetComponentInParent<IDamageable>();

                if (damageable != null && !damageable.IsDead)
                {
                    DamageInfo info = new DamageInfo(attackDamage, aimDirection, knockback, false, playerCombat.gameObject);
                    damageable.TakeDamage(info);
                }
            }
        }

        public void ExecuteChargedAttack(Vector3 aimDirection, float chargeRatio)
        {
            if (playerCombat == null) return;

            AttackProfileDefinition profile = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentChargedAttack : null;

            float baseMult = profile != null ? profile.damageMultiplier : 1.5f;
            float attackDamage = playerCombat.BaseDamage * (baseMult + chargeRatio * 1.0f);
            float attackRange = profile != null ? profile.attackRange : 3.5f;
            float knockback = profile != null ? profile.knockbackForce : 10.0f;

            // Sword N3 Empowered: Launch Energy Wave on Charged Attack
            MasteryTier swordTier = ProgressionManager.Instance != null ? ProgressionManager.Instance.GetTier(MasteryPath.Path2) : MasteryTier.None;
            bool launchesWave = (profile != null && profile.launchesEnergyWave) || (swordTier >= MasteryTier.N3);

            if (launchesWave)
            {
                LaunchEnergyWave(aimDirection, attackDamage * 0.8f);
            }

            Collider[] hits = Physics.OverlapSphere(playerCombat.transform.position, attackRange);
            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable damageable = hits[i].GetComponent<IDamageable>();
                if (damageable == null) damageable = hits[i].GetComponentInParent<IDamageable>();

                if (damageable != null && !damageable.IsDead)
                {
                    Vector3 knockDir = (hits[i].transform.position - playerCombat.transform.position).normalized;
                    DamageInfo info = new DamageInfo(attackDamage, knockDir, knockback, true, playerCombat.gameObject);
                    damageable.TakeDamage(info);
                }
            }
        }

        private void LaunchEnergyWave(Vector3 direction, float waveDamage)
        {
            Vector3 safeDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : playerCombat.transform.forward;
            if (safeDir.sqrMagnitude < 0.0001f) safeDir = Vector3.forward;
            Vector3 safeUp = Mathf.Abs(Vector3.Dot(safeDir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion waveRot = Quaternion.LookRotation(safeDir, safeUp);

            GameObject waveObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            waveObj.name = "KnightEnergyWave";
            waveObj.transform.position = playerCombat.transform.position + Vector3.up * 1.0f + safeDir * 1.2f;
            waveObj.transform.rotation = waveRot;
            waveObj.transform.localScale = new Vector3(2.5f, 0.4f, 0.6f);

            Renderer r = waveObj.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.2f, 0.8f, 1.0f, 0.8f);
            }

            Collider col = waveObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            EnergyWaveProjectile proj = waveObj.AddComponent<EnergyWaveProjectile>();
            proj.Initialize(direction, waveDamage, playerCombat.gameObject);
        }
    }

    public class EnergyWaveProjectile : MonoBehaviour
    {
        private Vector3 moveDir;
        private float damage;
        private GameObject attacker;
        private float speed = 18f;
        private float lifetime = 1.2f;

        public void Initialize(Vector3 dir, float dmg, GameObject owner)
        {
            moveDir = dir.normalized;
            damage = dmg;
            attacker = owner;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.position += moveDir * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null && !damageable.IsDead)
            {
                DamageInfo info = new DamageInfo(damage, moveDir, 6.0f, false, attacker);
                damageable.TakeDamage(info);
            }
        }
    }
}
