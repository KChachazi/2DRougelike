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

        [Header("近战")]
        [SerializeField] private float attackDuration = 0.25f;
        [SerializeField] private float attackRange = 1f;
        [SerializeField] private int attackDamage = 20;

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
        public float AttackRange => attackRange;
        public int AttackDamage => attackDamage;
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
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
        }

        private void Start()
        {
            stateMachine.ChangeState(IdleState);
        }

        private void Update()
        {
            ReadMoveInput();
            RotateTowardsMouse();
            if (dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= Time.deltaTime;
            }
            stateMachine.Tick();
        }

        private void FixedUpdate()
        {
            stateMachine.FixedTick();
        }

        public void Move(float speed)
        {
            Rb.MovePosition(Rb.position + MoveInput * speed * Time.fixedDeltaTime);
        }

        public void StartDashCooldown()
        {
            dashCooldownTimer = dashCooldown;
        }

        public bool ConsumeDashPressed()
        {
            return Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
        }

        public bool ConsumeAttackPressed()
        {
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
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
    }
}