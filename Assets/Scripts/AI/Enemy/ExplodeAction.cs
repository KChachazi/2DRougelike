using Game.Core;
using Game.Entities;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 动作：自爆，同时对爆炸范围内玩家造成伤害。
    /// </summary>
    public class ExplodeAction : ActionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        private const int MaxHits = 16;
        private readonly Collider2D[] hitBuffer = new Collider2D[MaxHits];
        private readonly ContactFilter2D filter = ContactFilter2D.noFilter;
        public ExplodeAction(EnemyController enemy, EnemyBehaviour behaviour)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
        }
        public override NodeState Evaluate()
        {
            if (enemy.Player == null) return NodeState.Failure;
            int count = Physics2D.OverlapCircle(enemy.transform.position,
                behaviour.explodeRange, filter, hitBuffer);
            for (int i = 0; i < count; i ++)
            {
                if (!hitBuffer[i].CompareTag("Player")) continue;
                if (hitBuffer[i].TryGetComponent(out Health health))
                    health.TakeDamage(new DamageInfo(behaviour.explodeDamage));
            }
            enemy.health.TakeDamage(int.MaxValue);
            return NodeState.Success;
        }
    }
}