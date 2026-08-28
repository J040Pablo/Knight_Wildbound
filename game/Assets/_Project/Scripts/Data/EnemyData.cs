using UnityEngine;

namespace Roguelite.Data
{
    public enum EnemyType
    {
        Gnome,
        MiniTree,
        Creature,
        Boss,
        Slime = Gnome,
        Goblin = MiniTree,
        Wolf = Creature
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Roguelite/Data/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public string enemyName = "Gnome";
        public EnemyType enemyType = EnemyType.Gnome;
        
        [Header("Stats")]
        public float maxHealth = 35f;
        public float moveSpeed = 4.2f;
        public float attackDamage = 8f;
        public float attackRange = 1.8f;
        public float attackCooldown = 1.8f;
        public int xpReward = 10;
        
        [Header("Visual Properties")]
        public Color enemyColor = Color.green;
        public Vector3 modelScale = Vector3.one;
    }
}
