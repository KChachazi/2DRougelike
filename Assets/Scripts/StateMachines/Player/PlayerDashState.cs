using Game.Entities;
using Game.StateMachines.Player;
using UnityEngine;

namespace Game.StateMachines.Player
{
    public class PlayerDashState : IState
    {
        private readonly PlayerController player;
        private readonly StateMachine stateMachine;
        private Vector2 dashDirection;
        private float timer;

        public PlayerDashState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            timer = 0f;
            dashDirection = player.MoveInput.sqrMagnitude > 0.01f
                ? player.MoveInput : (Vector2)player.transform.right;
            player.health.SetInvincible(player.DashDuration);
            player.StartDashCooldown();
        }

        public void Tick()
        {
            timer += Time.deltaTime;
            if (timer >= player.DashDuration)
            {
                stateMachine.ChangeState(player.MoveInput.sqrMagnitude > 0.01f ? player.MoveState : player.IdleState);
            }
        }

        public void FixedTick()
        {
            player.Rb.MovePosition(player.Rb.position + dashDirection * player.DashSpeed * Time.fixedDeltaTime);
        }

        public void Exit() { }
    }
}