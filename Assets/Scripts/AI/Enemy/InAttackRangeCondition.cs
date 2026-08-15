using Game.Entities;

namespace Game.AI
{
    /// <summary>
    /// 条件：玩家是否在近战攻击范围内。
    /// </summary>
    // 对应原 FSM 里 Chase→Attack 的进入条件。
    public class InAttackRangeCondition : ConditionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        public InAttackRangeCondition(EnemyController enemy, EnemyBehaviour behaviour)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
        }
        protected override bool Check()
        {
            return enemy.DistanceToPlayer() <= behaviour.attackRange;
        }
    }
}