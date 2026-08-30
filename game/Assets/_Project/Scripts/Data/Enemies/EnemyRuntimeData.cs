using System;
using UnityEngine;

namespace Roguelite.Data
{
    [Serializable]
    public class EnemyRuntimeData
    {
        public EnemyDefinition Definition { get; private set; }
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }
        public float Damage { get; set; }
        public float MoveSpeed { get; set; }
        public bool IsDead => CurrentHealth <= 0f;

        public EnemyRuntimeData(EnemyDefinition def)
        {
            Definition = def;
            if (def != null)
            {
                MaxHealth = def.maxHealth;
                CurrentHealth = MaxHealth;
                Damage = def.damage;
                MoveSpeed = def.moveSpeed;
            }
            else
            {
                MaxHealth = 50f;
                CurrentHealth = 50f;
                Damage = 10f;
                MoveSpeed = 4f;
            }
        }

        public void TakeDamage(float amount)
        {
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        }

        public void Heal(float amount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        }
    }
}
