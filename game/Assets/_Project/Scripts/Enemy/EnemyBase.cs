using System;
using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Data;
using Roguelite.Player;

namespace Roguelite.Enemy
{
    [RequireComponent(typeof(CharacterController))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Enemy Data-Driven Configuration")]
        [SerializeField] protected EnemyData enemyData;
        [SerializeField] protected EnemyDefinition enemyDefinition;

        public EnemyData EnemyData => enemyData;
        public EnemyDefinition EnemyDefinition => enemyDefinition;
        public EnemyRuntimeData RuntimeData { get; protected set; }

        public float CurrentHP
        {
            get => RuntimeData != null ? RuntimeData.CurrentHealth : currentHPInternal;
            protected set
            {
                if (RuntimeData != null) RuntimeData.CurrentHealth = value;
                currentHPInternal = value;
            }
        }

        public float MaxHP
        {
            get => RuntimeData != null ? RuntimeData.MaxHealth : maxHPInternal;
            protected set
            {
                if (RuntimeData != null) RuntimeData.MaxHealth = value;
                maxHPInternal = value;
            }
        }

        private float currentHPInternal = 50f;
        private float maxHPInternal = 50f;

        public bool IsDead { get; protected set; } = false;

        /// <summary>Override to true in boss-tier enemy AIs so the HUD shows the big boss health bar for them.</summary>
        public virtual bool IsBossEnemy => false;

        /// <summary>Override with a proper display name in boss-tier enemy AIs (used by the boss health bar).</summary>
        public virtual string DisplayName => gameObject.name.Replace("(Clone)", "").Trim();

        protected CharacterController characterController;
        protected Transform playerTransform;
        protected PlayerStats playerStats;
        protected Renderer meshRenderer;
        protected Color originalColor;
        protected bool isAttacking = false;
        protected float attackTimer = 0f;

        protected Vector3 knockbackVelocity;

        public event Action<EnemyBase> OnEnemyDied;

        public virtual void InitializeWithDefinition(EnemyDefinition def)
        {
            enemyDefinition = def;
            if (def != null)
            {
                RuntimeData = new EnemyRuntimeData(def);
                maxHPInternal = def.maxHealth;
                currentHPInternal = maxHPInternal;
                transform.localScale = def.modelScale;

                if (meshRenderer != null && def.enemyColor != Color.clear)
                {
                    meshRenderer.material.color = def.enemyColor;
                    originalColor = def.enemyColor;
                }
            }
        }

        protected virtual void Awake()
        {
            Quaternion rot = transform.rotation;
            float sqrMag = rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w;
            if (sqrMag < 0.99f || sqrMag > 1.01f)
            {
                transform.rotation = (sqrMag < 0.001f) ? Quaternion.identity : Quaternion.Normalize(rot);
            }

            characterController = GetComponent<CharacterController>();
            meshRenderer = GetComponentInChildren<Renderer>();

            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }

            if (enemyDefinition != null && RuntimeData == null)
            {
                InitializeWithDefinition(enemyDefinition);
            }
            else if (enemyData != null && RuntimeData == null)
            {
                MaxHP = enemyData.maxHealth;
                CurrentHP = MaxHP;
                transform.localScale = enemyData.modelScale;
                if (meshRenderer != null && enemyData.enemyColor != Color.clear)
                {
                    meshRenderer.material.color = enemyData.enemyColor;
                    originalColor = enemyData.enemyColor;
                }
            }
            else if (RuntimeData == null)
            {
                MaxHP = 50f;
                CurrentHP = 50f;
            }
        }

        protected virtual void Start()
        {
            FindPlayerTarget();
        }

        protected void FindPlayerTarget()
        {
            PlayerStats player = FindFirstObjectByType<PlayerStats>();
            if (player != null)
            {
                playerStats = player;
                playerTransform = player.transform;
            }
        }

        protected virtual void Update()
        {
            if (IsDead) return;

            if (playerTransform == null || playerStats == null)
            {
                FindPlayerTarget();
            }

            ApplyKnockbackDecay();
        }

        /// <summary>The attacker from the most recent TakeDamage call — used by Die() to tell
        /// on-kill relic effects (e.g. Bloomheart) whether the player landed the killing blow.</summary>
        protected GameObject lastDamageSource;

