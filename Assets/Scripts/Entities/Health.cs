using System;
using Game.Core;
using UnityEngine;

namespace Game.Entities
{
    /// <summary>
    /// 通用血量组件。
    /// </summary>
    //
    // 与 StatusEffectManager 的分工：
    // Health 管理 HP；StatusEffectManager 管理 BUFF/DEBUFF。
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool isDead => Current <= 0;
        public bool isInvincible { get; private set; }

        /// <summary>
        /// 该对象受到伤害时触发，参数为伤害值。
        /// </summary>
        public event Action<int> Damaged;

        /// <summary>
        /// 该对象死亡时触发。
        /// </summary>
        public event Action Died;

        /// <summary>
        /// 当生命值发生变化时触发，参数为当前生命值和最大生命值
        /// </summary>
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

        // ======================== 公开接口 ========================
        /// <summary>无敌帧</summary>
        public void SetInvincible(float duration)
        {
            isInvincible = true;
            invincibleTimer = duration;
        }
        /// <summary>
        /// V1.5 主入口：接受 DamageInfo，自动处理易伤倍率。
        /// 应用易伤倍率的完整伤害入口应使用此重载。
        /// </summary>
        public void TakeDamage(DamageInfo info)
        {
            if (isDead || isInvincible) return ;
            float multiplier = 1f;
            if (statusEffectManager != null)
                multiplier = statusEffectManager.DamageMultiplier;
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(info.Amount * multiplier));
            ApplyDamage(finalDamage);
        }
        /// <summary>
        /// 仅接受整数伤害（无易伤倍率处理）。
        /// DoT伤害等应使用此重载。
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (isDead || isInvincible) return ;
            ApplyDamage(amount);
        }
        public void Heal(int amount)
        {
            if (isDead) return ;
            Current = Mathf.Min(Max, Current + amount);
            HealthChanged?.Invoke(Current, maxHealth);
        }
        // ======================== 私有工具 ========================
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
