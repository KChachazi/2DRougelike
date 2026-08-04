using Game.Core;

namespace Game.Weapons
{
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