using System;
using UnityEngine;

namespace Game.Entities
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        public int Current { get; private set; }
        public bool isDead => Current <= 0;
        public bool isInvincible { get; private set; }

        public event Action<int> Damaged;
        public event Action Died;

        private float invincibleTimer;

        private void Awake()
        {
            Current = maxHealth;
        }

        private void Update()
        {
            if (!isInvincible) return ;
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
            }
        }

        /* -------------- 公开接口 -------------- */
        public void SetInvincible(float duration)
        {
            isInvincible = true;
            invincibleTimer = duration;
        }

        public void TakeDamage(int amount)
        {
            if (isDead || isInvincible) return ;
            Current = Mathf.Max(0, Current - amount);
            Damaged?.Invoke(amount);

            if (isDead)
            {
                Die();
            }
        }

        private void Die()
        {
            Died?.Invoke();
            gameObject.SetActive(false);
        }
    }
}