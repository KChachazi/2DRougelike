using Game.Entities;

namespace Game.AI
{
    public class InSightCondition : ConditionNode
    {
        private readonly EnemyPerceptionData perception;
        public InSightCondition(Blackboard blackboard)
        {
            perception = blackboard.Get<EnemyPerceptionData>(EnemyBlackboardKeys.Perception);
        }
        protected override bool Check()
        {
            return perception != null && perception.IsAlerted;
        }
    }
}