using Game.Core;
using Game.StateMachines;
using Game.StateMachines.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        [SerializeField] private float moveSpeed = 5f;
        
        [Header("闪避")]
        [SerializeField] private float dashSpeed = 14f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 0.6f;

        [Header("受击")]
        [SerializeField] private float hurtDuration = 0.3f;
        [SerializeField] private float hurtInvincibleDuration = 0.6f;

        [Header("近战表现")]
        [SerializeField] private float attackDuration = 0.25f;

        public Rigidbody2D Rb { get; private set; }
        public Health health { get; private set; }
        public SpriteRenderer SpriteRenderer { get; private set; }
        public Vector2 MoveInput { get; private set; }

        private readonly StateMachine stateMachine = new StateMachine();
        private Camera mainCamera;
        private float dashCooldownTimer;

        public float MoveSpeed => moveSpeed;
        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDuration;
        public float HurtDuration => hurtDuration;
        public float HurtInvincibleDuration => hurtInvincibleDuration;
        public float AttackDuration => attackDuration;
        public bool CanDash => dashCooldownTimer <= 0f;

        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerDashState DashState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }
        public PlayerHurtState HurtState { get; private set; }

        public bool CanAct => stateMachine.CurrentState == IdleState || stateMachine.CurrentState == MoveState;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            SpriteRenderer = GetComponent<SpriteRenderer>();
            mainCamera = Camera.main;

            IdleState = new PlayerIdleState(this, stateMachine);
            MoveState = new PlayerMoveState(this, stateMachine);
            DashState = new PlayerDashState(this, stateMachine);
            AttackState = new PlayerAttackState(this, stateMachine);
            HurtState = new PlayerHurtState(this, stateMachine);
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.HealthChanged -= OnHealthChanged;
        }

        private void Start()
        {
            stateMachine.ChangeState(IdleState);
            EventBus.Publish(new PlayerHealthChangedEvent(health.Current, health.Max)); // 广播初始满血
        }

        private void Update()
        {
            ReadMoveInput();
            RotateTowardsMouse();
            if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;
            stateMachine.Tick();
        }

        private void FixedUpdate()
        {
            Rb.linearVelocity = Vector2.zero;
            stateMachine.FixedTick();
        }

        public void Move(float speed)
        {
            Rb.MovePosition(Rb.position + MoveInput * speed * Time.fixedDeltaTime);
        }

        // for Dash
        public void StartDashCooldown()
        {
            dashCooldownTimer = dashCooldown;
        }
        public bool ConsumeDashPressed()
        {
            return Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
        }

        // 供 WeaponController 在近战开火时调用
        // 可以切到 AttackState 播放"变黄 + 阻断"表现
        public void TriggerAttack()
        {
            stateMachine.ChangeState(AttackState);
        }

        private void ReadMoveInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                MoveInput = Vector2.zero;
                return ;
            }
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            MoveInput = new Vector2(x, y).normalized;
        }

        private void RotateTowardsMouse()
        {
            if (Mouse.current == null || mainCamera == null) return ;
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
            
            Vector2 direction = (Vector2)worldPos - Rb.position;
            if (direction.sqrMagnitude < 0.0001f) return ;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Rb.rotation = angle;
        }

        private void OnDamaged(int amount)
        {
            stateMachine.ChangeState(HurtState);
        }

        private void OnHealthChanged(int current, int max)
        {
            EventBus.Publish(new PlayerHealthChangedEvent(current, max));
        }
    }
}