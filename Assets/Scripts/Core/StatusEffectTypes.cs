namespace Game.Core
{
    public enum StatusType
    {
        Burn,
        Freeze,
        Vulnerable,
    }

    [System.Serializable]
    public struct StatusEffectConfig
    {
        public StatusType Type;
        public float Duration;
        // For Burn
        public int DamagePerTick;
        public float TickInterval;
        // For Freeze
        public float SlowPercent;
        // For Vulnerable
        public float DamageMultiplier;

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
        public static StatusEffectConfig Freeze(float duration, float slowPercent)
        {
            return new StatusEffectConfig
            {
                Type = StatusType.Freeze,
                Duration = duration,
                SlowPercent = slowPercent,
            };
        }
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