namespace Game.AI
{
    /// <summary>
    /// 选择节点，依次尝试子节点，成功则整体成功并返回。
    /// </summary>
    //
    // 语义：依次尝试子节点，找到第一个可行的就执行。
    //   - 遇到 Running → 返回 Running（这个正在执行）
    //   - 遇到 Success → 返回 Success（立刻短路，后面的不执行）
    //   - 全部 Failure → 返回 Failure（没有可选分支）
    //
    // 内涵：决策优先级
    //   Selector [ 攻击分支, 追击分支, 巡逻分支 ]
    //   优先级从高到低：能攻击就攻击，不行就追，再不行就巡逻。
    public class SelectorNode : CompositeNode
    {
        public SelectorNode(params Node[] childNodes) : base(childNodes) { }
        public override NodeState Evaluate()
        {
            foreach (Node child in children)
            {
                NodeState childState = child.Evaluate();
                if (childState != NodeState.Failure)
                    return State = childState;
            }
            return State = NodeState.Failure;
        }
    }
}