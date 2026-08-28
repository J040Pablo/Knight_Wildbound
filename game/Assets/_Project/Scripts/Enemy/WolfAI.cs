using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Data;

namespace Roguelite.Enemy
{
    public class WolfAI : EnemyBase
    {
        private bool isCharging = false;
        protected override void Awake()
        {
            base.Awake();
            if (enemyData == null || enemyData.xpReward == 0)
            {
                if (enemyData == null) enemyData = ScriptableObject.CreateInstance<EnemyData>();
                enemyData.enemyName = "Creature";
                enemyData.enemyType = EnemyType.Creature;
                enemyData.maxHealth = 50f;
                enemyData.moveSpeed = 6.5f;
                enemyData.attackDamage = 12f;
                enemyData.attackRange = 2.2f;
                enemyData.attackCooldown = 2.2f;
                enemyData.xpReward = 20;

                MaxHP = enemyData.maxHealth;
                CurrentHP = MaxHP;
            }
        }

        protected override void Start()
        {
            base.Start();
            BuildCreatureVisuals();
        }

        private void BuildCreatureVisuals()
        {
            if (transform.Find("CreatureBody_Visual") != null) return;

            // 1. Corrupted Quadruped Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "CreatureBody_Visual";
            body.transform.parent = transform;
            body.transform.localPosition = new Vector3(0, 0.5f, 0);
            body.transform.localScale = new Vector3(0.7f, 0.5f, 1.4f);
            Collider bCol = body.GetComponent<Collider>();
            if (bCol != null) Destroy(bCol);
            Renderer bR = body.GetComponent<Renderer>();
            if (bR != null) bR.material.color = new Color(0.18f, 0.12f, 0.22f); // Corrupted dark violet

            // 2. 4 Legs
            CreateCreatureLeg("Leg_FL", new Vector3(-0.35f, 0.25f, 0.5f));
            CreateCreatureLeg("Leg_FR", new Vector3(0.35f, 0.25f, 0.5f));
            CreateCreatureLeg("Leg_BL", new Vector3(-0.35f, 0.25f, -0.5f));
            CreateCreatureLeg("Leg_BR", new Vector3(0.35f, 0.25f, -0.5f));

            // 3. Head with Glowing Eyes
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "CreatureHead_Visual";
            head.transform.parent = transform;
            head.transform.localPosition = new Vector3(0, 0.65f, 0.8f);
            head.transform.localScale = new Vector3(0.48f, 0.45f, 0.55f);
            Collider hCol = head.GetComponent<Collider>();
            if (hCol != null) Destroy(hCol);
            Renderer hR = head.GetComponent<Renderer>();
            if (hR != null) hR.material.color = new Color(0.12f, 0.08f, 0.16f);

            // Glowing Eyes
            GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeL.name = "GlowingEyeL";
            eyeL.transform.parent = head.transform;
            eyeL.transform.localPosition = new Vector3(-0.18f, 0.1f, 0.22f);
            eyeL.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            Collider eLCol = eyeL.GetComponent<Collider>();
            if (eLCol != null) Destroy(eLCol);
            Renderer elR = eyeL.GetComponent<Renderer>();
            if (elR != null) elR.material.color = new Color(1.0f, 0.9f, 0.1f); // Bright yellow glow

            GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeR.name = "GlowingEyeR";
            eyeR.transform.parent = head.transform;
            eyeR.transform.localPosition = new Vector3(0.18f, 0.1f, 0.22f);
            eyeR.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            Collider eRCol = eyeR.GetComponent<Collider>();
            if (eRCol != null) Destroy(eRCol);
            Renderer erR = eyeR.GetComponent<Renderer>();
            if (erR != null) erR.material.color = new Color(1.0f, 0.9f, 0.1f);
        }

        private void CreateCreatureLeg(string legName, Vector3 localPos)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = legName;
            leg.transform.parent = transform;
            leg.transform.localPosition = localPos;
            leg.transform.localScale = new Vector3(0.16f, 0.25f, 0.16f);
            Collider col = leg.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Renderer r = leg.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.15f, 0.10f, 0.18f);
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
            if (IsDead || playerTransform == null || playerStats.IsDead || isCharging) return;

            attackTimer -= Time.deltaTime;
            float distToPlayer = GetFlatDistanceToPlayer();

            if (distToPlayer > enemyData.attackRange + 3.0f)
            {
                // Fast approach
                Vector3 moveDir = (playerTransform.position - transform.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.Normalize();
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir, Vector3.up), Time.deltaTime * 12f);
                }
                characterController.Move(moveDir * enemyData.moveSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);
            }
            else if (attackTimer <= 0)
            {
                StartCoroutine(PerformWolfCharge());
            }
        }

        private IEnumerator PerformWolfCharge()
        {
            isCharging = true;
            attackTimer = enemyData.attackCooldown;

            // Wind-up: Face player and pause
            Vector3 chargeDir = (playerTransform.position - transform.position);
            chargeDir.y = 0;
            if (chargeDir.sqrMagnitude > 0.0001f)
            {
                chargeDir.Normalize();
                transform.rotation = Quaternion.LookRotation(chargeDir, Vector3.up);
            }
            else
            {
                chargeDir = transform.forward;
            }

            // Wind-up telegraph pause
            yield return new WaitForSeconds(0.4f);

            // Fast Charge forward
            float chargeDuration = 0.5f;
            float elapsed = 0f;
            float chargeSpeed = enemyData.moveSpeed * 2.5f;
            bool hitPlayer = false;

            while (elapsed < chargeDuration)
            {
                if (IsDead) yield break;

                characterController.Move(chargeDir * chargeSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);

                if (!hitPlayer && playerStats != null && !playerStats.IsDead)
                {
                    if (GetFlatDistanceToPlayer() < 2.2f)
                    {
                        hitPlayer = true;
                        DamageInfo damage = new DamageInfo(
                            enemyData.attackDamage,
                            chargeDir,
                            8.0f,
                            false,
                            gameObject
                        );
                        playerStats.TakeDamage(damage);
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            isCharging = false;
        }
    }
}
