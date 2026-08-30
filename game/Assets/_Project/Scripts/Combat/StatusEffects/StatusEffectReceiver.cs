using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Combat.StatusEffects
{
    public class StatusEffectReceiver : MonoBehaviour
    {
        private readonly List<StatusEffect> activeEffects = new List<StatusEffect>();

        public float CombinedSpeedMultiplier { get; private set; } = 1.0f;
        public float CombinedDamageMultiplier { get; private set; } = 1.0f;
        public bool IsStunnedOrFrozen { get; private set; } = false;
        public bool IsRooted { get; private set; } = false;

        public void ApplyEffect(StatusEffect effect, GameObject caster, float duration)
        {
            if (effect == null) return;

            // Check if effect of same type is already active
            StatusEffect existing = activeEffects.Find(e => e.GetType() == effect.GetType());
            if (existing != null)
            {
                existing.Refresh(duration);
            }
            else
            {
                effect.Initialize(gameObject, caster, duration);
                activeEffects.Add(effect);
                effect.OnApply();
            }

            RecalculateCombinedModifiers();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool modified = false;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.OnUpdate(dt);

                if (effect.IsFinished)
                {
                    effect.OnRemove();
                    activeEffects.RemoveAt(i);
                    modified = true;
                }
            }

            if (modified)
            {
                RecalculateCombinedModifiers();
            }
        }

        public void RecalculateCombinedModifiers()
        {
            float speedMult = 1.0f;
            float dmgMult = 1.0f;
            bool stunnedOrFrozen = false;
            bool rooted = false;

            foreach (var effect in activeEffects)
            {
                if (effect is SlowStatusEffect slow)
                {
                    speedMult *= slow.SlowMultiplier;
                }
                else if (effect is FreezeStatusEffect || effect is StunStatusEffect)
                {
                    stunnedOrFrozen = true;
                    speedMult = 0f;
                }
                else if (effect is RootStatusEffect)
                {
                    rooted = true;
                    speedMult = 0f;
                }
                else if (effect is CurseStatusEffect curse)
                {
                    speedMult *= curse.SpeedDebuffMultiplier;
                    dmgMult *= curse.DamageDebuffMultiplier;
                }
            }

            CombinedSpeedMultiplier = speedMult;
            CombinedDamageMultiplier = dmgMult;
            IsStunnedOrFrozen = stunnedOrFrozen;
            IsRooted = rooted;

            // Sync with EnemyBase if attached
            var enemy = GetComponent<Roguelite.Enemy.EnemyBase>();
            if (enemy == null) enemy = GetComponentInParent<Roguelite.Enemy.EnemyBase>();
            if (enemy != null)
            {
                enemy.SetStatusModifiers(CombinedSpeedMultiplier, CombinedDamageMultiplier, IsStunnedOrFrozen || IsRooted);
            }
        }

        public void RemoveAllEffects()
        {
            foreach (var effect in activeEffects)
            {
                effect.OnRemove();
            }
            activeEffects.Clear();
            RecalculateCombinedModifiers();
        }

        private void OnDestroy()
        {
            RemoveAllEffects();
        }
    }
}
