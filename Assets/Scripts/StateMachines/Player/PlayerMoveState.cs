using Game.Entities;

namespace Game.StateMachines.Player
{
    public class PlayerMoveState : IState
    {
        private readonly PlayerController player;
        private readonly StateMachine stateMachine;

        public PlayerMoveState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter() { }
        public void Tick()
        {
            if (player.ConsumeDashPressed() && player.CanDash)
            {
                stateMachine.ChangeState(player.DashState);
                return ;
            }
            if (player.ConsumeAttackPressed())
            {
                stateMachine.ChangeState(player.AttackState);
                return ;
            }
            if (player.MoveInput.sqrMagnitude < 0.01f)
            {
                stateMachine.ChangeState(player.IdleState);
            }
        }

        public void FixedTick()
        {
            player.Move(player.MoveSpeed);
        }

        public void Exit() { }
    }
}