using UnityEngine;

namespace Roguelite.Data
{
    public enum EnemyType
    {
        Slime,
        Goblin,
        Wolf,
        Boss
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Roguelite/Data/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public string enemyName = "Slime";
        public EnemyType enemyType = EnemyType.Slime;
        
        [Header("Stats")]
        public float maxHealth = 50f;
        public float moveSpeed = 3.5f;
        public float attackDamage = 10f;
        public float attackRange = 1.8f;
        public float attackCooldown = 1.5f;
        public int xpReward = 20;
        
        [Header("Visual Properties")]
        public Color enemyColor = Color.green;
        public Vector3 modelScale = Vector3.one;
    }
}
