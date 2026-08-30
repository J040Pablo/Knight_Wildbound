using UnityEngine;

namespace Roguelite.Combat.StatusEffects
{
    public class SlowStatusEffect : StatusEffect
    {
        public float SlowMultiplier { get; private set; } = 0.6f; // e.g. 40% slow = 0.6 multiplier
        private GameObject frostVFX;

        public SlowStatusEffect(float slowPercent = 0.40f)
        {
            EffectName = "Slow";
            SlowMultiplier = Mathf.Clamp01(1.0f - slowPercent);
        }

        public override void OnApply()
        {
            if (target != null)
            {
                // Create subtle cyan frost glow under target
                frostVFX = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                frostVFX.name = "SlowFrostVFX";
                frostVFX.transform.SetParent(target.transform, false);
                frostVFX.transform.localPosition = new Vector3(0, 0.05f, 0);
                frostVFX.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
                var col = frostVFX.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
                var rend = frostVFX.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.3f, 0.8f, 1.0f, 0.5f);
                }
            }
        }

        public override void OnRemove()
        {
            if (frostVFX != null)
            {
                Object.Destroy(frostVFX);
            }
        }
    }
}
