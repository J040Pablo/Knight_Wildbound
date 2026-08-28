using System.Collections;
using UnityEngine;
using Roguelite.Combat;

namespace Roguelite.Enemy
{
    public class PumpkinEnemyAI : EnemyBase
    {
        [Header("Pumpkin Specifics")]
        [SerializeField] private bool isElite = false;
        [SerializeField] private float jumpAttackCooldown = 3.5f;

        private float attackTimer = 0f;
        private bool isJumping = false;

        public bool IsElite => isElite;

        public void SetEliteStatus(bool elite)
        {
            isElite = elite;
            if (isElite)
            {
                MaxHP = 160f;
                CurrentHP = MaxHP;
                transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                if (meshRenderer != null)
                {
                    originalColor = new Color(0.95f, 0.35f, 0.05f); // Bright fiery orange
                    meshRenderer.material.color = originalColor;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (enemyData == null)
            {
                MaxHP = isElite ? 160f : 45f;
                CurrentHP = MaxHP;
            }
        }

        protected override void Start()
        {
            base.Start();
            BuildPumpkinVisuals();
        }

        private void BuildPumpkinVisuals()
        {
            // Build low-poly pumpkin visual body if not present
            if (transform.Find("PumpkinBody_Visual") == null)
            {
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.name = "PumpkinBody_Visual";
                body.transform.parent = transform;
                body.transform.localPosition = new Vector3(0, 0.6f, 0);
                body.transform.localScale = new Vector3(1.1f, 0.9f, 1.1f);
                Destroy(body.GetComponent<Collider>());
                Renderer bR = body.GetComponent<Renderer>();
                if (bR != null)
                {
                    Color pColor = isElite ? new Color(0.95f, 0.3f, 0.05f) : new Color(0.9f, 0.45f, 0.1f);
                    bR.material.color = pColor;
                }

                // Green Stem
                GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "PumpkinStem_Visual";
                stem.transform.parent = transform;
                stem.transform.localPosition = new Vector3(0, 1.15f, 0);
                stem.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                Destroy(stem.GetComponent<Collider>());
                Renderer sR = stem.GetComponent<Renderer>();
                if (sR != null) sR.material.color = new Color(0.15f, 0.6f, 0.2f);
            }
        }

        private float GetFlatDistanceToPlayer()
        {
            if (playerTransform == null) return 999f;
            Vector3 pPos = playerTransform.position;
            Vector3 ePos = transform.position;
            return Vector2.Distance(new Vector2(ePos.x, ePos.z), new Vector2(pPos.x, pPos.z));
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || playerTransform == null || playerStats.IsDead || isJumping) return;

            attackTimer -= Time.deltaTime;
            float distToPlayer = GetFlatDistanceToPlayer();
            float moveSpeed = isElite ? 4.5f : 5.5f;

            if (distToPlayer > 2.5f)
            {
                Vector3 moveDir = (playerTransform.position - transform.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.Normalize();
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir, Vector3.up), Time.deltaTime * 8f);
                }
                characterController.Move(moveDir * moveSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);
            }
            else if (attackTimer <= 0)
            {
                StartCoroutine(PerformBounceAttack());
            }
        }

        private IEnumerator PerformBounceAttack()
        {
            isJumping = true;
            attackTimer = jumpAttackCooldown;

            Vector3 jumpDir = (playerTransform.position - transform.position);
            jumpDir.y = 0.4f;
            if (jumpDir.sqrMagnitude > 0.0001f) jumpDir.Normalize();
            else jumpDir = transform.forward;

            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (IsDead) yield break;

                characterController.Move(jumpDir * (isElite ? 9f : 11f) * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Impact damage
            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float dist = GetFlatDistanceToPlayer();
                if (dist <= 2.8f)
                {
                    float dmg = isElite ? 25f : 12f;
                    Vector3 kbDir = (playerTransform.position - transform.position);
                    kbDir.y = 0;
                    if (kbDir.sqrMagnitude > 0.0001f) kbDir.Normalize();
                    else kbDir = transform.forward;

                    DamageInfo damage = new DamageInfo(
                        dmg,
                        kbDir,
                        6f,
                        false,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }
            }

            yield return new WaitForSeconds(0.2f);
            isJumping = false;
        }
    }
}
