namespace Game.AI
{
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