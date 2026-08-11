using UnityEngine;
using Game.Core;
using Game.StateMachines;
using Game.StateMachines.Enemy;
using Game.Weapons;

namespace Game.Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyBehaviour behaviour;
        public EnemyBehaviour Behaviour => behaviour;

        public Rigidbody2D Rb { get; private set; }
        public Health health { get; private set; }
        public SpriteRenderer SpriteRenderer { get; private set; }
        public Color OriginalColor { get; private set; }
        public Transform Player { get; private set; }
        public Vector2 SpawnPosition { get; private set; }
        public StatusEffectManager StatusManager { get; private set; }

        
        public float PatrolSpeed => behaviour.patrolSpeed;
        public float PatrolRadius => behaviour.patrolRadius;
        public float ChaseSpeed => behaviour.chaseSpeed;
        public float DetectionRange => behaviour.detectionRange;
        public float LoseSightRange => behaviour.lostSightRange;
        public float AttackRange => behaviour.attackRange;
        public int ContactDamage => behaviour.contactDamage;
        public float AttackCooldown => behaviour.attackCooldown;
        public float KnockbackDuration => behaviour.knockbackDuration;

        public EnemyFreeState FreeState { get; private set; }
        public EnemyKnockbackState KnockbackState { get; private set; }
        public EnemyDeadState DeadState { get; private set; }

        public Vector2 KnockbackDirection { get; private set; }
        public float KnockbackSpeed { get; private set; }

        public bool IsActionLocked =>
            stateMachine.CurrentState == KnockbackState || stateMachine.CurrentState == DeadState;

        private readonly StateMachine stateMachine = new StateMachine();
        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            SpriteRenderer = GetComponent<SpriteRenderer>();
            StatusManager = GetComponent<StatusEffectManager>();
            OriginalColor = SpriteRenderer.color;
            SpawnPosition = Rb.position;
            // StateMachine
            FreeState = new EnemyFreeState(this, stateMachine);
            KnockbackState = new EnemyKnockbackState(this, stateMachine);
            DeadState = new EnemyDeadState(this, stateMachine);
        }
        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
        private void Start()
        {
            GameObject playerObj =GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                Player = playerObj.transform;
            stateMachine.ChangeState(FreeState);
        }
        private void Update()
        {
            stateMachine.Tick();
        }
        private void FixedUpdate()
        {
            stateMachine.FixedTick();
        }
        // === 公开接口 ===
        public float DistanceToPlayer()
        {
            return Player == null ? float.MaxValue : Vector2.Distance(Rb.position, Player.position);
        }
        public void MoveTowards(Vector2 targetPosition, float speed)
        {
            float multiplier = StatusManager != null ? StatusManager.SpeedMultiplier : 1f;
            Vector2 direction = (targetPosition - Rb.position).normalized;
            Rb.MovePosition(Rb.position + direction * speed * multiplier * Time.fixedDeltaTime);
        }
        public void TriggerKnockback(Vector2 direction, float force)
        {
            KnockbackDirection = direction.normalized;
            KnockbackSpeed = behaviour.knockbackSpeedMultiplier * force;
            stateMachine.ChangeState(KnockbackState);
        }
        // === 私有工具 ===
        private void OnDamaged(int amount)
        {
            EventBus.Publish(new EnemyDamagedEvent(Rb.position, amount));
        }
        private void OnDied()
        {
            EventBus.Publish(new EnemyDiedEvent(Rb.position));
            stateMachine.ChangeState(DeadState);
        }
    }
}