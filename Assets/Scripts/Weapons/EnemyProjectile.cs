using Game.Core;
using Game.Entities;
using UnityEngine;


namespace Game.Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyProjectile : MonoBehaviour, IPoolable
    {
        [Tooltip("飞行时间上限")]
        [SerializeField] private float lifeTime = 2f;
        [Tooltip("基本伤害")]
        [SerializeField] private int fallbackDamage = 8;

        private Rigidbody2D rb;
        private float timer;
        private float speed;
        private DamageInfo damageInfo;

        public ObjectPool Pool { get; set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            damageInfo = new DamageInfo(fallbackDamage);
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
            if (!other.CompareTag("Player")) return ;
            if (other.TryGetComponent(out Health health))
                health.TakeDamage(damageInfo);
            if (other.TryGetComponent(out StatusEffectManager statusManager))
                statusManager.ApplyEffects(damageInfo);
            ReturnToPool();
        }
        public void Launch(Vector3 position, Quaternion rotation, float speed, DamageInfo info)
        {
            transform.SetPositionAndRotation(position, rotation);
            this.speed = speed;
            damageInfo = info;
        }

        private void ReturnToPool()
        {
            rb.linearVelocity = Vector2.zero;
            if (Pool != null)
                Pool.Release(gameObject);
            else Destroy(gameObject);
        }
    }
}