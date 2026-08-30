using UnityEngine;

namespace Roguelite.Combat.StatusEffects
{
    public class DamageOverTimeStatusEffect : StatusEffect
    {
        private float dps;
        private float tickInterval;
        private float tickTimer = 0f;
        private Color dotColor;
        private GameObject dotVFX;

        public DamageOverTimeStatusEffect(string name, float damagePerSecond, float interval = 0.5f, Color? vfxColor = null)
        {
            EffectName = name;
            dps = damagePerSecond;
            tickInterval = interval;
            dotColor = vfxColor.HasValue ? vfxColor.Value : Color.red;
        }

        public override void OnApply()
        {
            if (target != null)
            {
                dotVFX = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dotVFX.name = $"{EffectName}_VFX";
                dotVFX.transform.SetParent(target.transform, false);
                dotVFX.transform.localPosition = new Vector3(0, 1.2f, 0);
                dotVFX.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

                var col = dotVFX.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                var rend = dotVFX.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = dotColor;
                }
            }
        }

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            tickTimer += deltaTime;

            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                if (target != null)
                {
                    var damageable = target.GetComponent<IDamageable>();
                    if (damageable == null) damageable = target.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        DamageInfo info = new DamageInfo(dps * tickInterval, Vector3.zero, 0f, false, caster);
                        damageable.TakeDamage(info);
                    }
                }
            }
        }

        public override void OnRemove()
        {
            if (dotVFX != null)
            {
                Object.Destroy(dotVFX);
            }
        }
    }
}
