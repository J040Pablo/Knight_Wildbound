using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Enemy;
using Roguelite.Data;
using Roguelite.Core;

namespace Roguelite.Wave
{
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float arenaRadius = 22.0f;
        [SerializeField] private Transform playerTransform;

        private int currentWaveIndex = 0;
        private List<EnemyBase> activeEnemies = new List<EnemyBase>();
        private bool isWaveInProgress = false;
        private bool bossSpawned = false;

        public int CurrentWave => currentWaveIndex;
        public int TotalEnemiesRemaining => activeEnemies.Count;

        public event Action<int> OnWaveStarted;
        public event Action OnBossSpawned;

        private void Start()
        {
            if (playerTransform == null)
            {
                var player = FindFirstObjectByType<Roguelite.Player.PlayerStats>();
                if (player != null) playerTransform = player.transform;
            }

            StartCoroutine(StartNextWaveWithDelay(2.0f));
        }

        private IEnumerator StartNextWaveWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextWave();
        }

        public void StartNextWave()
        {
            currentWaveIndex++;
            isWaveInProgress = true;

            OnWaveStarted?.Invoke(currentWaveIndex);

            switch (currentWaveIndex)
            {
                case 1:
                    SpawnEnemyGroup(EnemyType.Gnome, 5);
                    break;
                case 2:
                    SpawnEnemyGroup(EnemyType.Gnome, 3);
                    SpawnEnemyGroup(EnemyType.MiniTree, 3);
                    break;
                case 3:
                    SpawnEnemyGroup(EnemyType.MiniTree, 3);
                    SpawnEnemyGroup(EnemyType.Creature, 4);
                    break;
                default:
                    if (!bossSpawned)
                    {
                        bossSpawned = true;
                        SpawnBoss();
                    }
                    break;
            }
        }

        private int lastSpawnIndex = -1;
        private List<Vector3> predefinedSpawnPoints = new List<Vector3>();

        private void Awake()
        {
            InitializePredefinedSpawnPoints();
        }

        private void InitializePredefinedSpawnPoints()
        {
            predefinedSpawnPoints.Clear();
            int count = 12;
            float radius = 17.0f;

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.5f, Mathf.Sin(angle) * radius);
                predefinedSpawnPoints.Add(pos);
            }
        }

        private void SpawnEnemyGroup(EnemyType type, int count)
        {
            int spawnedCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (TryGetValidSpawnPosition(out Vector3 spawnPos))
                {
                    GameObject enemyObj = CreateEnemyPrimitiveInstance(type, spawnPos);
                    EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        activeEnemies.Add(enemy);
                        enemy.OnEnemyDied += HandleEnemyDied;
                        spawnedCount++;
                    }
                }
            }

            if (spawnedCount == 0 && count > 0)
            {
                Debug.LogWarning($"[WaveSpawner] Could not find valid spawn points for {type}. Skipped to prevent stacking.");
            }
        }

        private void SpawnBoss()
        {
            if (TryGetValidSpawnPosition(out Vector3 spawnPos))
            {
                GameObject bossObj = CreateEnemyPrimitiveInstance(EnemyType.Boss, spawnPos);
                BossAI boss = bossObj.GetComponent<BossAI>();
                if (boss != null)
                {
                    activeEnemies.Add(boss);
                    boss.OnEnemyDied += HandleEnemyDied;
                    OnBossSpawned?.Invoke();
                }
            }
            else
            {
                // Fallback to center offset if tight
                Vector3 fallbackPos = new Vector3(0, 0.5f, 15f);
                GameObject bossObj = CreateEnemyPrimitiveInstance(EnemyType.Boss, fallbackPos);
                BossAI boss = bossObj.GetComponent<BossAI>();
                if (boss != null)
                {
                    activeEnemies.Add(boss);
                    boss.OnEnemyDied += HandleEnemyDied;
                    OnBossSpawned?.Invoke();
                }
            }
        }

        private bool TryGetValidSpawnPosition(out Vector3 validPos)
        {
            validPos = Vector3.zero;
            if (playerTransform == null)
            {
                var pStats = FindFirstObjectByType<Roguelite.Player.PlayerStats>();
                if (pStats != null) playerTransform = pStats.transform;
            }

            Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

            int attempts = 0;
            while (attempts < 25)
            {
                attempts++;
                int index = UnityEngine.Random.Range(0, predefinedSpawnPoints.Count);
                if (index == lastSpawnIndex && predefinedSpawnPoints.Count > 1) continue;

                Vector3 candidate = predefinedSpawnPoints[index];
                
                // Add minor random jitter (up to 1.5m) to avoid exact grid alignment while preserving minimum distances
                Vector2 jitter = UnityEngine.Random.insideUnitCircle * 1.5f;
                candidate += new Vector3(jitter.x, 0, jitter.y);

                // 1. Minimum distance from player check (10m)
                if (Vector3.Distance(candidate, playerPos) < 10.0f) continue;

                // 2. Minimum distance from all active enemies check (2m)
                bool tooCloseToOtherEnemy = false;
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    if (activeEnemies[i] != null && Vector3.Distance(candidate, activeEnemies[i].transform.position) < 2.0f)
                    {
                        tooCloseToOtherEnemy = true;
                        break;
                    }
                }
                if (tooCloseToOtherEnemy) continue;

                // 3. Playable Arena Bounds Check
                if (new Vector2(candidate.x, candidate.z).magnitude > arenaRadius) continue;

                // 4. Ground raycast check (must hit ground near y=0)
                if (Physics.Raycast(candidate + Vector3.up * 10f, Vector3.down, out RaycastHit groundHit, 15f))
                {
                    if (groundHit.point.y > 2.0f) continue; // Cliff or obstacle top
                    candidate.y = groundHit.point.y + 0.5f;
                }

                // 5. Physics obstacle overlap check
                if (Physics.CheckSphere(candidate, 0.8f)) continue;

                lastSpawnIndex = index;
                validPos = candidate;
                return true;
            }

            return false;
        }

        private GameObject CreateEnemyPrimitiveInstance(EnemyType type, Vector3 position)
        {
            GameObject obj;
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();

            switch (type)
            {
                case EnemyType.Gnome:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    obj.name = "Gnome_Enemy";
                    data.enemyName = "Gnome";
                    data.enemyType = EnemyType.Gnome;
                    data.maxHealth = 35f;
                    data.moveSpeed = 4.2f;
                    data.attackDamage = 8f;
                    data.attackRange = 1.8f;
                    data.attackCooldown = 1.8f;
                    data.xpReward = 10;
                    data.enemyColor = new Color(0.25f, 0.45f, 0.18f);
                    data.modelScale = new Vector3(0.8f, 0.8f, 0.8f);
                    
                    obj.AddComponent<CharacterController>();
                    var gnomeAI = obj.AddComponent<SlimeAI>();
                    SetEnemyDataField(gnomeAI, data);
                    break;

                case EnemyType.MiniTree:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    obj.name = "MiniTree_Enemy";
                    data.enemyName = "Mini Tree";
                    data.enemyType = EnemyType.MiniTree;
                    data.maxHealth = 60f;
                    data.moveSpeed = 4.8f;
                    data.attackDamage = 14f;
                    data.attackRange = 2.0f;
                    data.attackCooldown = 1.6f;
                    data.xpReward = 20;
                    data.enemyColor = new Color(0.32f, 0.20f, 0.12f);
                    data.modelScale = new Vector3(1.0f, 1.2f, 1.0f);

                    obj.AddComponent<CharacterController>();
                    var miniTreeAI = obj.AddComponent<GoblinAI>();
                    SetEnemyDataField(miniTreeAI, data);
                    break;

                case EnemyType.Creature:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = "Creature_Enemy";
                    data.enemyName = "Creature";
                    data.enemyType = EnemyType.Creature;
                    data.maxHealth = 50f;
                    data.moveSpeed = 6.5f;
                    data.attackDamage = 12f;
                    data.attackRange = 2.2f;
                    data.attackCooldown = 2.2f;
                    data.xpReward = 20;
                    data.enemyColor = new Color(0.18f, 0.12f, 0.22f);
                    data.modelScale = new Vector3(1.1f, 0.9f, 1.8f);

                    obj.AddComponent<CharacterController>();
                    var creatureAI = obj.AddComponent<WolfAI>();
                    SetEnemyDataField(creatureAI, data);
                    break;

                case EnemyType.Boss:
                default:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    obj.name = "MiniBoss_Enemy";
                    data.enemyName = "Mini Boss";
                    data.enemyType = EnemyType.Boss;
                    data.maxHealth = 500f;
                    data.moveSpeed = 3.8f;
                    data.attackDamage = 28f;
                    data.attackRange = 3.0f;
                    data.attackCooldown = 1.8f;
                    data.xpReward = 300;
                    data.enemyColor = new Color(0.7f, 0.1f, 0.1f);
                    data.modelScale = new Vector3(2.5f, 2.5f, 2.5f);

                    obj.AddComponent<CharacterController>();
                    var bossAI = obj.AddComponent<BossAI>();
                    SetEnemyDataField(bossAI, data);
                    break;
            }

            obj.transform.position = position;
            return obj;
        }

        private void SetEnemyDataField(EnemyBase enemyScript, EnemyData data)
        {
            var field = typeof(EnemyBase).GetField("enemyData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(enemyScript, data);
            }
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            activeEnemies.Remove(enemy);
            FindFirstObjectByType<RunManager>()?.RegisterKill();

            if (activeEnemies.Count == 0 && isWaveInProgress)
            {
                isWaveInProgress = false;
                if (currentWaveIndex < 4)
                {
                    StartCoroutine(StartNextWaveWithDelay(3.0f));
                }
            }
        }
    }
}
