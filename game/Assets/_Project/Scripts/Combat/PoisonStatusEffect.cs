using System.Collections;
using UnityEngine;
using Roguelite.Player;
using Roguelite.Combat;

namespace Roguelite.Combat
{
    public class PoisonStatusEffect : MonoBehaviour
    {
        private PlayerStats playerStats;
        private PlayerController playerController;

        private float poisonDurationTimer = 0f;
        private float tickTimer = 0f;
        private float dps = 3.0f;
        public bool IsPoisoned => poisonDurationTimer > 0f;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            playerController = GetComponent<PlayerController>();
        }

        public void ApplyPoison(float duration = 6f, float damagePerSecond = 3f)
        {
            poisonDurationTimer = Mathf.Max(poisonDurationTimer, duration);
            dps = damagePerSecond;
            Debug.Log($"[PoisonStatusEffect] Applied Poison for {duration:F1}s!");
        }

        public void Cleanse()
        {
            poisonDurationTimer = 0f;
            Debug.Log("[PoisonStatusEffect] Poison Cleansed!");
        }

        private void Update()
        {
            if (poisonDurationTimer <= 0f) return;

            poisonDurationTimer -= Time.deltaTime;
            tickTimer += Time.deltaTime;

            if (tickTimer >= 1.0f)
            {
                tickTimer = 0f;
                if (playerStats != null && !playerStats.IsDead)
                {
                    DamageInfo info = new DamageInfo(dps, Vector3.zero, 0f, false, gameObject);
                    playerStats.TakeDamage(info);
                }
            }
        }
    }
}
