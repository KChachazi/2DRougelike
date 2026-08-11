using UnityEngine;

namespace Game.AI
{
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