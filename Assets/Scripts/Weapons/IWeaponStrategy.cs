using Game.Core;

namespace Game.Weapons
{
    public interface IWeaponStrategy
    {
        void Fire(WeaponController controller, DamageInfo damageInfo);
    }
}