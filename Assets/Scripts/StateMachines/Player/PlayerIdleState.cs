using Game.Entities;

namespace Game.StateMachines.Player
{
    /// <summary>玩家待机状态，对应无移动输入。</summary>
    public class PlayerIdleState : IState
    {
        private readonly PlayerController player;
        private readonly StateMachine stateMachine;

        public PlayerIdleState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter() {}

        public void Tick()
        {
            if (player.MoveInput.sqrMagnitude > 0.01f)
            {
                stateMachine.ChangeState(player.MoveState);
                return ;
            }
        }

        public void FixedTick() {}
        public void Exit() {}
    }
}