using Unity.VisualScripting;
using UnityEngine;

namespace Game.AI
{
    public class CooldownNode : Node
    {
        private readonly Node child;
        private readonly float cooldownDuration;
        private float timer;
        public CooldownNode(Node child, float cooldownDuration)
        {
            this.child = child;
            this.cooldownDuration = cooldownDuration;
        }
        public override NodeState Evaluate()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                return State = NodeState.Failure;
            }
            NodeState childState = child.Evaluate();
            if (childState == NodeState.Success)
                timer = cooldownDuration;
            return State = childState;
        }
    }
}