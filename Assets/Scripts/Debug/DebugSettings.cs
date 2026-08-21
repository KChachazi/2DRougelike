using UnityEngine;

namespace Game.Debug
{
    /// <summary>
    /// 统一调试系统的只读运行配置。
    /// </summary>
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

        /// <summary>统一调试输出的总开关。</summary>
        public bool DebugEnabled => debugEnabled;
        /// <summary>是否允许对象池重复归还等带额外运行成本的诊断。</summary>
        public bool ExpensiveChecksEnabled => expensiveChecksEnabled;
        /// <summary>是否转发到 Unity Console。</summary>
        public bool ConsoleEnabled => consoleEnabled;
        /// <summary>是否保存并转发到屏幕调试面板。</summary>
        public bool OverlayEnabled => overlayEnabled;
        /// <summary>当前允许通过过滤器的功能分类组合。</summary>
        public DebugCategory EnabledCategories => enableCategories;
        /// <summary>面板历史的有效容量。</summary>
        public int OverlayMessageLimit => Mathf.Max(1, overlayMessageLimit);
        /// <summary>格式化消息时是否显示非缩放时间戳。</summary>
        public bool IncludeTimestamp => includeTimestamp;
    }
}