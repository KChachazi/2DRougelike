namespace Game.AI
{
    /// <summary>
    /// 行为树运行器：持有根节点与构造时注入的 Blackboard。
    /// 调用 <see cref="Evaluate"/> 会从根节点开始评估一次完整决策。
    /// </summary>
    public class BehaviourTree
    {
        public Blackboard Blackboard { get; }
        private readonly Node root;
        public BehaviourTree(Blackboard blackboard, Node root)
        {
            Blackboard = blackboard;
            this.root = root;
        }
        public NodeState Evaluate() => root.Evaluate();
    }
}