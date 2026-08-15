using Game.Core;

namespace Game.Weapons
{
    /// <summary>
    /// 击退装饰器：在原有武器基础上附加击退效果。
    /// </summary>
    // 机制：
    //   命中时将目标沿 伤害源->目标 方向推开。
    //   击退不属于持续状态，
    //       通过 DamageInfo.KnockbackForce 传递，
    //       由命中逻辑（Bullet/Melee）执行。
    public class KnockbackDecorator : WeaponStrategyDecorator
    {
        private readonly float knockbackForce;
        public KnockbackDecorator(IWeaponStrategy inner, float force)
            : base(inner)
        {
            knockbackForce = force;
        }
        public override void Fire(WeaponController controller, DamageInfo damageInfo)
        {
            DamageInfo enhanced = damageInfo.WithKnockback(knockbackForce);
            inner.Fire(controller, enhanced);
        }
    }
}