using System.Collections.Generic;
using Game.Core;
using Game.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Weapons
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private ObjectPool bulletPool;
        [SerializeField] private Transform firePoint;
        [Tooltip("按顺序对应数字键 1/2/3, 元素 0 表示初始武器")]
        [SerializeField] private WeaponData[] weapons;

        public ObjectPool BulletPool => bulletPool;
        public Transform FirePoint => firePoint;

        private PlayerController playerController;
        private readonly Dictionary<WeaponType, IWeaponStrategy> strategies = 
            new Dictionary<WeaponType, IWeaponStrategy>();
        
        private int currentIdx;
        private int[] currentAmmo;
        private float cooldownTimer;

        private WeaponData CurrentWeapon => weapons[currentIdx];

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            strategies[WeaponType.Ranged] = new RangedWeaponStrategy();
            strategies[WeaponType.Melee]  = new MeleeWeaponStrategy();

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
            HandleSwitchInput();
            bool canAct = playerController == null || playerController.CanAct;
            bool firePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (firePressed && canAct && cooldownTimer <= 0f && HasAmmo())
                FireCurrentWeapon();
        }

        private void FireCurrentWeapon()
        {
            WeaponData data = CurrentWeapon;
            strategies[data.type].Fire(this, data);
            cooldownTimer = data.cooldown;

            if (data.maxAmmo >= 0)
            {
                currentAmmo[currentIdx] --;
                BroadcastAmmo();
            }

            if (data.type == WeaponType.Melee && playerController != null)
                playerController.TriggerAttack();
        }

        private void HandleSwitchInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) SwitchTo(0);
            else if (kb.digit2Key.wasPressedThisFrame) SwitchTo(1);
            else if (kb.digit3Key.wasPressedThisFrame) SwitchTo(2);
        }

        private void SwitchTo(int index)
        {
            if (index < 0 || index >= weapons.Length || index == currentIdx) return ;
            currentIdx = index;
            cooldownTimer = 0f;
            BroadcastWeapon();
            BroadcastAmmo();
        }

        public void AddAmmo(int amount)
        {
            WeaponData data = CurrentWeapon;
            if (data.maxAmmo < 0) return;
            currentAmmo[currentIdx] = Mathf.Min(currentAmmo[currentIdx] + amount, data.maxAmmo);
            BroadcastAmmo();
        }

        private bool HasAmmo() => CurrentWeapon.maxAmmo < 0 || currentAmmo[currentIdx] > 0;

        private void BroadcastAmmo() =>
            EventBus.Publish(new AmmoChangedEvent(currentAmmo[currentIdx], CurrentWeapon.maxAmmo));

        private void BroadcastWeapon() =>
            EventBus.Publish(new WeaponChangedEvent(CurrentWeapon.weaponName));
    }
}