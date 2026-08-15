using Game.Entities;

namespace Game.AI
{
    /// <summary>
    /// 条件：玩家进入了引爆范围。
    /// </summary>
    public class InExplodeRangeCondition : ConditionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        public InExplodeRangeCondition(EnemyController enemy, EnemyBehaviour behaviour)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
        }
        protected override bool Check()
        {
            return enemy.DistanceToPlayer() <= behaviour.explodeRange;
        }
    }
}