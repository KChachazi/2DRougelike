using UnityEngine;

namespace Game.Entities
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        public int Current { get; private set; }
        public bool isDead => Current <= 0;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (isDead) return ;
            Current = Mathf.Max(0, Current - amount);

            if (isDead)
            {
                Die();
            }
        }

        private void Die()
        {
            gameObject.SetActive(false);
        }
    }
}