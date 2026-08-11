using Game.Core;
using Game.Entities;
using Game.Weapons;
using UnityEngine;

namespace Game.AI
{
    public class ShootAction : ActionNode
    {
        private readonly EnemyController enemy;
        private readonly EnemyBehaviour behaviour;
        private readonly ObjectPool projectilePool;
        private float cooldownTimer;
        public ShootAction(EnemyController enemy, EnemyBehaviour behaviour, ObjectPool pool)
        {
            this.enemy = enemy;
            this.behaviour = behaviour;
            this.projectilePool = pool;
        }
        public override NodeState Evaluate()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                return NodeState.Running;
            }
            if (enemy.Player == null || projectilePool == null) return NodeState.Failure;

            GameObject bulletObj = projectilePool.Get(enemy.transform.position, Quaternion.identity);
            if (bulletObj.TryGetComponent(out EnemyProjectile bullet))
            {
                Vector2 dir = (Vector2)enemy.Player.position - (Vector2)enemy.transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                DamageInfo info = new DamageInfo(behaviour.projectileDamage);
                bullet.Launch(enemy.transform.position, Quaternion.Euler(0f, 0f, angle),
                    behaviour.projectileSpeed, info);
                cooldownTimer = behaviour.shootCooldown;
                return NodeState.Success;
            }
            projectilePool.Release(bulletObj);
            return NodeState.Failure;
        }
    }
}