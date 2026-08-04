using UnityEngine;
using Game.Core;
using Game.StateMachines;
using Game.StateMachines.Enemy;
using System;

namespace Game.Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        [Header("巡逻")]
        [SerializeField] private float patrolSpeed = 1.2f;
        [SerializeField] private float patrolRadius = 2f;

        [Header("追击")]
        [SerializeField] private float chaseSpeed = 2.5f;
        [SerializeField] private float detectionRange = 4f;
        [SerializeField] private float loseSightRange = 6f;

        [Header("攻击")]
        [SerializeField] private float attackRange = 1f;
        [SerializeField] private int contactDamage = 10;
        [SerializeField] private float attackCooldown = 1f;        

        [Header("击退")]
        [SerializeField] private float knockbackDuration = 0.15f;
        [SerializeField] private float knockbackSpeedMultiplier = 10f;
        public Vector2 KnockbackDirection { get; private set; }
        public float KnockbackSpeed { get; private set; }
        public float KnockbackDuration => knockbackDuration;

        public Rigidbody2D Rb { get; private set; }
        public Health health { get; private set; }
        public SpriteRenderer SpriteRenderer { get; private set; }
        public Color OriginalColor { get; private set; }
        public Transform Player { get; private set; }
        public Vector2 SpawnPosition { get; private set; }
        public StatusEffectManager StatusEffectManager { get; private set; }

        public float PatrolSpeed => patrolSpeed;
        public float PatrolRadius => patrolRadius;
        public float ChaseSpeed => chaseSpeed;
        public float DetectionRange => detectionRange;
        public float LoseSightRange => loseSightRange;
        public float AttackRange => attackRange;
        public int ContactDamage => contactDamage;
        public float AttackCooldown => attackCooldown;

        public EnemyPatrolState PatrolState { get; private set; }
        public EnemyChaseState ChaseState { get; private set; }
        public EnemyAttackState AttackState { get; private set; }
        public EnemyDeadState DeadState { get; private set; }
        public EnemyKnockbackState KnockbackState { get; private set; }

        private readonly StateMachine stateMachine = new StateMachine();

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            SpriteRenderer = GetComponent<SpriteRenderer>();
            StatusEffectManager = GetComponent<StatusEffectManager>();
            OriginalColor = SpriteRenderer.color;
            SpawnPosition = Rb.position;

            PatrolState = new EnemyPatrolState(this, stateMachine);
            ChaseState = new EnemyChaseState(this, stateMachine);
            AttackState = new EnemyAttackState(this, stateMachine);
            DeadState = new EnemyDeadState(this, stateMachine);
            KnockbackState = new EnemyKnockbackState(this, stateMachine);
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
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Player = playerObj.transform;
            }

            stateMachine.ChangeState(PatrolState);
        }
        private void Update()
        {
            stateMachine.Tick();
        }
        private void FixedUpdate()
        {
            stateMachine.FixedTick();
        }

        public float DistanceToPlayer()
        {
            return Player == null ? float.MaxValue : Vector2.Distance(Rb.position, Player.position);
        }
        public void MoveTowards(Vector2 targetPosition, float speed)
        {
            float multiplier = StatusEffectManager != null ? StatusEffectManager.SpeedMultiplier : 1f;
            Vector2 direction = (targetPosition - Rb.position).normalized;
            Rb.MovePosition(Rb.position + direction * speed * multiplier * Time.fixedDeltaTime);
        }
        public void TriggerKnockback(Vector2 direction, float force)
        {
            KnockbackDirection = direction.normalized;
            KnockbackSpeed = knockbackSpeedMultiplier * force;
            stateMachine.ChangeState(KnockbackState);
        }

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