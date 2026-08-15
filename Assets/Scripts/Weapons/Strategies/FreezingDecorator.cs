using Game.Core;

namespace Game.Weapons
{
    /// <summary>
    /// 冰冻装饰器：在原有武器基础上附加减速效果。
    /// </summary>
    // 机制：
    //   命中后目标进入冰冻状态，duration 秒内移速降低 slowPercent。
    //   多个 Freeze 效果同时存在时取最慢者。
    public class FreezingDecorator : WeaponStrategyDecorator
    {
        private readonly StatusEffectConfig freezeConfig;
        
        /// <summary>
        /// 使用数值参数创建冰冻装饰器。
        /// </summary>
        /// <param name="inner">被装饰的武器策略</param>
        /// <param name="duration">减速持续时间（秒）</param>
        /// <param name="slowPercent">减速比例：0.3 = 减速 30%</param>
        public FreezingDecorator(IWeaponStrategy inner, float duration, float slowPercent)
            : base(inner)
        {
            freezeConfig = StatusEffectConfig.Freeze(duration, slowPercent);
        }
        
        /// <summary>
        /// 使用已有状态配置创建冰冻装饰器。
        /// </summary>
        /// <param name="inner">被装饰的武器策略</param>
        /// <param name="config">冰冻配置</param>
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