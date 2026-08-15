namespace Game.Core
{
    /// <summary>
    /// 持续状态异常类型。
    /// </summary>
    public enum StatusType
    {
        Burn,       // 灼烧：持续扣血
        Freeze,     // 冰冻：减速
        Vulnerable, // 易伤：受到伤害增加
    }
    /// <summary>
    /// 单条状态异常的配置数据。
    /// 不同 StatusType 用到的字段不同，未用到的字段可直接忽略：<br/>
    ///   Burn       → DamagePerTick / TickInterval / Duration  <br/>
    ///   Freeze     → SlowPercent / Duration                   <br/>
    ///   Vulnerable → DamageMultiplier / Duration              <br/>
    /// </summary>
    [System.Serializable]
    public struct StatusEffectConfig
    {
        public StatusType Type;
        /// <summary>效果持续时间（秒）</summary>
        public float Duration;

        // ---- Burn 专用 ----
        /// <summary>每次跳字的伤害量</summary>
        public int DamagePerTick;
        /// <summary>跳字间隔（秒）</summary>
        public float TickInterval;

        // ---- Freeze 专用 ----
        /// <summary>减速比例：0.3 = 减速 30%</summary>
        public float SlowPercent;

        // ---- Vulnerable 专用 ----
        /// <summary>受伤倍率：1.5 = 受到 150% 伤害</summary>
        public float DamageMultiplier;

        /// <summary>
        /// 快捷构造：灼烧效果。
        /// </summary>
        public static StatusEffectConfig Burn(float duration, int damagePerTick, float tickInterval = 0.5f)
        {
            return new StatusEffectConfig
            {
                Type = StatusType.Burn,
                Duration = duration,
                DamagePerTick = damagePerTick,
                TickInterval = tickInterval,
            };
        }
        /// <summary>
        /// 快捷构造：冰冻减速效果。
        /// </summary>
        public static StatusEffectConfig Freeze(float duration, float slowPercent)
        {
            return new StatusEffectConfig
            {
                Type = StatusType.Freeze,
                Duration = duration,
                SlowPercent = slowPercent,
            };
        }
        /// <summary>
        /// 快捷构造：易伤效果。
        /// </summary>
        public static StatusEffectConfig Vulnerable(float duration, float damageMultiplier)
        {
            return new StatusEffectConfig
            {
                Type = StatusType.Vulnerable,
                Duration = duration,
                DamageMultiplier = damageMultiplier,
            };
        }
    }
}