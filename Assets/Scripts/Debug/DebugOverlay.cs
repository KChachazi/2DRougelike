using System.Collections.Generic;
using System.Text;
using Game.Commands;
using UnityEngine;
using TMPro;

namespace Game.Debug
{
    /// <summary>
    /// 屏幕调试面板，负责表现，不参与日志过滤规则或输入执行。
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        [Header("Debug Overlay面板")]
        [SerializeField] private GameObject debugOverlayPanel;

        [Header("日志")]
        [SerializeField] private TMP_Text logText;

        [Header("输入缓冲")]
        [SerializeField] private TMP_Text inputBufferText;
        [SerializeField] private PlayerInputHandler playerInputHandler;

        private readonly List<DebugMessage> messages = new List<DebugMessage>();
        private readonly StringBuilder builder = new StringBuilder(512);
        private int lastBufferCount = int.MinValue;
        private string lastCommandName;
        private bool lastOverlayVisiable;

        private void OnEnable()
        {
            GameDebug.MessagePublished += OnMessagePublished;
            GameDebug.CopyOverlayHistory(messages);
            lastOverlayVisiable = IsOverlayVisible();
            RefreshLogText();
            RefreshInputBuffer(true);
            if (GameDebug.Settings.DebugEnabled && GameDebug.Settings.OverlayEnabled)
            {
                debugOverlayPanel.SetActive(true);
                GameDebug.Log(DebugCategory.System, "Debug 面板开启。", this);
            }
        }
        private void OnDisable()
        {
            GameDebug.MessagePublished -= OnMessagePublished;
        }
        private void Update()
        {
            bool OverlayVisiable = IsOverlayVisible();
            if (OverlayVisiable != lastOverlayVisiable)
            {
                lastOverlayVisiable = OverlayVisiable;
                RefreshLogText();
                RefreshInputBuffer(true);
            }
            RefreshInputBuffer(false);
        }
        private void OnMessagePublished(DebugMessage message)
        {
            messages.Add(message);
            DebugSettings settings = GameDebug.Settings;
            int limit = settings != null ? settings.OverlayMessageLimit : 1;
            int overflow = messages.Count - limit;
            if (overflow > 0)
                messages.RemoveRange(0, overflow);
            RefreshLogText();
        }
        private void RefreshLogText()
        {
            if (logText == null) return ;
            DebugSettings settings = GameDebug.Settings;
            if (settings == null || !settings.DebugEnabled || !settings.OverlayEnabled)
            {
                logText.text = string.Empty;
                return ;
            }
            int firstVisibleIndex = FindFirstVisibleMessageIndex(settings);
            BuildLogText(settings, firstVisibleIndex);
            logText.text = builder.ToString();
        }
        private int FindFirstVisibleMessageIndex(DebugSettings settings)
        {
            if (messages.Count <= 1) return 0;

            Rect rect = logText.rectTransform.rect;
            Vector4 margin = logText.margin;
            float availableWidth = Mathf.Max(0f, rect.width - margin.x - margin.z);
            float availableHeight = Mathf.Max(0f, rect.height - margin.y - margin.w);
            if (availableWidth <= 0f || availableHeight <= 0f)
                return messages.Count - 1;

            for (int firstIndex = 0; firstIndex < messages.Count; firstIndex++)
            {
                BuildLogText(settings, firstIndex);
                float preferredHeight = logText.GetPreferredValues(builder.ToString(), availableWidth, Mathf.Infinity).y;
                if (preferredHeight <= availableHeight + 0.01f)
                    return firstIndex;
            }
            return messages.Count - 1;
        }
        private void BuildLogText(DebugSettings settings, int firstIndex)
        {
            builder.Clear();
            for (int i = firstIndex; i < messages.Count; i ++)
            {
                if (i > firstIndex) builder.AppendLine();
                builder.Append(messages[i].Format(settings.IncludeTimestamp));
            }
        }
        private void RefreshInputBuffer(bool force)
        {
            if (inputBufferText == null) return ;
            DebugSettings settings = GameDebug.Settings;
            if (settings == null || !settings.DebugEnabled || !settings.OverlayEnabled || !GameDebug.IsEnabled(DebugCategory.Input)
                || playerInputHandler == null || playerInputHandler.Buffer == null)
            {
                if (force || inputBufferText.text.Length > 0)
                    inputBufferText.text = string.Empty;
                lastBufferCount = int.MinValue;
                lastCommandName = null;
                return ;
            }
            InputBuffer buffer = playerInputHandler.Buffer;
            int count = buffer.Count;
            string commandName = buffer.Empty() ? "-" : buffer.Peek().GetType().Name;
            // force 用于面板刚启用或可见性变化；常规帧只在数据变化时更新。
            if (!force && count == lastBufferCount && commandName == lastCommandName)
                return ;
            lastBufferCount = count;
            lastCommandName = commandName;
            inputBufferText.text = $"Buffer({count}) : {commandName}";
        }
        private static bool IsOverlayVisible()
        {
            DebugSettings settings = GameDebug.Settings;
            return settings != null && settings.DebugEnabled && settings.OverlayEnabled;
        }
    }
}