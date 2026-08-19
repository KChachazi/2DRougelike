using System;

namespace Game.Debug
{
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
    public enum DebugSeverity
    {
        Info,
        Warning,
        Error,
    }

    public readonly struct DebugMessage
    {
        public readonly float Timestamp;
        public readonly DebugCategory Category;
        public readonly DebugSeverity Severity;
        public readonly string Text;
        public DebugMessage(float timestamp, DebugCategory category, DebugSeverity severity, string text)
        {
            Timestamp = timestamp;
            Category = category;
            Severity = severity;
            Text = text;
        }
        public string Format(bool includeTimestamp)
        {
            string prefix = $"[{Category}][{Severity}]";
            return includeTimestamp ? $"[{Timestamp:F2}]{prefix} {Text}" : $"{prefix} {Text}";
        }
    }
}