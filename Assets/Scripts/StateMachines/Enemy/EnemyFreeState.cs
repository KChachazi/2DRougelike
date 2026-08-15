using Game.Entities;

namespace Game.StateMachines.Enemy
{
    /// <summary>
    /// 自由状态，当前主要行为由行为树接管。
    /// </summary>
    //
    // 分层设计里，状态机只负责被动打断，主动决策则交给行为树。
    public class EnemyFreeState : IState
    {
        public EnemyFreeState(EnemyController enemy, StateMachine stateMachine) { }
        public void Enter() { }
        public void Tick() { }
        public void FixedTick() { }
        public void Exit() { }
    }
}