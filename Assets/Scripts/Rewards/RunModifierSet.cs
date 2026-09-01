using Game.Core;
using Game.Entities;
using UnityEngine;

namespace Game.Rewards
{
    [RequireComponent(typeof(Health))]
    public sealed class RunModifierSet : MonoBehaviour
    {
        private Health health;
        private int bonusBurnDamage;
        private float bonusBurnDuration;
        private float bonusBurnInterval = 0.5f;
        private float bonusFreezePercent;
        private float bonusFreezeDuration;

        public float DamageMultiplier { get; private set; } = 1f;
        public float CooldownMultiplier { get; private set; } = 1f;
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public float KnockbackMultiplier { get; private set; } = 1f;
        public bool HasBonusBurn => bonusBurnDamage > 0 && bonusBurnDuration > 0f;
        public bool HasBonusFreeze => bonusFreezePercent > 0f && bonusFreezeDuration > 0f;
        public StatusEffectConfig BonusBurn => StatusEffectConfig.Burn(bonusBurnDuration, bonusBurnDamage, bonusBurnInterval);
        public StatusEffectConfig BonusFreeze => StatusEffectConfig.Freeze(bonusFreezeDuration, bonusFreezePercent);

        private void Awake()
        {
            health = GetComponent<Health>();
        }
        public void Apply(RunUpgradeData upgrade)
        {
            if (upgrade == null) return ;
            switch (upgrade.Type)
            {
                case RunUpgradeType.DamagePercent:
                    DamageMultiplier += Mathf.Max(0f, upgrade.Value);
                    break;
                case RunUpgradeType.CooldownReduction:
                    CooldownMultiplier = Mathf.Max(0.25f, CooldownMultiplier - Mathf.Max(0f, upgrade.Value));
                    break;
                case RunUpgradeType.MoveSpeedPercent:
                    MoveSpeedMultiplier += Mathf.Max(0f, upgrade.Value);
                    break;
                case RunUpgradeType.MaxHealth:
                    health.IncreaseMaxHealth(Mathf.Max(1, Mathf.RoundToInt(upgrade.Value)), true);
                    break;
                case RunUpgradeType.Burn:
                    bonusBurnDamage += Mathf.Max(1, Mathf.RoundToInt(upgrade.Value));
                    bonusBurnDuration = Mathf.Max(bonusBurnDuration, upgrade.Duration);
                    bonusBurnInterval = upgrade.Interval > 0f ? upgrade.Interval : 0.5f;
                    break;
                case RunUpgradeType.Freeze:
                    bonusFreezePercent = Mathf.Clamp(bonusFreezePercent + upgrade.Value, 0f, 0.8f);
                    bonusFreezeDuration = Mathf.Max(bonusFreezeDuration, upgrade.Duration);
                    break;
                case RunUpgradeType.KnockbackPercent:
                    KnockbackMultiplier += Mathf.Max(0f, upgrade.Value);
                    break;
            }
        }
    }
}