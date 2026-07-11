using UnityEngine;

namespace Game.Weapons
{
    public class RangedWeaponStrategy : IWeaponStrategy
    {
        public void Fire(WeaponController controller, WeaponData data)
        {
            GameObject bulletObj = controller.BulletPool.Get(controller.FirePoint.position, controller.FirePoint.rotation);
            if (bulletObj.TryGetComponent(out Bullet bullet))
                bullet.SetDamage(data.damage);
        }
    }
}