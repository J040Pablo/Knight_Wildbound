using System;
using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Environment
{
    public class BarrierHealth : MonoBehaviour, IDamageable
    {
        [Header("Barrier Health Settings")]
        [SerializeField] private float maxHealth = 200f;
        [SerializeField] private float currentHealth = 200f;
        [SerializeField] private bool isUnlocked = false;

        public float MaxHP => maxHealth;
        public float CurrentHP => currentHealth;
        public bool IsUnlocked => isUnlocked;
        public bool IsDead => currentHealth <= 0;

        public event Action<float, float> OnHealthChanged;
        public event Action OnBarrierDestroyed;

        private BiomeExitBarrier exitBarrier;
        private BarrierDestructionSequence destructionSequence;

        private void Awake()
        {
            currentHealth = maxHealth;
            exitBarrier = GetComponent<BiomeExitBarrier>();
            destructionSequence = GetComponent<BarrierDestructionSequence>();
        }

        public void UnlockBarrier()
        {
            isUnlocked = true;
            // Debug.Log("[BarrierHealth] Corrupted barrier is now unlocked and damageable!");
        }

        public void SetMaxHealth(float hp)
        {
            maxHealth = hp;
            currentHealth = hp;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!isUnlocked || IsDead) return;

            currentHealth = Mathf.Max(0f, currentHealth - damageInfo.amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            float damageRatio = 1f - (currentHealth / maxHealth);

            // Update visual cracks on barrier
            if (exitBarrier != null)
            {
                exitBarrier.SetCrackStage(damageRatio);
            }

            // Spawn hit impact feedback
            SpawnHitImpact(damageInfo.knockbackDirection);

            // Debug.Log($"[BarrierHealth] Barrier took {damageInfo.amount:F1} damage. HP: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void SpawnHitImpact(Vector3 hitDir)
        {
            // Wood chip impact effect
            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.name = "WoodChipImpact";
            impact.transform.position = transform.position + Vector3.up * 3.5f - hitDir.normalized * 0.5f;
            impact.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            Collider c = impact.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer r = impact.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.85f, 0.55f, 0.25f);

            Destroy(impact, 0.4f);
        }

        private void Die()
        {
            OnBarrierDestroyed?.Invoke();

            if (destructionSequence != null)
            {
                destructionSequence.ExecuteDestruction();
            }
            else
            {
                Destroy(gameObject, 0.5f);
            }
        }
    }
}
