using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 远程武器策略：从对象池取子弹，注入 DamageInfo，朝 FirePoint 方向发射。
    /// </summary>
    public class RangedWeaponStrategy : IWeaponStrategy
    {
        public void Fire(WeaponController controller, DamageInfo damageInfo)
        {
            GameObject bulletObj = controller.BulletPool.Get(
                controller.FirePoint.position, controller.FirePoint.rotation);
            if (bulletObj.TryGetComponent(out Bullet bullet))
                bullet.SetDamageInfo(damageInfo);
            else controller.BulletPool.Release(bulletObj);
        }
    }
}