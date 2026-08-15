using UnityEngine;

namespace Game.Weapons
{
    /// <summary>武器采用的基础开火策略类型。</summary>
    public enum WeaponType { Ranged, Melee }

    /// <summary>武器的伤害、冷却、弹药、范围和状态效果配置。</summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("=== 基础属性 ===")]
        public string weaponName = "Pistol";
        public WeaponType type = WeaponType.Ranged;
        public int damage = 10;
        public float cooldown = 0.25f;
        [Tooltip("弹夹容量；-1表示无限弹药")]
        public int maxAmmo = 30;
        [Tooltip("近战判定半径，远程忽略")]
        public float range = 1f;

        // ======================== V1.5 新增 ========================
        [Header("=== 特殊效果 ===")]

        [Header("灼烧 DoT")]
        [Tooltip("DoT单次伤害；0 表示当前武器无灼烧效果")]
        public int burnDamagePerTick = 0;
        [Tooltip("灼烧持续时间-秒")]
        public float burnDuration = 0f;
        [Tooltip("DoT 间隔")]
        public float burnTickInterval = 0.5f;

        [Header("冰冻减速")]
        [Tooltip("减速比例；0.3 表示减速 30%，即移速 x0.7")]
        public float freezePercent = 0f;
        [Tooltip("冰冻持续时间-秒")]
        public float freezeDuration = 0f;

        [Header("击退")]
        [Tooltip("击退力度；1.0 为标准，0 表示无击退")]
        public float knockbackForce = 0f;
    }
}