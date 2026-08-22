using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class RunResultUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text summaryText;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }
        private void OnEnable() => EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
        private void OnDisable() => EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        public void RestartSameSeed()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RestartSameSeed();
        }
        public void RestartNewSeed()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RestartNewSeed();
        }
        private void OnRunEnded(RunEndedEvent e)
        {
            if (panel != null) panel.SetActive(true);
            if (titleText != null) titleText.text = e.Result == RunResult.Victory ? "恭喜通关！" : "遗憾失败！";
            if (summaryText != null)
            {
                summaryText.text =
                    $"Seed：{e.Seed}\n" +
                    $"探索房间：{e.RoomsVisited}\n" +
                    $"击败敌人：{e.EnemiesDefeated}\n" +
                    $"获得强化：{e.UpgradesCollected}";
            }
        }
    }
}
