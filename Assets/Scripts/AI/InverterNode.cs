namespace Game.AI
{
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