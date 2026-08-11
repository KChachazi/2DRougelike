using UnityEngine;
using Game.Entities;

namespace Game.AI
{
    public class MeleeAttackAction : ActionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        private float cooldownTimer;

        public MeleeAttackAction(EnemyController enemy, EnemyBehaviour behaviour)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
        }
        public override NodeState Evaluate()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                return NodeState.Running;
            }
            if (enemy.Player != null && enemy.Player.TryGetComponent(out Health health))
            {
                health.TakeDamage(behaviour.contactDamage);
                cooldownTimer = behaviour.attackCooldown;
                return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }
}