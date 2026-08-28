using System.Collections;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Data;

namespace Roguelite.Enemy
{
    public class GoblinAI : EnemyBase
    {
        protected override void Awake()
        {
            base.Awake();
            if (enemyData == null || enemyData.xpReward == 0)
            {
                if (enemyData == null) enemyData = ScriptableObject.CreateInstance<EnemyData>();
                enemyData.enemyName = "Mini Tree";
                enemyData.enemyType = EnemyType.MiniTree;
                enemyData.maxHealth = 60f;
                enemyData.moveSpeed = 4.8f;
                enemyData.attackDamage = 14f;
                enemyData.attackRange = 2.0f;
                enemyData.attackCooldown = 1.6f;
                enemyData.xpReward = 20;

                MaxHP = enemyData.maxHealth;
                CurrentHP = MaxHP;
            }
        }

        protected override void Start()
        {
            base.Start();
            BuildMiniTreeVisuals();
        }

        private void BuildMiniTreeVisuals()
        {
            if (transform.Find("MiniTreeTrunk_Visual") != null) return;

            // 1. Bark Trunk with face
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "MiniTreeTrunk_Visual";
            trunk.transform.parent = transform;
            trunk.transform.localPosition = new Vector3(0, 0.7f, 0);
            trunk.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            Collider tCol = trunk.GetComponent<Collider>();
            if (tCol != null) DestroyImmediate(tCol);
            Renderer tR = trunk.GetComponent<Renderer>();
            if (tR != null) tR.material.color = new Color(0.32f, 0.20f, 0.12f); // Bark brown

            // 2. Branch Arms
            GameObject leftBranch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftBranch.name = "MiniTreeBranchL";
            leftBranch.transform.parent = transform;
            leftBranch.transform.localPosition = new Vector3(-0.45f, 0.8f, 0);
            leftBranch.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
            leftBranch.transform.localRotation = Quaternion.Euler(0, 0, 65f);
            Collider lbCol = leftBranch.GetComponent<Collider>();
            if (lbCol != null) DestroyImmediate(lbCol);
            Renderer lbR = leftBranch.GetComponent<Renderer>();
            if (lbR != null) lbR.material.color = new Color(0.28f, 0.18f, 0.10f);

            GameObject rightBranch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightBranch.name = "MiniTreeBranchR";
            rightBranch.transform.parent = transform;
            rightBranch.transform.localPosition = new Vector3(0.45f, 0.8f, 0);
            rightBranch.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
            rightBranch.transform.localRotation = Quaternion.Euler(0, 0, -65f);
            Collider rbCol = rightBranch.GetComponent<Collider>();
            if (rbCol != null) DestroyImmediate(rbCol);
            Renderer rbR = rightBranch.GetComponent<Renderer>();
            if (rbR != null) rbR.material.color = new Color(0.28f, 0.18f, 0.10f);

            // 3. Foliage Canopy
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "MiniTreeCanopy_Visual";
            canopy.transform.parent = transform;
            canopy.transform.localPosition = new Vector3(0, 1.4f, 0);
            canopy.transform.localScale = new Vector3(1.1f, 0.65f, 1.1f);
            Collider cCol = canopy.GetComponent<Collider>();
            if (cCol != null) DestroyImmediate(cCol);
            Renderer cR = canopy.GetComponent<Renderer>();
            if (cR != null) cR.material.color = new Color(0.15f, 0.42f, 0.15f); // Lush foliage green
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
            if (IsDead || playerTransform == null || playerStats.IsDead || isAttacking) return;

            attackTimer -= Time.deltaTime;
            float distToPlayer = GetFlatDistanceToPlayer();

            if (distToPlayer > enemyData.attackRange)
            {
                Vector3 moveDir = (playerTransform.position - transform.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.Normalize();
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir, Vector3.up), Time.deltaTime * 10f);
                }
                characterController.Move(moveDir * enemyData.moveSpeed * Time.deltaTime + new Vector3(0, -9.8f, 0) * Time.deltaTime);
            }
            else if (attackTimer <= 0)
            {
                StartCoroutine(PerformGoblinSlash());
            }
        }

        private IEnumerator PerformGoblinSlash()
        {
            isAttacking = true;
            attackTimer = enemyData.attackCooldown;

            // Telegraph pause (0.3s)
            yield return new WaitForSeconds(0.3f);

            if (!IsDead && playerStats != null && !playerStats.IsDead)
            {
                float distToPlayer = GetFlatDistanceToPlayer();
                if (distToPlayer <= enemyData.attackRange + 0.8f)
                {
                    Vector3 knockbackDir = (playerTransform.position - transform.position);
                    knockbackDir.y = 0;
                    if (knockbackDir.sqrMagnitude > 0.0001f) knockbackDir.Normalize();
                    else knockbackDir = transform.forward;

                    DamageInfo damage = new DamageInfo(
                        enemyData.attackDamage,
                        knockbackDir,
                        5.0f,
                        false,
                        gameObject
                    );
                    playerStats.TakeDamage(damage);
                }
            }

            yield return new WaitForSeconds(0.2f);
            isAttacking = false;
        }
    }
}
