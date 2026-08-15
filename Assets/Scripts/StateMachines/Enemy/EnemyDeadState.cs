using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
    /// <summary>敌人死亡后的终止状态：停止移动并保留死亡锁定。</summary>
    public class EnemyDeadState : IState
    {
        private readonly EnemyController enemy;

        public EnemyDeadState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
        }

        public void Enter()
        {
            enemy.SpriteRenderer.color = Color.gray;
            enemy.Rb.linearVelocity = Vector2.zero;
        }

        public void Tick() { }
        public void FixedTick() { }
        public void Exit() { }
    }
}