        public virtual void TakeDamage(DamageInfo damageInfo)
        {
            if (IsDead) return;

            if (damageInfo.attacker != null) lastDamageSource = damageInfo.attacker;

            CurrentHP = Mathf.Max(CurrentHP - damageInfo.amount, 0f);

            if (damageInfo.amount > 0f)
            {
                Roguelite.Core.Events.GameEvents.TriggerCombatText(
                    transform.position + Vector3.up * (IsBossEnemy ? 2.5f : 1.5f),
                    damageInfo.amount,
                    damageInfo.isCritical ? Roguelite.Core.Events.CombatTextType.Critical : Roguelite.Core.Events.CombatTextType.Normal);
            }

            // Apply Knockback
            if (damageInfo.knockbackForce > 0)
            {
                knockbackVelocity = damageInfo.knockbackDirection.normalized * damageInfo.knockbackForce;
            }

            // Visual Hit Flash
            StartCoroutine(FlashRed());

            if (CurrentHP <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashRed()
        {
            yield return FlashColor(Color.red);
        }

        private IEnumerator FlashColor(Color color)
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = color;
                yield return new WaitForSeconds(0.12f);
                meshRenderer.material.color = originalColor;
            }
        }

        public virtual void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            if (meshRenderer != null)
            {
                StartCoroutine(FlashColor(Color.green));
            }

            Roguelite.Core.Events.GameEvents.TriggerCombatText(
                transform.position + Vector3.up * (IsBossEnemy ? 2.5f : 1.5f),
                amount,
                Roguelite.Core.Events.CombatTextType.Heal);
        }

        public float MoveSpeedMultiplier { get; set; } = 1.0f;
        public float DamageOutputMultiplier { get; set; } = 1.0f;
        public bool IsStunnedOrFrozen { get; set; } = false;

        public void SetStatusModifiers(float speedMult, float damageMult, bool stunnedOrFrozen)
        {
            MoveSpeedMultiplier = speedMult;
            DamageOutputMultiplier = damageMult;
            IsStunnedOrFrozen = stunnedOrFrozen;
        }

        public Roguelite.Combat.StatusEffects.StatusEffectReceiver GetOrCreateStatusReceiver()
        {
            var receiver = GetComponent<Roguelite.Combat.StatusEffects.StatusEffectReceiver>();
            if (receiver == null)
            {
                receiver = gameObject.AddComponent<Roguelite.Combat.StatusEffects.StatusEffectReceiver>();
            }
            return receiver;
        }

        protected bool SafeCanMove()
        {
            return characterController != null && characterController.enabled && gameObject.activeInHierarchy && !IsStunnedOrFrozen;
        }

        protected virtual void ApplyKnockbackDecay()
        {
            if (knockbackVelocity.magnitude > 0.1f)
            {
                knockbackVelocity.y = 0f; // Force horizontal knockback only
                if (SafeCanMove())
                {
                    characterController.Move(knockbackVelocity * Time.deltaTime);
                }
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 8f);
            }
        }

        protected virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;

            // Give XP to player & ProgressionManager
            int xp = (enemyData != null) ? enemyData.xpReward : 10;
            if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddXP(xp);
            }
            else if (Progression.ProgressionManager.Instance != null)
            {
                Progression.ProgressionManager.Instance.AddXP(xp);
            }

            OnEnemyDied?.Invoke(this);

            // On-kill relic effects (e.g. Bloomheart) — no-ops if nothing relevant is equipped
            // or the kill wasn't the player's.
            Roguelite.Combat.OnHitRelicEffects.TryTriggerBloomheart(this, lastDamageSource);

            // Evaluate & Spawn Loot Drop
            SpawnEnemyLoot();

            // Simple shrink death effect
            StartCoroutine(ShrinkAndDestroy());
        }

        protected virtual void SpawnEnemyLoot()
        {
            Roguelite.Loot.LootResult result = null;
            string n = gameObject.name.ToLower();

            if (n.Contains("fairy"))
            {
                result = Roguelite.Loot.LootTable.ForFairy();
            }
            else if (n.Contains("mushroom"))
            {
                result = Roguelite.Loot.LootTable.ForMushroom();
            }
            else if (n.Contains("stonegiant") || n.Contains("stone_giant") || n.Contains("giant"))
            {
                result = Roguelite.Loot.LootTable.ForStoneGiant();
            }
            else
            {
                result = Roguelite.Loot.LootTable.Default();
            }

            Roguelite.Loot.LootDrop.SpawnFromResult(result, transform.position + Vector3.up * 0.5f);
        }

        private IEnumerator ShrinkAndDestroy()
        {
            float duration = 0.35f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
