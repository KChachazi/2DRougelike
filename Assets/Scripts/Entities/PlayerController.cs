using Game.Core;
using Game.StateMachines;
using Game.StateMachines.Player;
using Game.Rewards;
using UnityEngine;

namespace Game.Entities
{
    /// <summary>
    /// 玩家实体上下文，持有组件、运行玩家 FSM，并向命令和状态暴露移动、
    /// 瞄准、攻击与闪避能力；不直接读取键盘或鼠标。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Header("=== 移动 ===")]
        [SerializeField] private float moveSpeed = 5f;
        
        [Header("=== 闪避 ===")]
        [SerializeField] private float dashSpeed = 14f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 0.6f;

        [Header("=== 受击 ===")]
        [SerializeField] private float hurtDuration = 0.3f;
        [SerializeField] private float hurtInvincibleDuration = 0.6f;

        [Header("=== 近战表现 ===")]
        [SerializeField] private float attackDuration = 0.25f;

        public Rigidbody2D Rb { get; private set; }
        public Health health { get; private set; }
        public SpriteRenderer SpriteRenderer { get; private set; }
        public Vector2 MoveInput { get; private set; }
        public StatusEffectManager StatusEffectManager { get; private set; }
        public RunModifierSet RunModifiers { get; private set; }

        private readonly StateMachine stateMachine = new StateMachine();
        private float dashCooldownTimer;

        public float MoveSpeed => moveSpeed * (RunModifiers != null ? RunModifiers.MoveSpeedMultiplier : 1f);
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

        /// <summary>只有 Idle 与 Move 状态允许响应普通行动命令。</summary>
        public bool CanAct => stateMachine.CurrentState == IdleState || stateMachine.CurrentState == MoveState;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            SpriteRenderer = GetComponent<SpriteRenderer>();
            StatusEffectManager = GetComponent<StatusEffectManager>();
            RunModifiers = GetComponent<RunModifierSet>();

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
            // Start 广播，确保 UI 已经完成 OnEnable 订阅。
            EventBus.Publish(new PlayerHealthChangedEvent(health.Current, health.Max)); // 广播初始满血
        }
        private void Update()
        {
            if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;
            stateMachine.Tick();
        }
        private void FixedUpdate()
        {
            Rb.linearVelocity = Vector2.zero;
            stateMachine.FixedTick();
        }

        /// <summary>接收输入层计算好的归一化移动方向。</summary>
        public void SetMoveInput(Vector2 input)
        {
            MoveInput = input;
        }
        /// <summary>由移动状态在物理帧调用，并自动应用减速倍率。</summary>
        public void Move(float speed)
        {
            float multiplier = StatusEffectManager != null ? StatusEffectManager.SpeedMultiplier : 1f;
            Rb.MovePosition(Rb.position + MoveInput * speed * multiplier * Time.fixedDeltaTime);
        }
        /// <summary>接收输入层计算好的世界空间瞄准方向并旋转刚体。</summary>
        public void SetAimDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return ;
            Rb.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
        /// <summary>
        /// 玩家进入攻击状态，供 WeaponController 在近战开火时调用。
        /// </summary>
        // 切到 AttackState 播放“变黄 + 阻断”表现。
        public void TriggerAttack()
        {
            stateMachine.ChangeState(AttackState);
        }
        /// <summary>
        /// 玩家进入冲刺状态，供 DashCommand 调用。
        /// </summary>
        public void TriggerDash()
        {
            stateMachine.ChangeState(DashState);
        }
        /// <summary>
        /// 开始计时冲刺冷却，玩家进入冲刺状态后自动调用。
        /// </summary>
        public void StartDashCooldown()
        {
            dashCooldownTimer = dashCooldown;
            EventBus.Publish(new SkillCooldownStartedEvent(SkillId.Dash, dashCooldown));
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
