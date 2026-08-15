namespace Game.AI
{
    /// <summary>
    /// 动作节点抽象基类——执行真正的行为。
    /// </summary>
    //
    // 动作可以返回三种结果：
    //   Success —— 动作完成（如"攻击了一刀"）
    //   Running —— 动作还在持续（如"正在移动过去"，需要多帧）
    //   Failure —— 动作无法执行（如"玩家不在，没法攻击"）
    //
    // 典型子类：
    //   ChaseAction  —— 追向玩家（返回 Running）
    //   MeleeAttackAction —— 砍一刀（返回 Success）
    //   PatrolAction —— 巡逻（返回 Running）
    public abstract class ActionNode : Node
    {
        // ActionNode 本身不添加新接口，只作为语义标记基类。
        // 子类直接实现 Evaluate()。
    }
}