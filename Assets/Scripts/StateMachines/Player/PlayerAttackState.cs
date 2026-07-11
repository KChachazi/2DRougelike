using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Player
{
    public class PlayerAttackState : IState
    {
        private readonly PlayerController player;
        private readonly StateMachine stateMachine;
        private float timer;
        private Color originalColor;

        public PlayerAttackState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            timer = 0f;
            originalColor = player.SpriteRenderer.color;
            player.SpriteRenderer.color = Color.yellow;
        }

        public void Tick()
        {
            timer += Time.deltaTime;
            if (timer >= player.AttackDuration)
            {
                stateMachine.ChangeState(player.MoveInput.sqrMagnitude > 0.01f ? player.MoveState : player.IdleState);
            }
        }

        public void FixedTick() { }

        public void Exit()
        {
            player.SpriteRenderer.color = originalColor;
        }
    }
}