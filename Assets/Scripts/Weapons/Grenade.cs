using Game.Core;
using Game.Entities;
using UnityEngine;

namespace Game.Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Grenade : MonoBehaviour, IPoolable
    {
        [Header("投掷")]
        [SerializeField] private float throwSpeed = 8f;
        [Tooltip("阻尼：越大则手雷越快停止")]
        [SerializeField] private float drag = 3f;

        [Header("爆炸")]
        [Tooltip("引信时间")]
        [SerializeField] private float fuseTime = 1f;
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private int damage = 40;
        [Tooltip("爆炸特效持续时间")]
        [SerializeField] private float effectDuration = 0.15f;

        [Header("表现")]
        [Tooltip("手雷本体")]
        [SerializeField] private GameObject bodyVisual;
        [Tooltip("爆炸范围")]
        [SerializeField] private GameObject explosionVisual;

        private Rigidbody2D rb;

        private float timer;
        private bool exploded;
        private const int MaxHits = 32;
        private readonly Collider2D[] hitBuffer = new Collider2D[MaxHits];
        private readonly ContactFilter2D filter = ContactFilter2D.noFilter;

        public ObjectPool Pool { get; set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            timer = 0f;
            exploded = false;
            rb.linearDamping = drag;
            rb.linearVelocity = transform.right * throwSpeed;
            bodyVisual.SetActive(true);
            explosionVisual.SetActive(false);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (!exploded)
            {
                if (timer >= fuseTime) Explode();
                return ;
            }
            if (timer >= fuseTime + effectDuration) ReturnToPool();
        }

        private void Explode()
        {
            exploded = true;
            rb.linearVelocity = Vector2.zero;
            int count = Physics2D.OverlapCircle(transform.position, explosionRadius, filter, hitBuffer);
            for (int i = 0; i < count; i ++)
            {
                if (!hitBuffer[i].CompareTag("Enemy")) continue;
                if (hitBuffer[i].TryGetComponent(out Health health))
                    health.TakeDamage(damage);
            }

            bodyVisual.SetActive(false);
            explosionVisual.transform.localScale = Vector3.one * explosionRadius * 2f;
            explosionVisual.SetActive(true);
        }

        private void ReturnToPool()
        {
            if (Pool != null) Pool.Release(gameObject);
            else Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}