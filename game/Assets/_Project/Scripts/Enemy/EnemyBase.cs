using System;
using System.Collections;
using UnityEngine;
using Roguelite.Data;
using Roguelite.Combat;
using Roguelite.Player;

namespace Roguelite.Enemy
{
    [RequireComponent(typeof(CharacterController))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Enemy Configuration")]
        [SerializeField] protected EnemyData enemyData;

        public EnemyData EnemyData => enemyData;
        public float CurrentHP { get; protected set; }
        public float MaxHP { get; protected set; }
        public bool IsDead { get; protected set; } = false;

        protected CharacterController characterController;
        protected Transform playerTransform;
        protected PlayerStats playerStats;
        protected Renderer meshRenderer;
        protected Color originalColor;
        protected bool isAttacking = false;
        protected float attackTimer = 0f;

        protected Vector3 knockbackVelocity;

        public event Action<EnemyBase> OnEnemyDied;

        protected virtual void Awake()
        {
            characterController = GetComponent<CharacterController>();
            meshRenderer = GetComponentInChildren<Renderer>();

            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }

            if (enemyData != null)
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
            else
            {
                enemyData = ScriptableObject.CreateInstance<EnemyData>();
                enemyData.maxHealth = 50f;
                enemyData.moveSpeed = 4.5f;
                enemyData.attackDamage = 10f;
                enemyData.attackRange = 2.0f;
                enemyData.attackCooldown = 1.8f;
                enemyData.xpReward = 10;

                MaxHP = enemyData.maxHealth;
                CurrentHP = MaxHP;
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

        public virtual void TakeDamage(DamageInfo damageInfo)
        {
            if (IsDead) return;

            CurrentHP = Mathf.Max(CurrentHP - damageInfo.amount, 0f);

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
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.red;
                yield return new WaitForSeconds(0.12f);
                meshRenderer.material.color = originalColor;
            }
        }

        protected virtual void ApplyKnockbackDecay()
        {
            if (knockbackVelocity.magnitude > 0.1f)
            {
                knockbackVelocity.y = 0f; // Force horizontal knockback only
                characterController.Move(knockbackVelocity * Time.deltaTime);
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

            // Simple shrink death effect
            StartCoroutine(ShrinkAndDestroy());
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
