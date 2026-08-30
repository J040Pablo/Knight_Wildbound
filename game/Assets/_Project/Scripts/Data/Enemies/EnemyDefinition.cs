using UnityEngine;
using Roguelite.Loot;

namespace Roguelite.Data
{
    [CreateAssetMenu(fileName = "NewEnemyDefinition", menuName = "Roguelite/Data/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName = "Goblin";
        public EnemyType enemyType = EnemyType.Goblin;

        [Header("Base Stats")]
        public float maxHealth = 40f;
        public float damage = 10f;
        public float moveSpeed = 4.5f;
        public float attackRange = 1.8f;
        public float attackCooldown = 1.8f;
        public int xpReward = 10;

        [Header("Assets & Prefab")]
        public GameObject prefab;
        public ChestRarity dropRarity = ChestRarity.Common;
        public Color enemyColor = Color.green;
        public Vector3 modelScale = Vector3.one;
    }
}
