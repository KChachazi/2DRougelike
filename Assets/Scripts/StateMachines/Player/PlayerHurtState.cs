using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Player
{
    /// <summary>玩家受伤无敌状态，同时伴随短暂硬直。</summary>
    public class PlayerHurtState : IState
    {
        private const float FlashInterval = 0.08f;

        private readonly PlayerController player;
        private readonly StateMachine stateMachine;
        private float timer;
        private Color originalColor;

        public PlayerHurtState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            timer = 0f;
            originalColor = player.SpriteRenderer.color;
            player.health.SetInvincible(player.HurtInvincibleDuration);
        }

        public void Tick()
        {
            timer += Time.deltaTime;
            bool flashOn = Mathf.FloorToInt(timer / FlashInterval) % 2 == 0;
            player.SpriteRenderer.color = flashOn ? Color.red : originalColor;
            if (timer >= player.HurtDuration)
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