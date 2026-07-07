using UnityEngine;

namespace Game.Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private int contactDamage = 10;
        [SerializeField] private float damageCooldown = 1f;

        private Rigidbody2D rb;
        private Transform player;
        private float damageTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        private void Update()
        {
            if (damageTimer > 0f)
            {
                damageTimer -= Time.deltaTime;
            }
        }

        private void FixedUpdate()
        {
            if (player == null) return ;
            Vector2 direction = ((Vector2)player.position - rb.position).normalized;
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (damageTimer > 0f) return ;
            if (!collision.collider.CompareTag("Player")) return ;
            if (collision.collider.TryGetComponent(out Health health))
            {
                health.TakeDamage(contactDamage);
                damageTimer = damageCooldown;
            }
        }
    }
}