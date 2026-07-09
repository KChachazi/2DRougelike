using Game.Core;
using Game.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Weapons
{
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private ObjectPool bulletPool;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireCooldown = 0.25f;

        private PlayerController playerController;
        private float cooldownTimer;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            bool firePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool canFire = playerController == null || playerController.CanAct;

            if (firePressed && canFire && cooldownTimer <= 0f)
            {
                Fire();
                cooldownTimer = fireCooldown;
            }
        }

        private void Fire()
        {
            bulletPool.Get(firePoint.position, firePoint.rotation);
        }
    }
}