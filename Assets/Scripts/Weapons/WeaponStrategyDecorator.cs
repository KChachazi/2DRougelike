using Game.Core;

namespace Game.Weapons
{
    public abstract class WeaponStrategyDecorator : IWeaponStrategy
    {
        protected readonly IWeaponStrategy inner;
        protected WeaponStrategyDecorator(IWeaponStrategy inner) => this.inner = inner;
        public abstract void Fire(WeaponController controller, DamageInfo damageInfo);
    }
}