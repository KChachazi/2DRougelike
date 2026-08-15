namespace Game.AI
{
    /// <summary>
    /// 取反装饰器（NOT），包装一个子节点，将其翻转。
    /// </summary>
    //
    //   Success → Failure
    //   Failure → Success
    //   Running → Running
    //
    // 典型用途：表达"不在范围才……"这类否定条件。
    //   例如"玩家不在攻击范围" = Inverter( InAttackRange )
    public class InverterNode : Node
    {
        private readonly Node child;
        public InverterNode(Node child) => this.child = child;
        public override NodeState Evaluate()
        {
            NodeState childState = child.Evaluate();
            if (childState == NodeState.Success) return State = NodeState.Failure;
            if (childState == NodeState.Failure) return State = NodeState.Success;
            return State = NodeState.Running;
        }
    }
}