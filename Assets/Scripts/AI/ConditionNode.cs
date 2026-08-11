using System.ComponentModel.Design;

namespace Game.AI
{
    public abstract class ConditionNode : Node
    {
        public override NodeState Evaluate()
        {
            return State = Check() ? NodeState.Success : NodeState.Failure;
        }
        protected abstract bool Check();
    }
}