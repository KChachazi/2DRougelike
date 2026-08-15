using UnityEngine;
using Game.Entities;
using Game.Core;

namespace Game.AI
{
    /// <summary>
    /// 动作：近战攻击玩家。
    /// </summary>
    // 三段式行为：
    //   冷却中 → Running
    //   冷却结束 → 扣血 + 进入冷却 → Success
    //   玩家不存在 → Failure
    // 其中，冷却由节点自己管理。
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
                health.TakeDamage(new DamageInfo(behaviour.contactDamage));
                cooldownTimer = behaviour.attackCooldown;
                return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }
}