using System;

namespace Game.Core
{
    /// <summary>
    /// 一次伤害的完整信息包，包含基础伤害、击退力度和状态效果。
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>基础伤害值</summary>
        public readonly int Amount;
        /// <summary>击退力度（0 表示无击退）</summary>
        public readonly float KnockbackForce;
        /// <summary>命中时附加的异常状态列表</summary>
        public readonly StatusEffectConfig[] StatusEffects;
        public DamageInfo(int amount)
        {
            Amount = amount;
            KnockbackForce = 0f;
            StatusEffects = Array.Empty<StatusEffectConfig>();
        }
        public DamageInfo(int amount, float knockbackForce)
        {
            Amount = amount;
            KnockbackForce = knockbackForce;
            StatusEffects = Array.Empty<StatusEffectConfig>();
        }
        public DamageInfo(int amount, float knockbackForce, StatusEffectConfig[] statusEffects)
        {
            Amount = amount;
            KnockbackForce = knockbackForce;
            StatusEffects = statusEffects;
        }
        /// <summary>返回一份 Amount 不同的副本，其余字段不变。</summary>
        public DamageInfo WithAmount(int newAmount) => new DamageInfo(newAmount, KnockbackForce, StatusEffects);
        /// <summary>返回一份 KnockbackForce 不同的副本，其余字段不变。</summary>
        public DamageInfo WithKnockback(float force) => new DamageInfo(Amount, force, StatusEffects);
        /// <summary>返回一份追加了异常状态的副本，其余字段不变。</summary>
        public DamageInfo WithAddedStatus(StatusEffectConfig effect)
        {
            var newEffects = new StatusEffectConfig[StatusEffects.Length + 1];
            System.Array.Copy(StatusEffects, newEffects, StatusEffects.Length);
            newEffects[StatusEffects.Length] = effect;
            return new DamageInfo(Amount, KnockbackForce, newEffects);
        }
        /// <summary>返回一份 KnockbackForce 不同且追加了异常状态的副本，其余字段不变。</summary>
        public DamageInfo WithKnockbackAndStatus(float force, StatusEffectConfig effect)
        {
            var newEffects = new StatusEffectConfig[StatusEffects.Length + 1];
            System.Array.Copy(StatusEffects, newEffects, StatusEffects.Length);
            newEffects[StatusEffects.Length] = effect;
            return new DamageInfo(Amount, force, newEffects);
        }
        /// <summary>
        /// 调试使用，将 DamageInfo 转化为 string。
        /// </summary>
        public override string ToString()
        {
            return $"DamageInfo(amount={Amount}, knockback={KnockbackForce}, effects={StatusEffects.Length})";
        }
    }
}