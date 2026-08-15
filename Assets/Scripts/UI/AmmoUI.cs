using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>弹药文本 UI，订阅武器与弹药相关事件。</summary>
    public class AmmoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private string weaponName = "";
        private int current;
        private int max;

        private void OnEnable()
        {
            EventBus.Subscribe<AmmoChangedEvent>(OnAmmoChanged);
            EventBus.Subscribe<WeaponChangedEvent>(OnWeaponChanged);
        }
        private void OnDisable()
        {
            EventBus.Unsubscribe<AmmoChangedEvent>(OnAmmoChanged);
            EventBus.Unsubscribe<WeaponChangedEvent>(OnWeaponChanged);
        }

        private void OnAmmoChanged(AmmoChangedEvent e)
        {
            if (current == e.Current && max == e.Max) return ;
            current = e.Current;
            max = e.Max;
            Refresh();
        }

        private void OnWeaponChanged(WeaponChangedEvent e)
        {
            weaponName = e.WeaponName;
            Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;
            string ammoText = max < 0 ? "∞" : $"{current} / {max}";
            label.text = $"{weaponName}  {ammoText}";
        }
    }
}