using UnityEngine;
using Roguelite.Combat;
using Roguelite.Enemy;

namespace Roguelite.Combat
{
    public class MagicProjectile : MonoBehaviour
    {
        [Header("Projectile Config")]
        [SerializeField] private float speed = 18.0f;
        [SerializeField] private float maxLifetime = 4.0f;
        
        private float damage = 20.0f;
        private bool isExplosive = false;
        private float explosionRadius = 3.5f;
        private float knockbackForce = 6.0f;
        private GameObject owner;
        private Color projectileColor = Color.cyan;
        private Vector3 moveDirection;

        public void Initialize(GameObject casterOwner, Vector3 direction, float projDamage, float projSpeed = 18.0f, bool explosive = false, float radius = 3.5f, float knockback = 6.0f, Color? color = null)
        {
            owner = casterOwner;
            moveDirection = direction.normalized;
            damage = projDamage;
            speed = projSpeed;
            isExplosive = explosive;
            explosionRadius = radius;
            knockbackForce = knockback;
            if (color.HasValue) projectileColor = color.Value;

            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = projectileColor;
            }

            Destroy(gameObject, maxLifetime);
        }

        private void Update()
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ignore owner, self, and trigger zones
            if (other.gameObject == owner || other.isTrigger) return;

            // Ignore Player collision
            if (other.GetComponent<Roguelite.Player.PlayerStats>() != null) return;

            if (isExplosive)
            {
                ExplodeAoE();
            }
            else
            {
                // Single target hit
                EnemyBase enemy = other.GetComponent<EnemyBase>();
                if (enemy == null) enemy = other.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    DamageInfo info = new DamageInfo(damage, moveDirection, knockbackForce, false, owner);
                    enemy.TakeDamage(info);
                }
            }

            // Spawn simple impact visual
            CreateImpactVisual();
            Destroy(gameObject);
        }

        private void ExplodeAoE()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyBase enemy = hits[i].GetComponent<EnemyBase>();
                if (enemy == null) enemy = hits[i].GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead)
                {
                    Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
                    knockDir.y = 0.3f;
                    DamageInfo info = new DamageInfo(damage, knockDir, knockbackForce, true, owner);
                    enemy.TakeDamage(info);
                }
            }
        }

        private void CreateImpactVisual()
        {
            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.name = "MagicImpactVFX";
            impact.transform.position = transform.position;
            float scale = isExplosive ? explosionRadius * 1.5f : 0.8f;
            impact.transform.localScale = new Vector3(scale, scale, scale);
            Destroy(impact.GetComponent<Collider>());
            
            Renderer r = impact.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0.7f);
            }
            Destroy(impact, 0.25f);
        }
    }
}
