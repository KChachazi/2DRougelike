using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Debug
{
    public static class GameDebug
    {
        private static readonly Queue<DebugMessage> overlayHistory = new Queue<DebugMessage>();
        private static DebugSettings settings;

        public static event Action<DebugMessage> MessagePublished;
        public static DebugSettings Settings => settings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            settings = null;
            overlayHistory.Clear();
            MessagePublished = null;
        }

        public static void Initialize(DebugSettings newSettings)
        {
            settings = newSettings;
            overlayHistory.Clear();
            if (settings == null)
            {
                UnityEngine.Debug.LogError("[Debug][Error] DebugBootstrap 没有指定 DebugSettings。统一调试输出已停用。");
            }
        }
        public static bool IsEnabled(DebugCategory category)
        {
            return settings != null && settings.DebugEnabled 
                && category != DebugCategory.None
                && (settings.EnabledCategories & category) != 0;
        }
        public static bool AreExpensiveChecksEnabled(DebugCategory category)
        {
            return IsEnabled(category) && settings.ExpensiveChecksEnabled;
        }
        public static void Log(DebugCategory category, string message, Object context = null)
        {
            Publish(category, DebugSeverity.Info, message, context);
        }
        public static void Warning(DebugCategory category, string message, Object context = null)
        {
            Publish(category, DebugSeverity.Warning, message, context);
        }
        public static void Error(DebugCategory category, string message, Object context = null)
        {
            Publish(category, DebugSeverity.Error, message, context);
        }
        public static void CopyOverlayHistory(List<DebugMessage> destination)
        {
            if (destination == null) return ;
            destination.Clear();
            destination.AddRange(overlayHistory);
        }
        // ======================== 私有工具 ========================
        private static void Publish(DebugCategory category, DebugSeverity severity, string message, Object context)
        {
            if (!IsEnabled(category)) return ;
            DebugMessage debugMessage = new DebugMessage(Time.unscaledTime, category, severity, message);
            if (settings.ConsoleEnabled)
            {
                string formatted = debugMessage.Format(settings.IncludeTimestamp);
                switch (severity)
                {
                    case DebugSeverity.Info:
                        UnityEngine.Debug.Log(formatted, context);
                        break;
                    
                    case DebugSeverity.Warning:
                        UnityEngine.Debug.LogWarning(formatted, context);
                        break;
                    case DebugSeverity.Error:
                        UnityEngine.Debug.LogError(formatted, context);
                        break;
                }
            }

            if (!settings.OverlayEnabled) return ;
            overlayHistory.Enqueue(debugMessage);
            while (overlayHistory.Count > settings.OverlayMessageLimit)
                overlayHistory.Dequeue();
            MessagePublished?.Invoke(debugMessage);
        }
    }
}