using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
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