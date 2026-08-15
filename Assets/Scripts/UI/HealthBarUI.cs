using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>玩家血条 UI，订阅玩家血量变化相关事件。</summary>
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        private void OnEnable() => EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        private void OnDisable() => EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);

        private void OnHealthChanged(PlayerHealthChangedEvent e)
        {
            if (fillImage == null || e.Max <= 0) return ;
            fillImage.fillAmount = (float)e.Current / e.Max;
        }
    }
}