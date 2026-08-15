using Game.Entities;

namespace Game.AI
{
    /// <summary>
    /// 条件：玩家处于适当的射击范围内，处于 minShootRange 与 shootRange 之间。
    /// </summary>
    public class InShootRangeCondition : ConditionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        public InShootRangeCondition(EnemyController enemy, EnemyBehaviour behaviour)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
        }
        protected override bool Check()
        {
            float distance = enemy.DistanceToPlayer();
            return distance <= behaviour.shootRange && distance >= behaviour.minShootRange;
        }
    }
}