using Game.Core;

namespace Game.Weapons
{
    public class FreezingDecorator : WeaponStrategyDecorator
    {
        private readonly StatusEffectConfig freezeConfig;
        public FreezingDecorator(IWeaponStrategy inner, float duration, float slowPercent)
            : base(inner)
        {
            freezeConfig = StatusEffectConfig.Freeze(duration, slowPercent);
        }
        public FreezingDecorator(IWeaponStrategy inner, StatusEffectConfig config)
            : base(inner)
        {
            freezeConfig = config;
        }
        public override void Fire(WeaponController controller, DamageInfo damageInfo)
        {
            DamageInfo enhanced = damageInfo.WithAddedStatus(freezeConfig);
            inner.Fire(controller, enhanced);
        }
    }
}