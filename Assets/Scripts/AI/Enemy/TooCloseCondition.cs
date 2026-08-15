using Game.Entities;

namespace Game.AI
{
    /// <summary>
    /// 条件：距离玩家过近，即小于 minShootRange。
    /// </summary>
    public class TooCloseCondition : ConditionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        public TooCloseCondition(EnemyController enemy, EnemyBehaviour behaviour)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
        }
        protected override bool Check()
        {
            return enemy.DistanceToPlayer() < behaviour.minShootRange;
        }
    }
}