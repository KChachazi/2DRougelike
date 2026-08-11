using Unity.VisualScripting;
using UnityEngine;

namespace Game.AI
{
    public class BehaviourTreeTester : MonoBehaviour
    {
        public float FakeDistance { get; private set; } = 5f;
        private BehaviourTree tree;
        private void Awake()
        {
            tree = new BehaviourTree(new Blackboard(), BuildDemoTree());
        }
        private void Update()
        {
            FakeDistance = 3f + Random.value * 5f;
            tree.Evaluate();
        }

        private Node BuildDemoTree()
        {
            return new SelectorNode(
                new SequenceNode(
                    new DistanceBelowCondition(this, 3.5f),
                    new LogAction("攻击！！")
                ),
                new LogAction("巡逻！！")
            );
        }

        private class DistanceBelowCondition : ConditionNode
        {
            private readonly BehaviourTreeTester host;
            private readonly float threshold;
            public DistanceBelowCondition(BehaviourTreeTester host, float threshold)
            {
                this.host = host;
                this.threshold = threshold;
            }
            protected override bool Check() => host.FakeDistance < threshold;
        }
        private class LogAction : ActionNode
        {
            private readonly string message;
            public LogAction(string message) { this.message = message; }
            public override NodeState Evaluate()
            {
                Debug.Log(message);
                return NodeState.Success;
            }
        }
    }
}