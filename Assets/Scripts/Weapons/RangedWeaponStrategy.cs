using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    public class RangedWeaponStrategy : IWeaponStrategy
    {
        public void Fire(WeaponController controller, DamageInfo damageInfo)
        {
            GameObject bulletObj = controller.BulletPool.Get(
                controller.FirePoint.position, controller.FirePoint.rotation);
            if (bulletObj.TryGetComponent(out Bullet bullet))
                bullet.SetDamageInfo(damageInfo);
        }
    }
}