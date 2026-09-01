using UnityEngine;
namespace Game.Rewards
{
    public enum RunUpgradeType
    {
        DamagePercent,
        CooldownReduction,
        MoveSpeedPercent,
        MaxHealth,
        Burn,
        Freeze,
        KnockbackPercent,
    }

    [CreateAssetMenu(fileName = "NewRunUpgrade", menuName = "Game/Run Upgrade")]
    public sealed class RunUpgradeData : ScriptableObject
    {
        [Header("显示")]
        [SerializeField] private string displayName = "Upgrade";
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        [Header("规则")]
        [SerializeField] private RunUpgradeType type;
        [SerializeField, Min(1)] private int maxStacks = 3;
        [Tooltip("百分比/最大生命值/燃烧")]
        [SerializeField] private float value = 0.2f;
        [Tooltip("持续时间")]
        [SerializeField] private float duration = 3f;
        [Tooltip("燃烧的结算间隔")]
        [SerializeField] private float interval = 0.5f;

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public RunUpgradeType Type => type;
        public int MaxStacks => Mathf.Max(1, maxStacks);
        public float Value => value;
        public float Duration => duration;
        public float Interval => interval;
    }
}