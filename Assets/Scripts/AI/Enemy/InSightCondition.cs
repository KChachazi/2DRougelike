using Game.Entities;

namespace Game.AI
{
    /// <summary>
    /// 条件：该敌人的感知快照是否处于警戒状态。
    /// </summary>
    //
    // 节点本身不负责计算距离或维护迟滞；
    // 迟滞统一由 EnemyBrain.UpdatePerception 更新。
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