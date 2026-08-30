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
            moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            damage = projDamage;
            speed = projSpeed;
            isExplosive = explosive;
            explosionRadius = radius;
            knockbackForce = knockback;
            if (color.HasValue) projectileColor = color.Value;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 safeUp = Mathf.Abs(Vector3.Dot(moveDirection, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
                Quaternion rot = Quaternion.LookRotation(moveDirection, safeUp);
                rot.Normalize();
                transform.rotation = rot;
            }

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
            if (other == null) return;

            // Strict Self-Damage & Caster Prevention
            if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform) || owner.transform.IsChildOf(other.transform) || other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Player")))
            {
                return;
            }
            if (other.GetComponent<Roguelite.Player.PlayerStats>() != null) return;

            // Check if target is an enemy
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy == null) enemy = other.GetComponentInParent<EnemyBase>();

            bool isEnemy = enemy != null && !enemy.IsDead;
            bool isEnemyLayer = other.gameObject.layer == LayerMask.NameToLayer("Enemy") || other.gameObject.layer == LayerMask.NameToLayer("Boss") || other.gameObject.layer == LayerMask.NameToLayer("Destructible");

            // Ignore non-enemy triggers (like interaction triggers, camera bounds)
            if (other.isTrigger && !isEnemy && !isEnemyLayer) return;

            if (isExplosive)
            {
                ExplodeAoE();
            }
            else
            {
                if (isEnemy)
                {
                    DamageInfo info = new DamageInfo(damage, moveDirection, knockbackForce, false, owner);
                    enemy.TakeDamage(info);
                }
            }

            // Spawn impact visual
            CreateImpactVisual();
            Destroy(gameObject);
        }

        private void ExplodeAoE()
        {
            int enemyMask = LayerMask.GetMask("Enemy", "Boss", "Destructible");
            if (enemyMask == 0) enemyMask = ~LayerMask.GetMask("Player", "PlayerHitbox", "Ignore Raycast", "UI", "Water");

            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, enemyMask);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;
                if (owner != null && (hits[i].gameObject == owner || hits[i].transform.IsChildOf(owner.transform) || owner.transform.IsChildOf(hits[i].transform) || hits[i].CompareTag("Player") || hits[i].gameObject.layer == LayerMask.NameToLayer("Player"))) continue;

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
