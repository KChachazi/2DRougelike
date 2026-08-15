namespace Game.AI
{
    /// <summary>
    /// 行为树节点的三种返回值。
    /// </summary>
    //
    //  Running —— 正在执行（动作需要多帧，如移动/攻击前摇）
    //  Success —— 成功（条件成立 / 动作完成）
    //  Failure —— 失败（条件不成立 / 动作无法执行）
    //
    // 行为树的核心：
    //    父节点每次评估时调用子节点的 Evaluate()，
    //    依据返回值决定接下来走哪个分支。
    public enum NodeState
    {
        Running,
        Success,
        Failure,
    }
}