using Game.Core;
using UnityEngine;

namespace Game.UI
{
    public class VictoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private bool freezeTimeOnVictory = true;

        private void Awake()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
        }
        private void OnEnable() => EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        private void OnDisable() => EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            if (victoryPanel != null) victoryPanel.SetActive(true);
            if (freezeTimeOnVictory) Time.timeScale = 0f;
        }
    }
}