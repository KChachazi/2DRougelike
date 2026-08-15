namespace Game.StateMachines
{
    /// <summary>状态生命周期契约：进入、逻辑帧、物理帧和退出。</summary>
    public interface IState
    {
        void Enter();
        void Tick();
        void FixedTick();
        void Exit();
    }
}