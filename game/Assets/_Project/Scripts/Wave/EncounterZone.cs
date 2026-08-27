using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelite.Enemy;

namespace Roguelite.Wave
{
    public enum EncounterDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    public class EncounterZone : MonoBehaviour
    {
        [Header("Encounter Config")]
        [SerializeField] private EncounterDifficulty difficulty = EncounterDifficulty.Easy;
        [SerializeField] private GameObject forwardGate;
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        public EncounterDifficulty Difficulty => difficulty;
        public bool IsActive { get; private set; } = false;
        public bool IsCompleted { get; private set; } = false;

        public int EnemiesRemaining => activeEnemies.Count;

        private List<EnemyBase> activeEnemies = new List<EnemyBase>();

        public event Action<EncounterZone> OnEncounterStarted;
        public event Action<EncounterZone> OnEncounterCompleted;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive && !IsCompleted && other.CompareTag("Player"))
            {
                StartEncounter();
            }
        }

        public void StartEncounter()
        {
            IsActive = true;

            // Lock forward path
            if (forwardGate != null)
            {
                forwardGate.SetActive(true);
            }

            // Spawn predefined enemy group based on difficulty
            SpawnEnemiesForDifficulty();

            OnEncounterStarted?.Invoke(this);
        }

        private void SpawnEnemiesForDifficulty()
        {
            activeEnemies.Clear();

            // Fallback spawn points if none assigned
            if (spawnPoints.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    GameObject sp = new GameObject($"SpawnPoint_{i}");
                    sp.transform.position = transform.position + Quaternion.Euler(0, i * 90, 0) * Vector3.forward * 6f;
                    spawnPoints.Add(sp.transform);
                }
            }

            switch (difficulty)
            {
                case EncounterDifficulty.Easy:
                    // Slimes + Pumpkin enemies (tutorial combat)
                    SpawnEnemy<PumpkinEnemyAI>(spawnPoints[0].position, false);
                    SpawnEnemy<PumpkinEnemyAI>(spawnPoints[1 % spawnPoints.Count].position, false);
                    SpawnEnemy<SlimeAI>(spawnPoints[2 % spawnPoints.Count].position, false);
                    break;

                case EncounterDifficulty.Medium:
                    // Goblins + Wolves + Pumpkins
                    SpawnEnemy<PumpkinEnemyAI>(spawnPoints[0].position, false);
                    SpawnEnemy<WolfAI>(spawnPoints[1 % spawnPoints.Count].position, false);
                    SpawnEnemy<GoblinAI>(spawnPoints[2 % spawnPoints.Count].position, false);
                    SpawnEnemy<SlimeAI>(spawnPoints[3 % spawnPoints.Count].position, false);
                    break;

                case EncounterDifficulty.Hard:
                    // Elite Pumpkin + Wolves + Goblins + Slimes
                    var elitePumpkin = SpawnEnemy<PumpkinEnemyAI>(spawnPoints[0].position, true);
                    if (elitePumpkin != null) elitePumpkin.SetEliteStatus(true);

                    SpawnEnemy<WolfAI>(spawnPoints[1 % spawnPoints.Count].position, false);
                    SpawnEnemy<WolfAI>(spawnPoints[2 % spawnPoints.Count].position, false);
                    SpawnEnemy<GoblinAI>(spawnPoints[3 % spawnPoints.Count].position, false);
                    SpawnEnemy<GoblinAI>(spawnPoints[0 % spawnPoints.Count].position + Vector3.right * 2f, false);
                    break;
            }
        }

        private T SpawnEnemy<T>(Vector3 position, bool isElite) where T : EnemyBase
        {
            Vector3 validSpawnPos = position;

            // Validate spawn position against ground & obstacles
            if (Roguelite.Core.SpawnManager.Instance != null)
            {
                if (Roguelite.Core.SpawnManager.Instance.ValidateSpawnPosition(position, 0.6f, out Vector3 validatedPos))
                {
                    validSpawnPos = validatedPos;
                }
                else
                {
                    // Fallback downward raycast
                    if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
                    {
                        validSpawnPos = hit.point + Vector3.up * 0.1f;
                    }
                }
            }

            GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObj.name = $"EncounterEnemy_{typeof(T).Name}";
            enemyObj.transform.position = validSpawnPos;

            CharacterController cc = enemyObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0, 0.9f, 0);

            T enemyComp = enemyObj.AddComponent<T>();
            enemyComp.OnEnemyDied += HandleEnemyDied;

            activeEnemies.Add(enemyComp);
            return enemyComp;
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            if (activeEnemies.Contains(enemy))
            {
                activeEnemies.Remove(enemy);
            }

            if (activeEnemies.Count == 0 && IsActive)
            {
                CompleteEncounter();
            }
        }

        private void CompleteEncounter()
        {
            IsActive = false;
            IsCompleted = true;

            // Unlock forward gate
            if (forwardGate != null)
            {
                forwardGate.SetActive(false);
            }

            OnEncounterCompleted?.Invoke(this);
        }
    }
}
