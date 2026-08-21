using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Debug
{
    /// <summary>
    /// 全项目统一的调试消息入口。
    /// </summary>
    public static class GameDebug
    {
        // Queue 只保存面板需要的有限历史；Console 自己负责完整日志历史。
        private static readonly Queue<DebugMessage> overlayHistory = new Queue<DebugMessage>();
        private static DebugSettings settings;

        /// <summary>新消息进入 Overlay 历史后触发，供当前激活的面板增量刷新。</summary>
        public static event Action<DebugMessage> MessagePublished;
        /// <summary>当前由 DebugBootstrap 注入的配置。</summary>
        public static DebugSettings Settings => settings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            settings = null;
            overlayHistory.Clear();
            MessagePublished = null;
        }

        /// <summary>注入本次运行使用的配置，并清除初始化前或上次运行留下的历史。</summary>
        public static void Initialize(DebugSettings newSettings)
        {
            settings = newSettings;
            overlayHistory.Clear();
            if (settings == null)
            {
                // 配置缺失时统一入口无法工作，因此这里是唯一有意绕过 GameDebug 的兜底错误。
                UnityEngine.Debug.LogError("[Debug][Error] DebugBootstrap 没有指定 DebugSettings。统一调试输出已停用。");
            }
        }

        /// <summary>判断指定分类是否同时通过总开关与分类掩码。</summary>
        public static bool IsEnabled(DebugCategory category)
        {
            return settings != null && settings.DebugEnabled 
                && category != DebugCategory.None
                && (settings.EnabledCategories & category) != 0;
        }

        /// <summary>判断指定分类是否允许执行带额外运行成本的诊断逻辑。</summary>
        public static bool AreExpensiveChecksEnabled(DebugCategory category)
        {
            return IsEnabled(category) && settings.ExpensiveChecksEnabled;
        }

        /// <summary>发布普通信息。</summary>
        public static void Log(DebugCategory category, string message, Object context = null)
        {
            Publish(category, DebugSeverity.Info, message, context);
        }
        /// <summary>发布警告信息。</summary>
        public static void Warning(DebugCategory category, string message, Object context = null)
        {
            Publish(category, DebugSeverity.Warning, message, context);
        }
        /// <summary>发布错误信息。</summary>
        public static void Error(DebugCategory category, string message, Object context = null)
        {
            Publish(category, DebugSeverity.Error, message, context);
        }

        /// <summary>
        /// 把当前 Overlay 历史复制到调用方提供的列表。
        /// 供面板重新启用时借此恢复最近消息。
        /// </summary>
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