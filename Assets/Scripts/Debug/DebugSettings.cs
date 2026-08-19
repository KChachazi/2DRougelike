using UnityEngine;

namespace Game.Debug
{
    [CreateAssetMenu(fileName = "DebugSettings", menuName = "Game/Debug Settings")]
    public sealed class DebugSettings : ScriptableObject
    {
        [Header("总开关")]
        [Tooltip("关闭后，GameDebug 不再向 Console 或屏幕面板输出消息。")]
        [SerializeField] private bool debugEnabled = true;

        [Tooltip("启用需要额外集合或检查的诊断逻辑，如对象池重复归还。")]
        [SerializeField] private bool expensiveChecksEnabled = true;

        [Header("输出目标")]
        [SerializeField] private bool consoleEnabled = true;
        [SerializeField] private bool overlayEnabled = true;

        [Header("过滤与格式")]
        [Tooltip("只输出勾选分类的消息。")]
        [SerializeField] private DebugCategory enableCategories = DebugCategory.All;

        [Header("屏幕面板保留的最近消息数。")]
        [Min(1)]
        [SerializeField] private int overlayMessageLimit = 12;

        [SerializeField] private bool includeTimestamp = true;

        public bool DebugEnabled => debugEnabled;
        public bool ExpensiveChecksEnabled => expensiveChecksEnabled;
        public bool ConsoleEnabled => consoleEnabled;
        public bool OverlayEnabled => overlayEnabled;
        public DebugCategory EnabledCategories => enableCategories;
        public int OverlayMessageLimit => Mathf.Max(1, overlayMessageLimit);
        public bool IncludeTimestamp => includeTimestamp;
    }
}