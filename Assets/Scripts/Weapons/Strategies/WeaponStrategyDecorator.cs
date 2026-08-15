using Game.Core;

namespace Game.Weapons
{
    /// <summary>
    /// 武器策略装饰器抽象基类。
    /// </summary>
    public abstract class WeaponStrategyDecorator : IWeaponStrategy
    {
        protected readonly IWeaponStrategy inner;
        protected WeaponStrategyDecorator(IWeaponStrategy inner) => this.inner = inner;
        /// <summary>
        /// 开火虚函数，子类应当修饰 damageInfo 后继续向 inner 转发。
        /// </summary>
        public abstract void Fire(WeaponController controller, DamageInfo damageInfo);
    }
}