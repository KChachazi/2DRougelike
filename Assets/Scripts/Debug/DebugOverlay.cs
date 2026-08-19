using System.Collections.Generic;
using System.Text;
using Game.Commands;
using UnityEngine;
using TMPro;

namespace Game.Debug
{
    public sealed class DebugOverlay : MonoBehaviour
    {
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
            GameDebug.Log(DebugCategory.System, "Debug 面板开启。", this);
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
            builder.Clear();
            for (int i = 0; i < messages.Count; i ++)
            {
                if (i > 0) builder.AppendLine();
                builder.Append(messages[i].Format(settings.IncludeTimestamp));
            }
            logText.text = builder.ToString();
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