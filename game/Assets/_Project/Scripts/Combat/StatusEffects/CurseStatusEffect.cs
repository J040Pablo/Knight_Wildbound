using UnityEngine;

namespace Roguelite.Combat.StatusEffects
{
    public class CurseStatusEffect : StatusEffect
    {
        public float SpeedDebuffMultiplier { get; private set; } = 1.0f;
        public float DamageDebuffMultiplier { get; private set; } = 1.0f;
        private float dps = 5f;
        private float tickTimer = 0f;
        private GameObject markVFX;

        public CurseStatusEffect(float dpsVal = 5f, float speedDebuff = 0f, float damageDebuff = 0f)
        {
            EffectName = "Curse";
            dps = dpsVal;
            SpeedDebuffMultiplier = Mathf.Clamp01(1.0f - speedDebuff);
            DamageDebuffMultiplier = Mathf.Clamp01(1.0f - damageDebuff);
        }

        public override void OnApply()
        {
            if (target != null)
            {
                markVFX = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markVFX.name = "CurseMarkVFX";
                markVFX.transform.SetParent(target.transform, false);
                markVFX.transform.localPosition = new Vector3(0, 2.4f, 0);
                markVFX.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

                var col = markVFX.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                var rend = markVFX.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.4f, 0.05f, 0.5f, 0.9f);
                }
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            tickTimer += deltaTime;

            if (markVFX != null)
            {
                markVFX.transform.Rotate(Vector3.up, 180f * deltaTime);
            }

            if (tickTimer >= 0.5f)
            {
                tickTimer = 0f;
                if (target != null)
                {
                    var damageable = target.GetComponent<IDamageable>();
                    if (damageable == null) damageable = target.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        DamageInfo info = new DamageInfo(dps * 0.5f, Vector3.zero, 0f, false, caster);
                        damageable.TakeDamage(info);
                    }
                }
            }
        }

        public override void OnRemove()
        {
            if (markVFX != null)
            {
                Object.Destroy(markVFX);
            }
        }
    }
}
