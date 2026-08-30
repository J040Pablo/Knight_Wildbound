using System.Collections.Generic;
using UnityEngine;
using Roguelite.Data;
using Roguelite.Enemy;

namespace Roguelite.Core.Managers
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float minPlayerDistance = 10f;
        [SerializeField] private float minEnemyDistance = 2.5f;

        private readonly List<GameObject> activeEnemies = new List<GameObject>();

        [Header("Population Limits")]
        [SerializeField] private int maxActiveFairies = 12;
        [SerializeField] private int maxActiveStoneGiants = 3;
        [SerializeField] private int maxActiveColossus = 1;
        [SerializeField] private int maxActiveFairyQueens = 1;

        public IReadOnlyList<GameObject> ActiveEnemies => activeEnemies;

        public bool CanSpawnEnemy(EnemyDefinition definition)
        {
            if (definition == null) return false;
            activeEnemies.RemoveAll(e => e == null);

            int currentFairies = 0;
            int currentGiants = 0;
            int currentColossus = 0;
            int currentQueens = 0;

            foreach (var e in activeEnemies)
            {
                if (e.GetComponent<FairyEnemyAI>() != null) currentFairies++;
                if (e.GetComponent<StoneGiantAI>() is StoneGiantAI sg)
                {
                    if (sg.IsColossus) currentColossus++;
                    else currentGiants++;
                }
                if (e.GetComponent<FairyQueenAI>() != null) currentQueens++;
            }

            if (definition.enemyName.Contains("Fairy") && currentFairies >= maxActiveFairies) return false;
            if (definition.enemyName.Contains("Giant") && currentGiants >= maxActiveStoneGiants) return false;
            if (definition.enemyName.Contains("Colossus") && currentColossus >= maxActiveColossus) return false;
            if (definition.enemyName.Contains("Queen") && currentQueens >= maxActiveFairyQueens) return false;

            return true;
        }

        public GameObject SpawnEnemy(EnemyDefinition definition, Vector3 position)
        {
            if (definition == null || definition.prefab == null)
            {
                Debug.LogWarning("[EnemySpawner] Missing enemy definition or prefab!");
                return null;
            }

            if (!CanSpawnEnemy(definition))
            {
                Debug.LogWarning($"[EnemySpawner] Active limit reached for {definition.enemyName}!");
                return null;
            }

            Vector3 validPos = GetValidPosition(position);
            GameObject enemyGO = Instantiate(definition.prefab, validPos, Quaternion.identity);
            enemyGO.name = definition.enemyName;

            EnemyBase enemyComp = enemyGO.GetComponent<EnemyBase>();
            if (enemyComp != null)
            {
                enemyComp.InitializeWithDefinition(definition);
                enemyComp.OnEnemyDied += HandleEnemyDied;
            }

            activeEnemies.Add(enemyGO);
            Debug.Log($"[EnemySpawner] Spawned {definition.enemyName} at {validPos}");
            return enemyGO;
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            if (enemy != null && activeEnemies.Contains(enemy.gameObject))
            {
                activeEnemies.Remove(enemy.gameObject);
            }
        }

        private Vector3 GetValidPosition(Vector3 preferredPos)
        {
            Vector3 rayStart = preferredPos + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 25f))
            {
                return hit.point + Vector3.up * 0.1f;
            }
            return preferredPos;
        }

        public void ClearEnemies()
        {
            activeEnemies.RemoveAll(e => e == null);
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null) Destroy(enemy);
            }
            activeEnemies.Clear();
        }
    }
}
