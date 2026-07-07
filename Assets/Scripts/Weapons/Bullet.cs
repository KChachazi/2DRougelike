using Game.Core;
using Game.Entities;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Game.Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifeTime = 2f;
        [SerializeField] private int damage = 10;

        private Rigidbody2D rb;
        private float timer;

        public ObjectPool Pool { get; set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
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
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("enemy")) return ;
            if (other.TryGetComponent(out Health health))
            {
                health.TakeDamage(damage);
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
    }
}