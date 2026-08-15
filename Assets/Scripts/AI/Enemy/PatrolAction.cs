using UnityEngine;
using Game.Entities;

namespace Game.AI
{
    /// <summary>
    /// 动作：在出生点附近随机巡逻。
    /// </summary>
    //
    // 走到目标点 → 等待 1 秒 → 选新目标 → 继续走。
    // 对应原 FSM 的 PatrolState。
    public class PatrolAction : ActionNode
    {
        private const float WaitDuration = 1f;
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        private Vector2 wanderTarget;
        private float waitTimer;
        public PatrolAction(EnemyController enemy, EnemyBehaviour behaviour)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
            PickNewTarget();
        }

        public override NodeState Evaluate()
        {
            if (Vector2.Distance(enemy.Rb.position, wanderTarget) < 0.1f)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= WaitDuration)
                    PickNewTarget();
            }
            else
            {
                enemy.MoveTowards(wanderTarget, behaviour.patrolSpeed);
            }
            return NodeState.Running;
        }
        private void PickNewTarget()
        {
            Vector2 offset = Random.insideUnitCircle * behaviour.patrolRadius;
            wanderTarget = enemy.SpawnPosition + offset;
            waitTimer = 0f;
        }
    }
}