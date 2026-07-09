using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
    public class EnemyAttackState : IState
    {
        private readonly EnemyController enemy;
        private readonly StateMachine stateMachine;
        private float cooldownTimer;

        public EnemyAttackState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            enemy.SpriteRenderer.color = Color.red;
            cooldownTimer = 0f;
        }

        public void Tick()
        {
            float distance = enemy.DistanceToPlayer();
            if (distance > enemy.AttackRange * 1.2f)
            {
                stateMachine.ChangeState(enemy.ChaseState);
                return ;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                TryHitPlayer();
                cooldownTimer = enemy.AttackCooldown;
            }
        }

        public void FixedTick() { }
        public void Exit() { }
        private void TryHitPlayer()
        {
            if (enemy.Player != null && enemy.Player.TryGetComponent(out Health health))
            {
                health.TakeDamage(enemy.ContactDamage);
            }
        }
    }
}