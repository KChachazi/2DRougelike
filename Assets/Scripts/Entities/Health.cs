using System;
using Game.Core;
using UnityEngine;

namespace Game.Entities
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool isDead => Current <= 0;
        public bool isInvincible { get; private set; }

        public event Action<int> Damaged;
        public event Action Died;
        public event Action<int, int> HealthChanged;

        private float invincibleTimer;
        private StatusEffectManager statusEffectManager;

        private void Awake()
        {
            Current = maxHealth;
            statusEffectManager = GetComponent<StatusEffectManager>();
        }

        private void Update()
        {
            if (!isInvincible) return ;
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f) isInvincible = false;
        }

        /* -------------- 公开接口 -------------- */
        public void SetInvincible(float duration)
        {
            isInvincible = true;
            invincibleTimer = duration;
        }
        public void TakeDamage(DamageInfo info)
        {
            if (isDead || isInvincible) return ;
            float multiplier = 1f;
            if (statusEffectManager != null)
                multiplier = statusEffectManager.DamageMultiplier;
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(info.Amount * multiplier));
            ApplyDamage(finalDamage);
        }
        public void TakeDamage(int amount)
        {
            if (isDead || isInvincible) return ;
            ApplyDamage(amount);
        }
        /* -------------- 私有工具 -------------- */
        private void ApplyDamage(int amount)
        {
            Current = Mathf.Max(0, Current - amount);
            Damaged?.Invoke(amount);
            HealthChanged?.Invoke(Current, maxHealth);
            if (isDead) Die();
        }
        private void Die()
        {
            Died?.Invoke();
            gameObject.SetActive(false);
        }
    }
}