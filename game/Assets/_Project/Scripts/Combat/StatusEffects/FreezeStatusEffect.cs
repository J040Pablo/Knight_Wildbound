using UnityEngine;

namespace Roguelite.Combat.StatusEffects
{
    public class FreezeStatusEffect : StatusEffect
    {
        private GameObject iceBlockVFX;

        public FreezeStatusEffect()
        {
            EffectName = "Freeze";
        }

        public override void OnApply()
        {
            if (target != null)
            {
                // Spawn Ice Block around target entity
                iceBlockVFX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                iceBlockVFX.name = "FreezeIceBlockVFX";
                iceBlockVFX.transform.SetParent(target.transform, false);
                iceBlockVFX.transform.localPosition = new Vector3(0, 1.0f, 0);
                iceBlockVFX.transform.localScale = new Vector3(1.4f, 2.0f, 1.4f);

                var col = iceBlockVFX.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                var rend = iceBlockVFX.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.4f, 0.9f, 1.0f, 0.65f);
                }
            }
        }

        public override void OnRemove()
        {
            if (iceBlockVFX != null)
            {
                Object.Destroy(iceBlockVFX);
            }
        }
    }
}
