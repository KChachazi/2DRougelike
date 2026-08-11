using UnityEngine;
using Game.Entities;

namespace Game.AI
{
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