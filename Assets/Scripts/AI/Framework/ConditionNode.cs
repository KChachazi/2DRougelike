namespace Game.AI
{
    /// <summary>
    /// 条件节点抽象基类，只做判断，不做动作。
    /// </summary>
    public abstract class ConditionNode : Node
    {
        public override NodeState Evaluate()
        {
            return State = Check() ? NodeState.Success : NodeState.Failure;
        }
        /// <summary>子类实现：判断条件是否成立</summary>
        protected abstract bool Check();
    }
}