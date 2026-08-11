using Game.Core;
using Game.Entities;
using Game.Weapons;
using UnityEngine;

namespace Game.AI
{
    public class BossSkillAction : ActionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        private readonly ObjectPool projectilePool;
        private readonly bool useProjectile;
        private float cooldownTimer;

        public BossSkillAction(EnemyController enemy, EnemyBehaviour behaviour,
            bool useProjectile, ObjectPool projectilePool)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
            this.useProjectile = useProjectile;
            this.projectilePool = projectilePool;
        }
        public override NodeState Evaluate()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                return NodeState.Running;
            }
            if (enemy.Player == null) return NodeState.Failure;
            DamageInfo info = new DamageInfo(behaviour.skillDamage);
            if (behaviour.skillBurnDuration > 0f && behaviour.skillBurnDamage > 0)
            {
                info = info.WithAddedStatus(StatusEffectConfig.Burn(
                    behaviour.skillBurnDuration, behaviour.skillBurnDamage, behaviour.skillBurnTickInterval));
            }

            if (useProjectile)
            {
                if (projectilePool == null) return NodeState.Failure;
                GameObject bulletObj = projectilePool.Get(enemy.transform.position, Quaternion.identity);
                if (!bulletObj.TryGetComponent(out EnemyProjectile bullet))
                {
                    projectilePool.Release(bulletObj);
                    return NodeState.Failure;
                }
                Vector2 dir = (Vector2)enemy.Player.position - (Vector2)enemy.transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                bullet.Launch(enemy.transform.position, Quaternion.Euler(0f, 0f, angle), behaviour.projectileSpeed, info);
            }
            else
            {
                if (enemy.Player.TryGetComponent(out Health health))
                {
                    health.TakeDamage(info);
                    if (enemy.Player.TryGetComponent(out StatusEffectManager statusManager))
                        statusManager.ApplyEffects(info);
                }
            }
            cooldownTimer = behaviour.skillCooldown;
            return NodeState.Success;
        }
    }
}