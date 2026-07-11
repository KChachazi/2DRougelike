using System.Xml.Serialization;
using Game.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
// 防止 CS0104，明确为 Game.Core 中的 EventBus
using EventBus = Game.Core.EventBus;

namespace Game.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        private void OnEnable() => EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        private void OnDisable() => EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);

        private void OnHealthChanged(PlayerHealthChangedEvent e)
        {
            if (fillImage == null || e.Max <= 0) return ;
            fillImage.fillAmount = (float)e.Current / e.Max;
        }
    }
}