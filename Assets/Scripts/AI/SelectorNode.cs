namespace Game.AI
{
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