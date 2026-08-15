using Game.Core;

namespace Game.Weapons
{
    /// <summary>
    /// 灼烧装饰器：在原有武器基础上附加灼烧 DoT 效果。
    /// </summary>
    //
    // 机制：
    //   命中后目标进入"灼烧"状态，在 duration 秒内每 tickInterval 秒
    //   受到一次 damagePerTick 点伤害。
    public class BurningDecorator : WeaponStrategyDecorator
    {
        private readonly StatusEffectConfig burnConfig;
        
        /// <summary>
        /// 使用数值参数创建灼烧装饰器。
        /// </summary>
        /// <param name="inner">被装饰的武器策略</param>
        /// <param name="duration">灼烧持续时间（秒）</param>
        /// <param name="damagePerTick">每跳伤害</param>
        /// <param name="tickInterval">跳字间隔（秒），默认 0.5</param>
        public BurningDecorator(IWeaponStrategy inner, float duration, int damagePerTick, float tickInterval = 0.5f)
            : base(inner)
        {
            burnConfig = StatusEffectConfig.Burn(duration, damagePerTick, tickInterval);
        }
        
        /// <summary>
        /// 使用已有灼烧配置创建灼烧装饰器。
        /// </summary>
        /// <param name="inner">被装饰的武器策略</param>
        /// <param name="config">灼烧配置</param>
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