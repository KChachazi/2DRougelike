using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
    public class EnemyKnockbackState : IState
    {
        private readonly EnemyController enemy;
        private readonly StateMachine stateMachine;
        private float timer;
        private Vector2 direction;
        private float startSpeed;
        public EnemyKnockbackState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }
        public void Enter()
        {
            direction = enemy.KnockbackDirection;
            startSpeed = enemy.KnockbackSpeed;
            timer = 0f;
            enemy.SpriteRenderer.color = Color.cyan;
        }
        public void Tick() { }
        public void FixedTick()
        {
            timer += Time.fixedDeltaTime;
            if (timer >= enemy.KnockbackDuration)
            {
                stateMachine.ChangeState(enemy.PatrolState);
                return ;
            }
            float t = timer / enemy.KnockbackDuration;
            float currentSpeed = Mathf.Lerp(startSpeed, 0f, t);
            enemy.Rb.MovePosition(
                enemy.Rb.position + direction * currentSpeed * Time.fixedDeltaTime);
        }
        public void Exit()
        {
            enemy.SpriteRenderer.color = enemy.OriginalColor;
        }
    }
}