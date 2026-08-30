using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Data;

namespace Roguelite.Enemy
{
    public class SlimeAI : EnemyBase
    {
        protected override void Awake()
        {
            base.Awake();
            if (enemyData == null || enemyData.xpReward == 0)
            {
                if (enemyData == null) enemyData = ScriptableObject.CreateInstance<EnemyData>();
                enemyData.enemyName = "Gnome";
                enemyData.enemyType = EnemyType.Gnome;
                enemyData.maxHealth = 35f;
                enemyData.moveSpeed = 4.2f;
                enemyData.attackDamage = 8f;
                enemyData.attackRange = 1.8f;
                enemyData.attackCooldown = 1.8f;
                enemyData.xpReward = 10;

                MaxHP = enemyData.maxHealth;
                CurrentHP = MaxHP;
            }
        }

        protected override void Start()
        {
            base.Start();
            BuildGnomeVisuals();
        }

        private void BuildGnomeVisuals()
        {
            if (transform.Find("GnomeBody_Visual") != null) return;

            // 1. Tunic Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "GnomeBody_Visual";
            body.transform.parent = transform;
            body.transform.localPosition = new Vector3(0, 0.4f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            Collider bCol = body.GetComponent<Collider>();
            if (bCol != null) Destroy(bCol);
            Renderer bR = body.GetComponent<Renderer>();
            if (bR != null) bR.material.color = new Color(0.25f, 0.45f, 0.18f); // Forest green tunic

            // 2. Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "GnomeHead_Visual";
            head.transform.parent = transform;
            head.transform.localPosition = new Vector3(0, 0.9f, 0);
            head.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            Collider hCol = head.GetComponent<Collider>();
            if (hCol != null) Destroy(hCol);
            Renderer hR = head.GetComponent<Renderer>();
            if (hR != null) hR.material.color = new Color(0.95f, 0.78f, 0.65f); // Peach skin

            // 3. Pointy Red Garden Hat
            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "GnomeHat_Visual";
            hat.transform.parent = transform;
            hat.transform.localPosition = new Vector3(0, 1.25f, 0);
            hat.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Collider hatCol = hat.GetComponent<Collider>();
            if (hatCol != null) Destroy(hatCol);
            Renderer hatR = hat.GetComponent<Renderer>();
            if (hatR != null) hatR.material.color = new Color(0.9f, 0.15f, 0.12f); // Pointy red hat
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
            if (IsDead || playerTransform == null || playerStats.IsDead || isAttacking || Inventory.StealthState.IsPlayerInvisible || !SafeCanMove()) return;

            attackTimer -= Time.deltaTime;
            float distToPlayer = GetFlatDistanceToPlayer();

            if (distToPlayer > enemyData.attackRange)
            {
                // Move towards player
                Vector3 moveDir = (playerTransform.position - transform.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.Normalize();
                    Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                    targetRot.Normalize();
                    Quaternion slerped = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
                    slerped.Normalize();
                    transform.rotation = slerped;
                }
                characterController.Move(moveDir * enemyData.moveSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);
            }
            else if (attackTimer <= 0)
            {
                StartCoroutine(PerformSlimeLeapAttack());
            }
        }

        private IEnumerator PerformSlimeLeapAttack()
        {
            isAttacking = true;
            attackTimer = enemyData.attackCooldown;

            Vector3 targetPos = playerTransform.position;
            Vector3 leapDir = (targetPos - transform.position);
            leapDir.y = 0;
            if (leapDir.sqrMagnitude > 0.0001f) leapDir.Normalize();
            else leapDir = transform.forward;

            float leapTime = 0.5f;
            float elapsed = 0f;

            while (elapsed < leapTime)
            {
                if (IsDead || !SafeCanMove()) yield break;

                // Move forward & arch up
                float height = Mathf.Sin((elapsed / leapTime) * Mathf.PI) * 2.0f;
                Vector3 leapVelocity = leapDir * (enemyData.moveSpeed * 1.8f);
                leapVelocity.y = height * 4.0f;

                characterController.Move(leapVelocity * Time.deltaTime);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Deal Damage if player is in range at landing
            if (playerStats != null && !playerStats.IsDead)
            {
                float distAtLanding = GetFlatDistanceToPlayer();
                if (distAtLanding <= enemyData.attackRange + 0.8f)
                {
                    Vector3 kbDir = (playerTransform.position - transform.position);
                    kbDir.y = 0;
                    if (kbDir.sqrMagnitude > 0.0001f) kbDir.Normalize();
                    else kbDir = transform.forward;

                    DamageInfo damage = new DamageInfo(
                        enemyData.attackDamage,
                        kbDir,
                        4.0f,
                        false,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }
            }

            isAttacking = false;
        }
    }
}
