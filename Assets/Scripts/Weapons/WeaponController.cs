using System.Collections.Generic;
using Game.Core;
using Game.Entities;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// 玩家武器系统的运行时协调器：维护当前武器、弹药与冷却，
    /// 从 WeaponData 构建 DamageInfo，并将开火委派给对应的无状态策略。
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private ObjectPool bulletPool;
        [SerializeField] private Transform firePoint;
        [Tooltip("按顺序对应数字键 1/2/3, 元素 0 表示初始武器")]
        [SerializeField] private WeaponData[] weapons;

        public ObjectPool BulletPool => bulletPool;
        public Transform FirePoint => firePoint;
        public int WeaponCount => weapons.Length;
        public float CurrentWeaponRange => weapons[currentIdx].range;

        private PlayerController playerController;
        
        private IWeaponStrategy[] weaponStrategies;
        private int currentIdx;
        private int[] currentAmmo;
        private float cooldownTimer;

        private WeaponData CurrentWeapon => weapons[currentIdx];

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            weaponStrategies = new IWeaponStrategy[weapons.Length];
            for (int i = 0; i < weapons.Length; i ++)
                weaponStrategies[i] = weapons[i].type == WeaponType.Ranged
                    ? new RangedWeaponStrategy()
                    : new MeleeWeaponStrategy();
            currentAmmo = new int[weapons.Length];
            for (int i = 0; i < weapons.Length; i ++)
                currentAmmo[i] = weapons[i].maxAmmo;
        }
        private void Start()
        {
            BroadcastWeapon();
            BroadcastAmmo();
        }
        private void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        }

        // ======================== 公开接口 ========================
        /// <summary>
        /// 判断当前是否满足开火条件。
        /// </summary>
        public bool CanFire()
        {
            bool canAct = playerController == null || playerController.CanAct;
            return canAct && cooldownTimer <= 0f && HasAmmo();
        }
        /// <summary>
        /// 玩家开火。
        /// 调用方应先通过 CanFire() 检查执行条件。
        /// </summary>
        public void Fire()
        {
            WeaponData data = CurrentWeapon;
            DamageInfo damageInfo = BuildDamageInfo(data);
            weaponStrategies[currentIdx].Fire(this, damageInfo);
            cooldownTimer = data.cooldown;
            if (data.maxAmmo >= 0)
            {
                currentAmmo[currentIdx] --;
                BroadcastAmmo();
            }
            if (data.type == WeaponType.Melee && playerController != null)
                playerController.TriggerAttack();
        }
        /// <summary>
        /// 切换到有效武器槽位。
        /// </summary>
        public void SwitchTo(int index)
        {
            if (index < 0 || index >= weapons.Length || index == currentIdx) return ;
            currentIdx = index;
            cooldownTimer = 0f;
            BroadcastWeapon();
            BroadcastAmmo();
        }
        /// <summary>
        /// 补充弹药，不会超过最大弹药量。
        /// </summary>
        public bool AddAmmo(int amount)
        {
            WeaponData data = CurrentWeapon;
            if (amount <= 0 || data.maxAmmo < 0 || currentAmmo[currentIdx] >= data.maxAmmo)
                return false;
            currentAmmo[currentIdx] = Mathf.Min(currentAmmo[currentIdx] + amount, data.maxAmmo);
            BroadcastAmmo();
            return true;
        }
        // ======================== 私有工具 ========================
        private DamageInfo BuildDamageInfo(WeaponData data)
        {
            DamageInfo info = new DamageInfo(data.damage, data.knockbackForce);
            if (data.burnDamagePerTick > 0 && data.burnDuration > 0f)
            {
                info = info.WithAddedStatus(StatusEffectConfig.Burn(
                    data.burnDuration, data.burnDamagePerTick, data.burnTickInterval));
            }
            if (data.freezePercent > 0f && data.freezeDuration > 0f)
            {
                info = info.WithAddedStatus(StatusEffectConfig.Freeze(
                    data.freezeDuration, data.freezePercent));
            }
            return info;
        }
        private bool HasAmmo() => CurrentWeapon.maxAmmo < 0 || currentAmmo[currentIdx] > 0;
        private void BroadcastAmmo() =>
            EventBus.Publish(new AmmoChangedEvent(currentAmmo[currentIdx], CurrentWeapon.maxAmmo));
        private void BroadcastWeapon() =>
            EventBus.Publish(new WeaponChangedEvent(CurrentWeapon.weaponName));
    }
}