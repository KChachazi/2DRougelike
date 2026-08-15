namespace Game.AI
{
    /// <summary>
    /// 行为树节点抽象基类，一切节点都继承自它，
    /// 唯一接口是 Evaluate()，每次行为树评估时调用，返回 NodeState。
    /// </summary>
    public abstract class Node
    {
        /// <summary>本节点最近一次 Evaluate 的返回值（只读，供调试）</summary>
        public NodeState State { get; protected set; }
        
        /// <summary>
        /// 评估本节点。
        /// 子类必须实现。
        /// </summary>
        public abstract NodeState Evaluate();

        /// <summary>
        /// 仅将本节点缓存的 State 重置为 Running；
        /// 不会递归重置子节点、冷却或动作内部状态。
        /// </summary>
        public void Reset() => State = NodeState.Running;
    }
}