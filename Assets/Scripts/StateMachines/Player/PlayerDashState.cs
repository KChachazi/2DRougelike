using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Player
{
    /// <summary>玩家冲刺闪避状态，根据当前移动方向执行冲刺，同时短暂免疫伤害并启动冷却。</summary>
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