namespace Game.AI
{
    public abstract class ActionNode : Node
    {
        // ActionNode 本身不添加新接口，只作为语义标记基类。
        // 子类直接实现 Evaluate()。
    }
}