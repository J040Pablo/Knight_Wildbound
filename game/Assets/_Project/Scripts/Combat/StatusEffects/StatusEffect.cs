using UnityEngine;

namespace Roguelite.Combat.StatusEffects
{
    public abstract class StatusEffect
    {
        public string EffectName { get; protected set; }
        public float Duration { get; protected set; }
        public float RemainingTime { get; protected set; }
        public bool IsFinished => RemainingTime <= 0f;

        protected GameObject target;
        protected GameObject caster;

        public virtual void Initialize(GameObject targetObj, GameObject casterObj, float duration)
        {
            target = targetObj;
            caster = casterObj;
            Duration = duration;
            RemainingTime = duration;
        }

        public virtual void OnApply() { }

        public virtual void OnUpdate(float deltaTime)
        {
            if (RemainingTime > 0f)
            {
                RemainingTime -= deltaTime;
            }
        }

        public virtual void OnRemove() { }

        public virtual void Refresh(float newDuration)
        {
            Duration = Mathf.Max(Duration, newDuration);
            RemainingTime = Mathf.Max(RemainingTime, newDuration);
        }
    }
}
