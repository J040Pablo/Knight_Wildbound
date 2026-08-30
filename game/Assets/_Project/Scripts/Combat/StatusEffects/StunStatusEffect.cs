using UnityEngine;

namespace Roguelite.Combat.StatusEffects
{
    public class StunStatusEffect : StatusEffect
    {
        private GameObject stunSparkVFX;

        public StunStatusEffect()
        {
            EffectName = "Stun";
        }

        public override void OnApply()
        {
            if (target != null)
            {
                stunSparkVFX = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stunSparkVFX.name = "StunSparkVFX";
                stunSparkVFX.transform.SetParent(target.transform, false);
                stunSparkVFX.transform.localPosition = new Vector3(0, 2.3f, 0);
                stunSparkVFX.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

                var col = stunSparkVFX.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                var rend = stunSparkVFX.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(1.0f, 0.95f, 0.2f, 0.9f);
                }
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            if (stunSparkVFX != null)
            {
                stunSparkVFX.transform.Rotate(Vector3.up, 360f * deltaTime);
            }
        }

        public override void OnRemove()
        {
            if (stunSparkVFX != null)
            {
                Object.Destroy(stunSparkVFX);
            }
        }
    }
}
