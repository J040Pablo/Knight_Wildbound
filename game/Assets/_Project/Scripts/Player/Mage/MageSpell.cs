using UnityEngine;
using Roguelite.Combat;
using Roguelite.Combat.StatusEffects;
using Roguelite.Enemy;

namespace Roguelite.Player.Mage
{
    public abstract class MageSpell
    {
        public MageAbilityDefinition Definition { get; protected set; }
        protected PlayerCombat playerCombat;
        protected PlayerStats playerStats;
        protected float cooldownTimer = 0f;

        public bool IsOnCooldown => cooldownTimer > 0f;
        public float RemainingCooldown => Mathf.Max(0f, cooldownTimer);

        public virtual void Initialize(MageAbilityDefinition def, PlayerCombat combat, PlayerStats stats)
        {
            Definition = def;
            playerCombat = combat;
            playerStats = stats;
            cooldownTimer = 0f;
        }

        public abstract void Cast(Vector3 aimDirection, float chargeRatio);

        public virtual void UpdateSpell(float deltaTime)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= deltaTime;
            }
        }

        protected void StartCooldown()
        {
            float atkSpeed = playerStats != null ? playerStats.AttackSpeedMultiplier : 1.0f;
            cooldownTimer = (Definition != null ? Definition.cooldown : 0.5f) / Mathf.Max(0.1f, atkSpeed);
        }

        protected float CalculateDamage(float chargeRatio = 0f)
        {
            float baseAtk = playerCombat != null ? playerCombat.BaseDamage : 15.0f;
            float magMult = playerCombat != null ? playerCombat.MagicDamageMultiplier : 1.0f;
            float defMult = Definition != null ? Definition.damageMultiplier : 1.0f;

            float scaling = 1.0f;
            if (Definition != null && Definition.isCharged)
            {
                scaling = 1.0f + chargeRatio * 0.8f;
            }

            return baseAtk * magMult * defMult * scaling;
        }

        protected Vector3 GetSpawnPosition(Vector3 aimDirection)
        {
            if (playerCombat == null) return Vector3.up;
            return playerCombat.transform.position + aimDirection.normalized * 1.0f + Vector3.up * 1.2f;
        }

        protected Vector3 GetTargetDirection(Vector3 spawnPos)
        {
            if (playerCombat == null) return Vector3.forward;

            MageAimController aimController = playerCombat.GetComponent<MageAimController>();
            if (aimController != null)
            {
                return aimController.GetDirectionToTarget(spawnPos);
            }

            Vector3 targetPt = playerCombat.GetReticleTargetWorldPosition();
            Vector3 dir = (targetPt - spawnPos);
            if (dir.sqrMagnitude < 0.0001f) return playerCombat.transform.forward;
            return dir.normalized;
        }

        protected StatusEffectReceiver GetStatusReceiver(EnemyBase enemy)
        {
            if (enemy == null) return null;
            return enemy.GetOrCreateStatusReceiver();
        }
    }
}
