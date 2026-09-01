using System;
using System.Collections.Generic;
using Game.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 使用固定槽位显示一次局内强化选择。
    /// </summary>
    public sealed class RewardChoiceUI : MonoBehaviour
    {
        [Serializable]
        private sealed class ChoiceSlot
        {
            public Button button = null;
            public Image icon = null;
            public TMP_Text title = null;
            public TMP_Text description = null;
        }

        [SerializeField] private GameObject panel;
        [SerializeField] private ChoiceSlot[] slots;

        private readonly List<RunUpgradeData> currentChoices = new List<RunUpgradeData>();
        private Action<RunUpgradeData> onSelected;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }
        public void Show(IReadOnlyList<RunUpgradeData>choices, Action<RunUpgradeData> selectionCallback)
        {
            currentChoices.Clear();
            for (int i = 0; i < choices.Count; i ++)
                currentChoices.Add(choices[i]);
            onSelected = selectionCallback;

            for (int i = 0; i < slots.Length; i ++)
            {
                ChoiceSlot slot = slots[i];
                bool active = i < currentChoices.Count;
                if (slot.button == null) continue;
                slot.button.gameObject.SetActive(active);
                slot.button.onClick.RemoveAllListeners();
                if (!active) continue;

                int capturedIndex = i;
                RunUpgradeData upgrade = currentChoices[i];
                if (slot.icon != null)
                {
                    slot.icon.sprite = upgrade.Icon;
                    slot.icon.enabled = upgrade.Icon != null;
                }
                if (slot.title != null) slot.title.text = upgrade.DisplayName;
                if (slot.description != null) slot.description.text = upgrade.Description;
                slot.button.onClick.AddListener(() => Select(capturedIndex));
            }
            if (panel != null) panel.SetActive(true);
        }
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            onSelected = null;
            currentChoices.Clear();
        }
        private void Select(int index)
        {
            if (index < 0 || index >= currentChoices.Count) return ;
            Action<RunUpgradeData> callback = onSelected;
            onSelected = null;
            callback?.Invoke(currentChoices[index]);
        }
    }
}