using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
    public class EnemyPatrolState : IState
    {
        private const float WaitDuration = 1f;

        private readonly EnemyController enemy;
        private readonly StateMachine stateMachine;
        private Vector2 wanderTarget;
        private float waitTimer;

        public EnemyPatrolState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            PickNewTarget();
        }

        public void Tick()
        {
            if (enemy.DistanceToPlayer() <= enemy.DetectionRange)
            {
                stateMachine.ChangeState(enemy.ChaseState);
                return ;
            }
            if (Vector2.Distance(enemy.Rb.position, wanderTarget) < 0.1f)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= WaitDuration)
                {
                    PickNewTarget();
                }
            }
        }

        public void FixedTick()
        {
            enemy.MoveTowards(wanderTarget, enemy.PatrolSpeed);
        }

        public void Exit() { }
        private void PickNewTarget()
        {
            Vector2 offset = Random.insideUnitCircle * enemy.PatrolRadius;
            wanderTarget = enemy.SpawnPosition + offset;
            waitTimer = 0f;
        }
    }
}