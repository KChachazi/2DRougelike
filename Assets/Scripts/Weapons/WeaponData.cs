using UnityEngine;

namespace Game.Weapons
{
    public enum WeaponType { Ranged, Melee }

    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName = "Pistol";
        public WeaponType type = WeaponType.Ranged;
        public int damage = 10;
        public float cooldown = 0.25f;
        [Tooltip("弹夹容量，-1表示近战")]
        public int maxAmmo = 30;
        [Tooltip("近战判定半径，远程忽略")]
        public float range = 1f;
    }
}