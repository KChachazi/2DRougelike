using Game.Entities;

namespace Game.AI
{
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