using UnityEngine;

namespace Game.Core
{
    public readonly struct PlayerHealthChangedEvent
    {
        public readonly int Current;
        public readonly int Max;
        public PlayerHealthChangedEvent(int current, int max) { Current = current; Max = max; }
    }

    public readonly struct AmmoChangedEvent
    {
        public readonly int Current;
        public readonly int Max; // Max 为 -1 时表示无限子弹，比如近战
        public AmmoChangedEvent(int current, int max) { Current = current; Max = max; }
    }

    public readonly struct WeaponChangedEvent
    {
        public readonly string WeaponName;
        public WeaponChangedEvent(string weaponName) { WeaponName = weaponName; }
    }
}