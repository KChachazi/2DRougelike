using Game.Core;
using Game.Entities;
using UnityEngine;

namespace Game.Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifeTime = 2f;
        [SerializeField] private int damage = 10;

        private Rigidbody2D rb;
        private float timer;
        private DamageInfo damageInfo;

        public ObjectPool Pool { get; set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            damageInfo = new DamageInfo(damage);
        }

        private void OnEnable()
        {
            timer = 0f;
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = transform.right * speed;
            timer += Time.fixedDeltaTime;
            if (timer >= lifeTime)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Enemy")) return ;
            // 1. 造成伤害
            if (other.TryGetComponent(out Health health))
                health.TakeDamage(damageInfo);
            // 2. 状态异常
            if (other.TryGetComponent(out StatusEffectManager statusEffectManager))
            {
                statusEffectManager.ApplyEffects(damageInfo);
            }
            // 3. 击退
            if (other.TryGetComponent(out EnemyController enemy))
            {
                if (damageInfo.KnockbackForce > 0f)
                {
                    Vector2 knockDirection = ((Vector2)other.transform.position - rb.position).normalized;
                    enemy.TriggerKnockback(knockDirection, damageInfo.KnockbackForce);
                }
            }
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            rb.linearVelocity = Vector2.zero;
            if (Pool != null)
            {
                Pool.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetDamageInfo(DamageInfo info) => damageInfo = info;
        public void SetDamage(int value) => damageInfo = new DamageInfo(value);
    }
}