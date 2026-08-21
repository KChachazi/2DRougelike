using System;

namespace Game.Debug
{
    /// <summary>
    /// 调试消息的功能分类。
    /// 调用方仍只需为每条消息指定它所属的单一分类。
    /// </summary>
    [Flags]
    public enum DebugCategory
    {
        None = 0,
        System = 1 << 0,
        Input = 1 << 1,
        Combat = 1 << 2,
        AI = 1 << 3,
        Level = 1 << 4,
        Pool = 1 << 5,
        All = System | Input | Combat | AI | Level | Pool,
    }

    /// <summary>调试消息的严重程度，决定 Console 使用普通、警告或错误输出。</summary>
    public enum DebugSeverity
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// 一条与具体输出目标无关的调试消息。
    /// </summary>
    public readonly struct DebugMessage
    {
        /// <summary>创建消息时的非缩放运行时间，暂停游戏时仍会继续前进。</summary>
        public readonly float Timestamp;
        /// <summary>消息所属的功能模块。</summary>
        public readonly DebugCategory Category;
        /// <summary>消息的严重程度。</summary>
        public readonly DebugSeverity Severity;
        /// <summary>尚未添加统一前缀的原始正文。</summary>
        public readonly string Text;
        public DebugMessage(float timestamp, DebugCategory category, DebugSeverity severity, string text)
        {
            Timestamp = timestamp;
            Category = category;
            Severity = severity;
            Text = text;
        }

        /// <summary>按统一格式生成最终显示文本，可选择是否包含时间戳。</summary>
        public string Format(bool includeTimestamp)
        {
            string prefix = $"[{Category}][{Severity}]";
            return includeTimestamp ? $"[{Timestamp:F2}]{prefix} {Text}" : $"{prefix} {Text}";
        }
    }
}
