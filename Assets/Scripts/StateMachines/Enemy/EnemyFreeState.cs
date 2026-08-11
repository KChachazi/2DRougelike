using Game.Entities;

namespace Game.StateMachines.Enemy
{
    public class EnemyFreeState : IState
    {
        public EnemyFreeState(EnemyController enemy, StateMachine stateMachine) { }
        public void Enter() { }
        public void Tick() { }
        public void FixedTick() { }
        public void Exit() { }
    }
}