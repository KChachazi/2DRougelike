using UnityEngine;
namespace Game.Entities
{
    public enum EnemyType
    {
        Melee,
        Ranged,
        Bomber,
        Boss,
    }

    [CreateAssetMenu(fileName = "NewEnemyBehaviour", menuName = "Game/Enemy Behavior")]
    public class EnemyBehaviour : ScriptableObject
    {
        [Header("=== 类型 ===")]
        public EnemyType type = EnemyType.Melee;

        [Header("=== 巡逻 ===")]
        public float patrolSpeed = 1.2f;
        public float patrolRadius = 2f;

        [Header("=== 追击 ===")]
        public float chaseSpeed = 2.5f;
        [Tooltip("探测范围")]
        public float detectionRange = 4f;
        [Tooltip("逃脱范围")]
        public float lostSightRange = 6f;

        [Header("=== 近战攻击 ===")]
        [Tooltip("近战判定距离")]
        public float attackRange = 1f;
        public int contactDamage = 10;
        public float attackCooldown = 1f;

        [Header("=== 击退 ===")]
        public float knockbackDuration = 0.15f;
        public float knockbackSpeedMultiplier = 10f;

        [Header("=== 远程专属 ===")]
        [Tooltip("最远射程")]
        public float shootRange = 6f;
        [Tooltip("保持距离")]
        public float minShootRange = 2f;
        public float projectileSpeed = 8f;
        public int projectileDamage = 8;
        public float shootCooldown = 1.2f;

        [Header("=== 自爆专属 ===")]
        [Tooltip("自爆距离")]
        public float explodeRange = 1.5f;
        public int explodeDamage = 30;

        [Header("=== Boss专属 ===")]
        [Tooltip("血量比例阈值")]
        public float[] phaseThresholds;
        [Tooltip("Boss 冷却间隔")]
        public float skillCooldown = 3f;
        public int skillDamage = 15;
        [Tooltip("灼烧效果")]
        public float skillBurnDuration = 3f;
        public int skillBurnDamage = 3;
        public float skillBurnTickInterval = 0.5f;
    }
}