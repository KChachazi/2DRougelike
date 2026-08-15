namespace Game.AI
{
    /// <summary>
    /// 顺序节点，所有子节点都成功，整体才成功。
    /// </summary>
    //
    // 语义：依次执行子节点，逐个检查。
    //   - 遇到 Running → 返回 Running（当前的还在跑，后面的还没开始）
    //   - 遇到 Failure → 返回 Failure（立刻短路，后面的不执行）
    //   - 全部 Success → 返回 Success
    //
    // 典型用途：表达"先判断再行动"的链
    //   Sequence [ 在视野? → 在攻击范围? → 攻击 ]
    public class SequenceNode : CompositeNode
    {
        public SequenceNode(params Node[] childNodes) : base(childNodes) { }
        public override NodeState Evaluate()
        {
            foreach (Node child in children)
            {
                NodeState childState = child.Evaluate();
                if (childState != NodeState.Success)
                    return State = childState;
            }
            return State = NodeState.Success;
        }
    }
}