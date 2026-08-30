using UnityEngine;

namespace Roguelite.Combat.StatusEffects
{
    public class RootStatusEffect : StatusEffect
    {
        private GameObject rootVFX;

        public RootStatusEffect()
        {
            EffectName = "Root";
        }

        public override void OnApply()
        {
            if (target != null)
            {
                rootVFX = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rootVFX.name = "RootShadowVFX";
                rootVFX.transform.SetParent(target.transform, false);
                rootVFX.transform.localPosition = new Vector3(0, 0.2f, 0);
                rootVFX.transform.localScale = new Vector3(1.1f, 0.4f, 1.1f);

                var col = rootVFX.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                var rend = rootVFX.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.2f, 0.05f, 0.3f, 0.8f);
                }
            }
        }

        public override void OnRemove()
        {
            if (rootVFX != null)
            {
                Object.Destroy(rootVFX);
            }
        }
    }
}
