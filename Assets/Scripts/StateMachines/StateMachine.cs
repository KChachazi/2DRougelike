namespace Game.StateMachines
{
    /// <summary>持有当前状态，并保证状态切换时按顺序执行 Exit 与 Enter。</summary>
    public class StateMachine
    {
        public IState CurrentState { get; private set; }

        public void ChangeState(IState nextState)
        {
            if (nextState == CurrentState) return ;
            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState?.Enter();
        }

        public void Tick() => CurrentState?.Tick();
        public void FixedTick() => CurrentState?.FixedTick();
    }
}