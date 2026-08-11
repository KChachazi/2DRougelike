using Game.Entities;

namespace Game.AI
{
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