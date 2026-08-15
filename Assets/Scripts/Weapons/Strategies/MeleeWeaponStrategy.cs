using Game.Core;
using Game.Entities;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 近战武器策略：对角色前方圆形范围内所有敌人施加 DamageInfo，以及击退。
    /// </summary>
    public class MeleeWeaponStrategy : IWeaponStrategy
    {
        private const int MaxHits = 16;
        private readonly Collider2D[] hitBuffer = new Collider2D[MaxHits];
        private readonly ContactFilter2D filter = ContactFilter2D.noFilter;
        public void Fire(WeaponController controller, DamageInfo damageInfo)
        {
            float range = controller.CurrentWeaponRange;
            Vector2 origin = (Vector2)controller.transform.position
                           + (Vector2)controller.transform.right * range;
            int count = Physics2D.OverlapCircle(origin, range, filter, hitBuffer);
            for (int i = 0; i < count; i ++)
            {
                Collider2D hit = hitBuffer[i];
                if (!hit.CompareTag("Enemy")) continue;
                // 1. 造成伤害
                if (hit.TryGetComponent(out Health health))
                    health.TakeDamage(damageInfo);
                // 2. 状态异常
                if (hit.TryGetComponent(out StatusEffectManager statusEffectManager))
                {
                    statusEffectManager.ApplyEffects(damageInfo);
                }
                // 3. 击退
                if (hit.TryGetComponent(out EnemyController enemy))
                {
                    if (damageInfo.KnockbackForce > 0f)
                    {
                        Vector2 knockDirection = ((Vector2)hit.transform.position - origin).normalized;
                        enemy.TriggerKnockback(knockDirection, damageInfo.KnockbackForce);
                    }
                }
            }
        }
    }
}