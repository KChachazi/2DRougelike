using System;

namespace Game.Core
{
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly float KnockbackForce;
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
        public DamageInfo WithAmount(int newAmount) => new DamageInfo(newAmount, KnockbackForce, StatusEffects);
        public DamageInfo WithKnockback(float force) => new DamageInfo(Amount, force, StatusEffects);
        public DamageInfo WithAddedStatus(StatusEffectConfig effect)
        {
            var newEffects = new StatusEffectConfig[StatusEffects.Length + 1];
            System.Array.Copy(StatusEffects, newEffects, StatusEffects.Length);
            newEffects[StatusEffects.Length] = effect;
            return new DamageInfo(Amount, KnockbackForce, newEffects);
        }
        public DamageInfo WithKnockbackAndStatus(float force, StatusEffectConfig effect)
        {
            var newEffects = new StatusEffectConfig[StatusEffects.Length + 1];
            System.Array.Copy(StatusEffects, newEffects, StatusEffects.Length);
            newEffects[StatusEffects.Length] = effect;
            return new DamageInfo(Amount, force, newEffects);
        }
        public override string ToString()
        {
            return $"DamageInfo(amount={Amount}, knockback={KnockbackForce}, effects={StatusEffects.Length})";
        }
    }
}