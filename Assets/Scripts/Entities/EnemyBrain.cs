using UnityEngine;
using Game.AI;
using Game.Core;
using Game.Debug;

namespace Game.Entities
{
    /// <summary>
    /// 敌人的主动决策入口。击退、死亡等仍由 FSM 负责。
    /// </summary>
    //
    // 被动状态生效时，EnemyController.IsActionLocked 会暂停行为树。
    // FixedUpdate 的顺序固定为：
    // 1. 更新感知快照；
    // 2. 把快照保存在该敌人自己的 Blackboard 中；
    // 3. 评估只读取快照的行为树。
    [RequireComponent(typeof(EnemyController))]
    public class EnemyBrain : MonoBehaviour
    {
        [Tooltip("敌人子弹池")]
        [SerializeField] private ObjectPool enemyProjectilePool;

        private EnemyController enemy;
        private Blackboard blackboard;
        private BehaviourTree tree;
        private EnemyPerceptionData perception;
        private void Awake()
        {
            enemy = GetComponent<EnemyController>();
            blackboard = new Blackboard();
            perception = new EnemyPerceptionData();
            blackboard.Set(EnemyBlackboardKeys.Perception, perception);
        }
        private void Start()
        {
            // EnemyController 已在 Awake 中记录 SpawnPosition，
            // 在 Start 建树可避免 PatrolAction 过早读取未初始化的出生点。
            tree = BuildTree(enemy.Behaviour);
            if (tree == null)
                enabled = false;
        }
        private void FixedUpdate()
        {
            if (enemy.IsActionLocked) return ;
            UpdatePerception();
            tree.Evaluate();
        }
        // ======================== 私有工具 ========================
        /// <summary>
        /// 每个物理帧只在此处计算一次视野状态。
        /// </summary>
        private void UpdatePerception()
        {
            perception.Target = enemy.Player;
            if (perception.Target == null)
            {
                perception.DistanceToTarget = float.MaxValue;
                perception.IsAlerted = false;
                return ;
            }
            float distance = enemy.DistanceToPlayer();
            perception.DistanceToTarget = distance;
            if (!perception.IsAlerted && distance <= enemy.Behaviour.detectionRange)
                perception.IsAlerted = true;
            else if (perception.IsAlerted && distance >= enemy.Behaviour.lostSightRange)
                perception.IsAlerted = false;
        }
        /// <summary>
        /// 阶段阈值必须按从高到低排列，例如 [0.7, 0.3]。
        /// 只在建树时检查一次，避免行为树每个物理帧重复打印错误。
        /// </summary>
        private bool ValidateBossPhaseThresholds(EnemyBehaviour behaviour, int expectedPhaseCount)
        {
            float[] thresholds = behaviour.phaseThresholds;
            if (thresholds == null || thresholds.Length != expectedPhaseCount - 1)
            {
                GameDebug.Error(DebugCategory.AI,$"当前 Boss 树需要 {expectedPhaseCount - 1} 个阶段阈值。", this);
                return false;
            }
            float previous = 1f;
            for (int i = 0; i < thresholds.Length; i ++)
            {
                float current = thresholds[i];
                if (current <= 0f || current >= previous)
                {
                    GameDebug.Error(DebugCategory.AI, $"阶段阈值必须在 0~1 之间，且从高到低排列。", this);
                    return false;
                }
                previous = current;
            }
            return true;
        }
        // ======================== 行为树构建 ========================
        private BehaviourTree BuildTree(EnemyBehaviour behaviour)
        {
            switch (behaviour.type)
            {
                case EnemyType.Melee:  return BuildMeleeTree(behaviour);
                case EnemyType.Ranged: return BuildRangedTree(behaviour);
                case EnemyType.Bomber: return BuildBomberTree(behaviour);
                case EnemyType.Boss:   return BuildBossTree(behaviour);
                default:
                    GameDebug.Error(DebugCategory.AI, "未指定的怪物类型，无法创建行为树。", this);
                    return null;
            }
        }
        private BehaviourTree BuildMeleeTree(EnemyBehaviour b)
        {
            Node root = new SelectorNode(
                new SequenceNode(
                    new InSightCondition(blackboard),
                    new InAttackRangeCondition(enemy, b),
                    new MeleeAttackAction(enemy, b)
                ),
                new SequenceNode(
                    new InSightCondition(blackboard),
                    new ChaseAction(enemy, b)
                ),
                new PatrolAction(enemy, b)
            );
            return new BehaviourTree(blackboard, root);
        }
        private BehaviourTree BuildRangedTree(EnemyBehaviour b)
        {
            Node root = new SelectorNode(
                new SequenceNode(
                    new InSightCondition(blackboard),
                    new TooCloseCondition(enemy, b),
                    new KeepDistanceAction(enemy, b)
                ),
                new SequenceNode(
                    new InSightCondition(blackboard),
                    new InShootRangeCondition(enemy, b),
                    new ShootAction(enemy, b, enemyProjectilePool)
                ),
                new SequenceNode(
                    new InSightCondition(blackboard),
                    new ChaseAction(enemy, b)
                ),
                new PatrolAction(enemy, b)
            );
            return new BehaviourTree(blackboard, root);
        }
        private BehaviourTree BuildBomberTree(EnemyBehaviour b)
        {
            Node root = new SelectorNode(
                new SequenceNode(
                    new InSightCondition(blackboard),
                    new InExplodeRangeCondition(enemy, b),
                    new ExplodeAction(enemy, b)
                ),
                new SequenceNode(
                    new InSightCondition(blackboard),
                    new ChaseAction(enemy, b)
                ),
                new PatrolAction(enemy, b)
            );
            return new BehaviourTree(blackboard, root);
        }
        private BehaviourTree BuildBossTree(EnemyBehaviour b)
        {
            if (!ValidateBossPhaseThresholds(b, 2))
                return null;
            Node root = new SelectorNode(
                new SequenceNode(
                    new BossPhaseCondition(enemy, b, 2),
                    new InSightCondition(blackboard),
                    new BossSkillAction(enemy, b, true, enemyProjectilePool)
                ),
                new SequenceNode(
                    new BossPhaseCondition(enemy, b, 1),
                    new InSightCondition(blackboard),
                    new InAttackRangeCondition(enemy, b),
                    new BossSkillAction(enemy, b, false, enemyProjectilePool)
                ),
                new SequenceNode(
                    new InSightCondition(blackboard),
                    new ChaseAction(enemy, b)
                ),
                new PatrolAction(enemy, b)
            );
            return new BehaviourTree(blackboard, root);
        }
    }
}
