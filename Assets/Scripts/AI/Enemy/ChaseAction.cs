using UnityEngine;
using Game.Entities;

namespace Game.AI
{
    /// <summary>
    /// 动作：追击玩家。
    /// </summary>
    //
    // 追击是一个直到上层条件变化都持续进行的过程，所以返回 Running。
    // 对应原 FSM 的 ChaseState。
    public class ChaseAction : ActionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        public ChaseAction(EnemyController enemy, EnemyBehaviour behaviour)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
        }
        public override NodeState Evaluate()
        {
            if (enemy.Player == null) return NodeState.Failure;
            enemy.MoveTowards(enemy.Player.position, behaviour.chaseSpeed);
            return NodeState.Running;
        }
    }
}