using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
    public class EnemyChaseState : IState
    {
        private readonly EnemyController enemy;
        private readonly StateMachine stateMachine;

        public EnemyChaseState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            enemy.SpriteRenderer.color = new Color(1f, 0.6f, 0f);
        }

        public void Tick()
        {
            float distance = enemy.DistanceToPlayer();
            if (distance <= enemy.AttackRange)
            {
                stateMachine.ChangeState(enemy.AttackState);
                return ;
            }
            if (distance > enemy.LoseSightRange)
            {
                stateMachine.ChangeState(enemy.PatrolState);
            }
        }

        public void FixedTick()
        {
            if (enemy.Player != null)
            {
                enemy.MoveTowards(enemy.Player.position, enemy.ChaseSpeed);
            }
        }

        public void Exit() { }
    }
}