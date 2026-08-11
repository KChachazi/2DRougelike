using System.Threading.Tasks;
using Game.Entities;
using UnityEngine;

namespace Game.AI
{
    public class KeepDistanceAction : ActionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        public KeepDistanceAction(EnemyController enemy, EnemyBehaviour behaviour)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
        }
        public override NodeState Evaluate()
        {
            if (enemy.Player == null) return NodeState.Failure;
            Vector2 away = (Vector2)enemy.transform.position - (Vector2)enemy.Player.position;
            if (away.sqrMagnitude < 0.0001f) return NodeState.Running;
            // 目标距离为 minShootRange + 0.5f(缓冲)
            float safeDistance = behaviour.minShootRange + 0.5f;
            Vector2 target = (Vector2)enemy.Player.position + away.normalized * safeDistance;
            enemy.MoveTowards(target, behaviour.chaseSpeed);
            return NodeState.Running;
        }
    }
}