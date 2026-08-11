namespace Game.AI
{
    public abstract class Node
    {
        public NodeState State { get; protected set; }
        public abstract NodeState Evaluate();
        public void Reset() => State = NodeState.Running;
    }
}