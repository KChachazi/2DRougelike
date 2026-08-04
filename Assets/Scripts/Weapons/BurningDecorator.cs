using Game.Core;

namespace Game.Weapons
{
    public class BurningDecorator : WeaponStrategyDecorator
    {
        private readonly StatusEffectConfig burnConfig;
        public BurningDecorator(IWeaponStrategy inner, float duration, int damagePerTick, float tickInterval = 0.5f)
            : base(inner)
        {
            burnConfig = StatusEffectConfig.Burn(duration, damagePerTick, tickInterval);
        }
        public BurningDecorator(IWeaponStrategy inner, StatusEffectConfig config)
            : base (inner)
        {
            burnConfig = config;
        }

        public override void Fire(WeaponController controller, DamageInfo damageInfo)
        {
            DamageInfo enhanced = damageInfo.WithAddedStatus(burnConfig);
            inner.Fire(controller, enhanced);
        }
    }
}