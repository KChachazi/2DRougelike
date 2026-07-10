# 第 2 周:状态机与敌人基础 AI

> 这周的核心是把第 1 周"一个脚本管所有逻辑"的写法,重构成状态机(FSM)。代码量比第 1 周多不少,但都遵循同一套模式,理解了第一个状态,后面的都是重复套路。照旧,不懂的地方随时问。

## 本周目标(对齐 README 六周计划)

- 玩家状态机:`Idle`(待机)、`Move`(移动)、`Dash`(闪避)、`Attack`(近战攻击)、`Hurt`(受击)
- 敌人状态机:`Patrol`(巡逻)、`Chase`(追击)、`Attack`(攻击)、`Dead`(死亡)
- 闪避(Dash)+ 无敌帧 + 受击闪白
- 近战武器原型

完成后:按 `Shift` 闪避一小段距离(闪避期间无敌);按鼠标右键近战攻击(角色短暂变黄,面前一圈范围内的敌人掉血);被敌人碰到会短暂无敌 + 红色闪烁(初版写的是闪白,验收后发现角色本身就是白的、闪白看不见,改成了闪红,见第 5.2 节);敌人会在你靠近时从白色(巡逻)变橙色(追击)、贴身后变红色(攻击),被打死后变灰消失;之前的移动、朝向鼠标、远程射击(左键)全部保留。

---

## 1. 为什么这么设计

### 1.1 为什么用状态类,不用 `enum + switch`

`CLAUDE.md` 里的架构约定写得很明确:**状态很少时才用枚举+switch,否则优先状态类**。这周玩家有 5 个状态、敌人有 4 个,而且每个状态的行为都不一样(移动方式、能不能被打断、要不要检测输入),用一个大 `switch` 会变成一坨很长的 `if/else`,以后加状态(比如第 6 周可能加的"翻滚受伤特效")还要去改这个大函数,容易牵一发动全身。拆成一个类一个状态之后,新增状态 = 新建一个文件,不用动其他状态的代码。

### 1.2 状态机引擎和"具体状态"是分开的

`Assets/Scripts/StateMachines/` 下有两种东西:

- **引擎本身**(`IState.cs`、`StateMachine.cs`):只有两个很小的文件,不认识"玩家""敌人"是什么,只知道"状态"要有 `Enter/Tick/FixedTick/Exit` 四个方法,状态机负责在状态之间切换,切换时自动调用上一个状态的 `Exit()` 和下一个状态的 `Enter()`。
- **具体状态**(`StateMachines/Player/...`、`StateMachines/Enemy/...`):比如 `PlayerDashState`,知道"闪避"具体要干什么,但它不认识"状态机"内部是怎么切换的,只管调用 `stateMachine.ChangeState(...)`。

这样拆的好处是:**同一套 `StateMachine` 引擎,玩家和敌人都能用**,以后如果要给 NPC、Boss 加状态机,也是复用这两个文件,不用重写。

对应到 `Update`/`FixedUpdate` 的约定:`IState.Tick()` 在 `Update` 里调用(处理输入、判断要不要切状态),`IState.FixedTick()` 在 `FixedUpdate` 里调用(处理物理位移)——这和第 1 周"输入放 Update、物理放 FixedUpdate"的原则是一致的,只是现在这个原则被下放到了每个状态类里。

### 1.3 "状态"怎么拿到玩家/敌人身上的数据

每个状态类的构造函数都长这样:

```csharp
public PlayerDashState(PlayerController player, StateMachine stateMachine)
```

`player` 就是"上下文"(context)——状态类通过它读写 Rigidbody2D、当前输入、血量组件等等,而不是自己重新声明一遍这些字段。`PlayerController`/`EnemyController` 现在的角色变成了:**持有数据 + 在 `Awake` 里把所有状态实例创建好 + 每帧调用状态机的 `Tick`/`FixedTick`**,具体"这一帧要干什么"完全交给当前状态决定。

### 1.4 无敌帧和"受击"事件放在 `Health` 里

第 1 周的 `Health` 只会扣血、死亡。这周加了两样东西:

- `IsInvincible` + `SetInvincible(duration)`:无敌状态下 `TakeDamage` 直接跳过。闪避、受击之后都需要一小段无敌时间,与其在每个用到的地方各写一遍计时器,不如让"血量组件"自己知道"我现在能不能被打"。
- `event Action<int> Damaged` 和 `event Action Died`:**这是一个轻量级的观察者模式,可以理解成第 3 周正式 EventBus 之前的"预演"**。`PlayerController` 订阅 `Damaged`,一挨打就自动切到 `HurtState`;`EnemyController` 订阅 `Died`,一死就自动切到 `DeadState`。好处是 `Health` 不需要认识 `PlayerController`/`StateMachine` 是什么,只管"我扣血了""我死了"这两件事往外广播,谁关心谁自己订阅——这正是 EventBus 要解决的"模块间解耦"问题的雏形。

### 1.5 敌人伤害判定方式变了

第 1 周敌人造成伤害是靠物理碰撞回调 `OnCollisionStay2D`。这周改成了**距离判断**:`EnemyAttackState` 每帧算一下和玩家的距离,在 `attackRange` 内、冷却好了就扣血。原因是现在"要不要打人"本来就该由状态决定(只有进入 `Attack` 状态才会打人,`Patrol`/`Chase` 状态不会),继续依赖物理回调的话,状态和伤害判定就变成两套独立逻辑,容易对不上。**物理碰撞本身(会互相撞开)没有变**,玩家和敌人的 Collider2D 设置不需要动。

### 1.6 近战攻击为什么直接写在 `PlayerAttackState` 里,没有单独建 `MeleeWeapon`

对照架构约定表,`IWeaponStrategy` 接口 + 武器切换是**第 3 周**要做的事。这周先把"近战攻击"作为 `Attack` 状态的一部分跑通(进入状态的瞬间,在角色前方画一个圈,圈里的敌人扣血),下周会把这部分和现有的远程射击一起收进统一的武器策略系统。现在这样写不是偷懒,是刻意不提前引入还用不上的抽象。

### 1.7 调试用的颜色反馈

`PlayerAttackState`/`PlayerHurtState`/敌人的每个状态,进入时都会改一下 `SpriteRenderer.color`(攻击变黄、受击闪白、巡逻白色、追击橙色、攻击红色、死亡灰色)。现在没有美术资源和动画,这是最低成本的"肉眼确认状态机对不对"的手段——你能直接看着敌人从白变橙再变红,而不用猜它内部状态是什么。以后有动画了可以把这些颜色代码删掉换成播放对应动画,不影响状态机本身的逻辑。

---

## 2. 需要在 `Assets/Scripts/` 下新建/修改的文件

```
Assets/Scripts/StateMachines/IState.cs                    # 新建:状态接口
Assets/Scripts/StateMachines/StateMachine.cs               # 新建:状态机引擎
Assets/Scripts/StateMachines/Player/PlayerIdleState.cs      # 新建
Assets/Scripts/StateMachines/Player/PlayerMoveState.cs      # 新建
Assets/Scripts/StateMachines/Player/PlayerDashState.cs      # 新建
Assets/Scripts/StateMachines/Player/PlayerAttackState.cs    # 新建
Assets/Scripts/StateMachines/Player/PlayerHurtState.cs      # 新建
Assets/Scripts/StateMachines/Enemy/EnemyPatrolState.cs      # 新建
Assets/Scripts/StateMachines/Enemy/EnemyChaseState.cs       # 新建
Assets/Scripts/StateMachines/Enemy/EnemyAttackState.cs      # 新建
Assets/Scripts/StateMachines/Enemy/EnemyDeadState.cs        # 新建
Assets/Scripts/Entities/Health.cs                           # 修改:加无敌帧 + 事件
Assets/Scripts/Entities/PlayerController.cs                 # 重写:变成状态机的"上下文"
Assets/Scripts/Entities/EnemyController.cs                  # 重写:变成状态机的"上下文"
Assets/Scripts/Weapons/PlayerShooter.cs                     # 修改:闪避/攻击/受击时不能开枪
```

参考实现在 `Reference/Scripts/...`(与 `Assets/` 同级,已加入 `.gitignore`)对应路径下,目录结构完全镜像上面这份清单。建议**先建两个新文件夹 `Assets/Scripts/StateMachines/Player/` 和 `Assets/Scripts/StateMachines/Enemy/`**,再对照参考代码逐个文件敲。

> `PlayerController.cs`/`EnemyController.cs` 是"重写"不是"新建"——直接把里面的内容整体替换成新版本即可,类名、命名空间、文件路径都没变,Unity 不需要重新挂载组件,已有的 Inspector 引用(比如 `PlayerShooter` 上拖的 `Bullet Pool`)也不会丢。少数字段改了名字(比如 `EnemyController` 的 `moveSpeed` 拆成了 `patrolSpeed`/`chaseSpeed`),这些字段会变回默认值,等下面"编辑器操作"里会提示你确认。

---

## 3. 完整代码

### 3.1 `Assets/Scripts/StateMachines/IState.cs`

```csharp
namespace Game.StateMachines
{
    public interface IState
    {
        void Enter();
        void Tick();
        void FixedTick();
        void Exit();
    }
}
```

四个方法分别是:进入状态时调一次(`Enter`)、每个 `Update` 调一次(`Tick`)、每个 `FixedUpdate` 调一次(`FixedTick`)、离开状态时调一次(`Exit`)。哪怕某个状态用不上某个方法,也要把四个都写出来(哪怕方法体是空的)——这是接口的硬性要求,不是遗漏。

### 3.2 `Assets/Scripts/StateMachines/StateMachine.cs`

```csharp
namespace Game.StateMachines
{
    public class StateMachine
    {
        public IState CurrentState { get; private set; }

        public void ChangeState(IState nextState)
        {
            if (nextState == CurrentState) return;

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState?.Enter();
        }

        public void Tick() => CurrentState?.Tick();
        public void FixedTick() => CurrentState?.FixedTick();
    }
}
```

注意这不是 `MonoBehaviour`——它是一个纯 C# 类,不需要挂在 GameObject 上。`PlayerController`/`EnemyController` 各自在内部 `new` 一个 `StateMachine` 出来使用。`ChangeState` 开头判断"如果目标状态就是当前状态,直接返回"是为了避免重复触发 `Enter`(比如敌人已经在 `Chase` 了,每帧都判断"距离够近,切到 Chase",如果不做这个检查,会每帧都重新 `Enter` 一次)。

### 3.3 `Assets/Scripts/Entities/Health.cs`(修改)

```csharp
using System;
using UnityEngine;

namespace Game.Entities
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        public int Current { get; private set; }
        public bool IsDead => Current <= 0;
        public bool IsInvincible { get; private set; }

        public event Action<int> Damaged;
        public event Action Died;

        private float invincibleTimer;

        private void Awake()
        {
            Current = maxHealth;
        }

        private void Update()
        {
            if (!IsInvincible) return;

            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
            {
                IsInvincible = false;
            }
        }

        public void SetInvincible(float duration)
        {
            IsInvincible = true;
            invincibleTimer = duration;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || IsInvincible) return;

            Current = Mathf.Max(0, Current - amount);
            Damaged?.Invoke(amount);

            if (IsDead)
            {
                Die();
            }
        }

        private void Die()
        {
            Died?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
```

`event Action<int> Damaged` 里的 `Action<int>` 表示"一个接收 `int` 参数、没有返回值的方法"。别人订阅时写 `Health.Damaged += OnDamaged`,`OnDamaged` 方法签名要长成 `void OnDamaged(int amount)`。`Damaged?.Invoke(amount)` 里的 `?.` 是判空——如果没有任何人订阅过这个事件,`Damaged` 是 `null`,直接调用 `.Invoke()` 会报错,`?.` 帮你省了一次 `if (Damaged != null)`。

### 3.4 `Assets/Scripts/Entities/PlayerController.cs`(重写)

```csharp
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

        private readonly StateMachine stateMachine = new StateMachine();
        private Camera mainCamera;
        private float dashCooldownTimer;

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
                return;
            }

            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);

            MoveInput = new Vector2(x, y).normalized;
        }

        private void RotateTowardsMouse()
        {
            if (Mouse.current == null || mainCamera == null) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));

            Vector2 direction = (Vector2)worldPos - Rb.position;
            if (direction.sqrMagnitude < 0.0001f) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Rb.rotation = angle;
        }

        private void OnDamaged(int amount)
        {
            stateMachine.ChangeState(HurtState);
        }
    }
}
```

几个关键点:

- `ConsumeDashPressed()`/`ConsumeAttackPressed()` 用的是 `wasPressedThisFrame`,不是 `isPressed`。`isPressed`(第 1 周开枪用的)是"这一帧按键是不是按着的",按住不放每帧都是 `true`;`wasPressedThisFrame` 是"这一帧是不是刚按下的那一下",按住不放也只会触发一次。闪避、近战都应该是"点一下触发一次",不能像开枪一样按住连发,所以这里换了 API。
- `CanAct` 判断当前状态是不是 `Idle` 或 `Move`——这是给 `PlayerShooter` 用的:闪避/攻击/受击的时候不能再开枪。
- `ReadMoveInput()`/`RotateTowardsMouse()` 仍然在 `Update()` 里无条件执行(不管当前是什么状态),这样任何状态都能拿到最新的输入和朝向;至于"要不要用这个输入移动",完全交给当前状态自己决定(比如 `HurtState` 就不会调用 `Move()`)。

### 3.5 `Assets/Scripts/Entities/EnemyController.cs`(重写)

```csharp
using Game.StateMachines;
using Game.StateMachines.Enemy;
using UnityEngine;

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

        public Rigidbody2D Rb { get; private set; }
        public Health health { get; private set; }
        public SpriteRenderer SpriteRenderer { get; private set; }
        public Transform Player { get; private set; }
        public Vector2 SpawnPosition { get; private set; }

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

        private readonly StateMachine stateMachine = new StateMachine();

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            SpriteRenderer = GetComponent<SpriteRenderer>();
            SpawnPosition = Rb.position;

            PatrolState = new EnemyPatrolState(this, stateMachine);
            ChaseState = new EnemyChaseState(this, stateMachine);
            AttackState = new EnemyAttackState(this, stateMachine);
            DeadState = new EnemyDeadState(this, stateMachine);
        }

        private void OnEnable()
        {
            health.Died += OnDied;
        }

        private void OnDisable()
        {
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
            Vector2 direction = (targetPosition - Rb.position).normalized;
            Rb.MovePosition(Rb.position + direction * speed * Time.fixedDeltaTime);
        }

        private void OnDied()
        {
            stateMachine.ChangeState(DeadState);
        }
    }
}
```

`SpawnPosition = Rb.position` 记录的是敌人**出生时**的位置,`Patrol` 状态会围绕这个点随机游走,而不是原点或者玩家位置——这样场景里放在不同位置的敌人,巡逻范围是各自独立的一圈,不会都跑到地图中心去。

### 3.6 玩家状态类(`Assets/Scripts/StateMachines/Player/`)

**`PlayerIdleState.cs`**

```csharp
using Game.Entities;

namespace Game.StateMachines.Player
{
    public class PlayerIdleState : IState
    {
        private readonly PlayerController player;
        private readonly StateMachine stateMachine;

        public PlayerIdleState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter() { }

        public void Tick()
        {
            if (player.ConsumeDashPressed() && player.CanDash)
            {
                stateMachine.ChangeState(player.DashState);
                return;
            }

            if (player.ConsumeAttackPressed())
            {
                stateMachine.ChangeState(player.AttackState);
                return;
            }

            if (player.MoveInput.sqrMagnitude > 0.01f)
            {
                stateMachine.ChangeState(player.MoveState);
            }
        }

        public void FixedTick() { }

        public void Exit() { }
    }
}
```

`Idle` 什么都不做,只负责"看有没有满足切换到别的状态的条件"。用 `sqrMagnitude > 0.01f` 而不是直接判断 `!= Vector2.zero`,是因为浮点数比较相等不可靠(练习:以后碰到"这个数为什么应该是 0 但判断不通过"的问题,先怀疑是不是用 `==` 比较了浮点数);`sqrMagnitude`(长度的平方)比 `magnitude` 快,因为不用开平方根,这里只是判断"有没有输入",不需要精确长度,用平方版本更省。

**`PlayerMoveState.cs`**

```csharp
using Game.Entities;

namespace Game.StateMachines.Player
{
    public class PlayerMoveState : IState
    {
        private readonly PlayerController player;
        private readonly StateMachine stateMachine;

        public PlayerMoveState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter() { }

        public void Tick()
        {
            if (player.ConsumeDashPressed() && player.CanDash)
            {
                stateMachine.ChangeState(player.DashState);
                return;
            }

            if (player.ConsumeAttackPressed())
            {
                stateMachine.ChangeState(player.AttackState);
                return;
            }

            if (player.MoveInput.sqrMagnitude < 0.01f)
            {
                stateMachine.ChangeState(player.IdleState);
            }
        }

        public void FixedTick()
        {
            player.Move(player.MoveSpeed);
        }

        public void Exit() { }
    }
}
```

和 `Idle` 几乎一样,唯一区别是 `FixedTick()` 里真的调用了 `player.Move()`,以及判断方向反过来(没输入了就回 `Idle`)。

**`PlayerDashState.cs`**

```csharp
using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Player
{
    public class PlayerDashState : IState
    {
        private readonly PlayerController player;
        private readonly StateMachine stateMachine;
        private Vector2 dashDirection;
        private float timer;

        public PlayerDashState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            timer = 0f;
            dashDirection = player.MoveInput.sqrMagnitude > 0.01f
                ? player.MoveInput
                : (Vector2)player.transform.right;

            player.health.SetInvincible(player.DashDuration);
            player.StartDashCooldown();
        }

        public void Tick()
        {
            timer += Time.deltaTime;
            if (timer >= player.DashDuration)
            {
                stateMachine.ChangeState(player.MoveInput.sqrMagnitude > 0.01f ? player.MoveState : player.IdleState);
            }
        }

        public void FixedTick()
        {
            player.Rb.MovePosition(player.Rb.position + dashDirection * player.DashSpeed * Time.fixedDeltaTime);
        }

        public void Exit() { }
    }
}
```

`dashDirection` 在 `Enter()` 时**只计算一次**并存起来,`FixedTick()` 里反复使用这个存住的值,而不是每帧重新读 `player.MoveInput`——这样闪避途中即使松开/换方向键,角色也会朝着"按下 Shift 那一刻的方向"冲出去一条直线,不会中途拐弯,手感更稳定。没按方向键时退化成朝当前朝向(`transform.right`)闪避。`Enter()` 里顺手调用 `SetInvincible` 和 `StartDashCooldown`,这样"进入闪避状态"和"获得无敌 + 开始冷却"永远是同一时刻发生,不用担心漏调。

**`PlayerAttackState.cs`**

```csharp
using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Player
{
    public class PlayerAttackState : IState
    {
        private readonly PlayerController player;
        private readonly StateMachine stateMachine;
        private float timer;
        private Color originalColor;

        public PlayerAttackState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            timer = 0f;
            originalColor = player.SpriteRenderer.color;
            player.SpriteRenderer.color = Color.yellow;

            PerformHit();
        }

        public void Tick()
        {
            timer += Time.deltaTime;
            if (timer >= player.AttackDuration)
            {
                stateMachine.ChangeState(player.MoveInput.sqrMagnitude > 0.01f ? player.MoveState : player.IdleState);
            }
        }

        public void FixedTick() { }

        public void Exit()
        {
            player.SpriteRenderer.color = originalColor;
        }

        private void PerformHit()
        {
            Vector2 origin = (Vector2)player.transform.position + (Vector2)player.transform.right * player.AttackRange;
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, player.AttackRange);

            foreach (Collider2D hit in hits)
            {
                if (!hit.CompareTag("Enemy")) continue;
                if (hit.TryGetComponent(out Health health))
                {
                    health.TakeDamage(player.AttackDamage);
                }
            }
        }
    }
}
```

`PerformHit()` 只在 `Enter()` 里调用一次——近战攻击是"挥一下"的瞬间判定,不是攻击状态期间每帧都在打。`Physics2D.OverlapCircleAll(圆心, 半径)` 返回这个圆形范围内**所有**碰撞体(不管是不是 Trigger),挨个检查 Tag 是不是 `Enemy`,是的话就找它身上的 `Health` 扣血。圆心 = 玩家位置 + 面朝方向 × `attackRange`,也就是"玩家前方一段距离"。

**`PlayerHurtState.cs`**

```csharp
using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Player
{
    public class PlayerHurtState : IState
    {
        private const float FlashInterval = 0.08f;

        private readonly PlayerController player;
        private readonly StateMachine stateMachine;
        private float timer;
        private Color originalColor;

        public PlayerHurtState(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            timer = 0f;
            originalColor = player.SpriteRenderer.color;
            player.health.SetInvincible(player.HurtInvincibleDuration);
        }

        public void Tick()
        {
            timer += Time.deltaTime;

            bool flashOn = Mathf.FloorToInt(timer / FlashInterval) % 2 == 0;
            player.SpriteRenderer.color = flashOn ? Color.white : originalColor;

            if (timer >= player.HurtDuration)
            {
                stateMachine.ChangeState(player.MoveInput.sqrMagnitude > 0.01f ? player.MoveState : player.IdleState);
            }
        }

        public void FixedTick() { }

        public void Exit()
        {
            player.SpriteRenderer.color = originalColor;
        }
    }
}
```

这个状态是怎么进入的,回头看 `PlayerController.OnDamaged`——`Health.Damaged` 事件一触发就切到这里,不需要玩家自己判断"我是不是刚挨打"。`Mathf.FloorToInt(timer / FlashInterval) % 2 == 0` 是"每隔 `FlashInterval` 秒切换一次颜色"的常见写法:把经过的时间除以间隔取整,得到一个每隔一段时间才会变化的整数,再用 `% 2` 判断奇偶,奇偶交替就是"闪烁"。`Exit()` 里强制把颜色改回 `originalColor`,是为了防止"闪烁到一半状态被打断切走"导致颜色卡在白色上出不来——**任何会临时改变外观/数值的状态,都应该在 `Exit()` 里负责把东西还原**,这是这一周除了状态机本身之外最值得记住的一条经验。

### 3.7 敌人状态类(`Assets/Scripts/StateMachines/Enemy/`)

**`EnemyPatrolState.cs`**

```csharp
using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
    public class EnemyPatrolState : IState
    {
        private const float WaitDuration = 1f;

        private readonly EnemyController enemy;
        private readonly StateMachine stateMachine;
        private Vector2 wanderTarget;
        private float waitTimer;

        public EnemyPatrolState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            enemy.SpriteRenderer.color = Color.white;
            PickNewTarget();
        }

        public void Tick()
        {
            if (enemy.DistanceToPlayer() <= enemy.DetectionRange)
            {
                stateMachine.ChangeState(enemy.ChaseState);
                return;
            }

            if (Vector2.Distance(enemy.Rb.position, wanderTarget) < 0.1f)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= WaitDuration)
                {
                    PickNewTarget();
                }
            }
        }

        public void FixedTick()
        {
            enemy.MoveTowards(wanderTarget, enemy.PatrolSpeed);
        }

        public void Exit() { }

        private void PickNewTarget()
        {
            Vector2 offset = Random.insideUnitCircle * enemy.PatrolRadius;
            wanderTarget = enemy.SpawnPosition + offset;
            waitTimer = 0f;
        }
    }
}
```

`Random.insideUnitCircle` 返回一个"单位圆内的随机点"(到圆心距离 ≤ 1 的随机 `Vector2`),乘上 `patrolRadius` 就变成"以出生点为圆心、`patrolRadius` 为半径的圆内随机一点"。走到目标点附近(< 0.1)之后不会立刻选下一个点,而是先等 `WaitDuration` 秒——不然敌人会一直无缝游走,停不下来,加个短暂停顿更像"巡逻"的样子。

**`EnemyChaseState.cs`**

```csharp
using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
    public class EnemyChaseState : IState
    {
        private readonly EnemyController enemy;
        private readonly StateMachine stateMachine;

        public EnemyChaseState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            enemy.SpriteRenderer.color = new Color(1f, 0.6f, 0f);
        }

        public void Tick()
        {
            float distance = enemy.DistanceToPlayer();

            if (distance <= enemy.AttackRange)
            {
                stateMachine.ChangeState(enemy.AttackState);
                return;
            }

            if (distance > enemy.LoseSightRange)
            {
                stateMachine.ChangeState(enemy.PatrolState);
            }
        }

        public void FixedTick()
        {
            if (enemy.Player != null)
            {
                enemy.MoveTowards(enemy.Player.position, enemy.ChaseSpeed);
            }
        }

        public void Exit() { }
    }
}
```

`detectionRange`(发现玩家的距离,4)和 `loseSightRange`(丢失玩家的距离,6)故意设成两个不同的数值,而不是共用一个——如果只用一个阈值,玩家刚好站在临界距离上下抖动时,敌人会在 `Patrol`/`Chase` 之间来回疯狂切换(专业说法叫"抖动"/state flapping)。两个不同的阈值中间留一段"缓冲区",进只看小阈值、出只看大阈值,能有效避免这个问题——这是状态机设计里一个很常见但容易被忽略的细节。

**`EnemyAttackState.cs`**

```csharp
using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
    public class EnemyAttackState : IState
    {
        private readonly EnemyController enemy;
        private readonly StateMachine stateMachine;
        private float cooldownTimer;

        public EnemyAttackState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            enemy.SpriteRenderer.color = Color.red;
            cooldownTimer = 0f;
        }

        public void Tick()
        {
            float distance = enemy.DistanceToPlayer();

            if (distance > enemy.AttackRange * 1.2f)
            {
                stateMachine.ChangeState(enemy.ChaseState);
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                TryHitPlayer();
                cooldownTimer = enemy.AttackCooldown;
            }
        }

        public void FixedTick() { }

        public void Exit() { }

        private void TryHitPlayer()
        {
            if (enemy.Player != null && enemy.Player.TryGetComponent(out Health health))
            {
                health.TakeDamage(enemy.ContactDamage);
            }
        }
    }
}
```

离开 `Attack` 回 `Chase` 的判断用的是 `attackRange * 1.2`,比进入时的 `attackRange` 宽松一点,原理和上面 `Chase`/`Patrol` 的双阈值一样,防止玩家贴在攻击范围边缘时来回抖动。`Enter()` 里把 `cooldownTimer` 清零,保证"刚进入攻击状态"一定会立刻打一下,而不是要先攒够一次冷却时间才打第一下。

**`EnemyDeadState.cs`**

```csharp
using Game.Entities;
using UnityEngine;

namespace Game.StateMachines.Enemy
{
    public class EnemyDeadState : IState
    {
        private readonly EnemyController enemy;

        public EnemyDeadState(EnemyController enemy, StateMachine stateMachine)
        {
            this.enemy = enemy;
        }

        public void Enter()
        {
            enemy.SpriteRenderer.color = Color.gray;
            enemy.Rb.linearVelocity = Vector2.zero;
        }

        public void Tick() { }

        public void FixedTick() { }

        public void Exit() { }
    }
}
```

这个状态看起来"什么都没干成"——因为 `Health.Die()` 在广播 `Died` 事件之后紧接着就把 `gameObject.SetActive(false)` 了,`Dead` 状态的 `Enter()` 跑完的瞬间敌人就已经被隐藏,`Tick`/`FixedTick` 根本没机会执行(Unity 不会给已禁用的物体调用生命周期方法)。这不是 bug,是这周故意先把"死亡"这个状态占位占好,**死亡动画、掉落物、经验值这些留到有美术资源/EventBus 之后再往这个状态里加**,现在只是先把 `Rigidbody2D` 的速度清零(防止死亡瞬间还带着惯性)。

### 3.8 `Assets/Scripts/Weapons/PlayerShooter.cs`(修改)

```csharp
using Game.Core;
using Game.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Weapons
{
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private ObjectPool bulletPool;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireCooldown = 0.25f;

        private PlayerController playerController;
        private float cooldownTimer;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            bool firePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool canFire = playerController == null || playerController.CanAct;

            if (firePressed && canFire && cooldownTimer <= 0f)
            {
                Fire();
                cooldownTimer = fireCooldown;
            }
        }

        private void Fire()
        {
            bulletPool.Get(firePoint.position, firePoint.rotation);
        }
    }
}
```

只加了 `playerController.CanAct` 这一个判断条件。`playerController == null` 的判断是个保险(理论上 `PlayerShooter` 都是和 `PlayerController` 挂在同一个物体上,`GetComponent` 不该失败,但万一以后 `PlayerShooter` 被挪到别的物体上测试,这里不会直接报空引用异常,而是"没有 PlayerController 就默认允许开火")。

---

## 4. Unity 编辑器操作

这周**不需要新建任何 GameObject 或预制体**,场景结构和第 1 周完全一样。需要做的只有:

1. 在 `Assets/Scripts/` 下新建两个文件夹:`StateMachines`,以及它里面的 `Player` 和 `Enemy` 子文件夹。
2. 按上面的清单在对应路径下新建/替换 `.cs` 文件。
3. 回 Unity 等它编译完(右下角转圈转完),Console 里如果有红色报错,对照报错信息先自己看一眼是不是敲错了字,不确定再贴给我。
4. 选中 `Player`,检查 Inspector 里 `Player Controller` 组件——因为这次很多字段改了名字(比如新增了 `Dash Speed`、`Attack Range` 这些),它们会是脚本里写的默认值,不需要手动填,能跑就行,想调手感再改。
5. 选中场景里的 `Enemy`(或者 `Assets/Prefabs/Enemy` 预制体),同理检查一遍 `Enemy Controller` 组件,字段从"Move Speed / Contact Damage / Damage Cooldown"变成了"Patrol Speed / Patrol Radius / Chase Speed / Detection Range / Lose Sight Range / Attack Range / Contact Damage / Attack Cooldown",默认值都能直接跑。

保存场景,点 Play 测试。

---

## 5. 验收 checklist

- [x] 站着不动 5 秒以上,角色保持白色(不是被挂了别的颜色卡住)
- [x] 按 WASD 正常移动,和第 1 周手感一致
- [x] 按一下 `Shift`,角色朝当前移动方向(或朝向)冲出一小段距离;冲刺途中被敌人碰到不会掉血(无敌帧生效);冲刺有冷却,连续按 `Shift` 不会连续冲刺
- [x] 按一下鼠标右键,角色短暂变黄,前方一圈内的敌人掉血(可以站远一点确认"没在范围内就打不到")
- [x] 攻击/闪避期间按住鼠标左键,不会打断当前动作去开枪(`CanAct` 生效)
- [x] 被敌人碰到会红色闪烁一小段时间(初版是闪白、看不见,已改红,见第 5.2 节),这段时间内连续被撞不会连续掉血(受击无敌帧生效)
- [x] 远远站着不动,敌人保持白色在出生点附近晃悠(巡逻);走近它,颜色变橙色并开始追你;贴上后变红色并周期性扣你血
- [x] 被追击的敌人拉开距离跑远,它会变回白色回去巡逻
- [x] 把敌人打死(子弹或近战都行),它变灰后消失,不报错

**过程中踩过的坑**(用户对照参考代码手动敲写时引入,记录下来方便以后排查同类问题):

- `PlayerController.cs`/`EnemyController.cs`:`CanAct` 一度写成 `stateMachine == IdleState`(拿状态机本身和某个具体状态比较,类型不兼容,编译报错);`StartDashCooldown()` 写成 `dashCooldownTimer -= dashCooldown`(应为 `=`,减法会让冷却完全失效);`RotateTowardsMouse()` 的空判断写成 `Mouse.current == null && mainCamera == null`(应为 `||`,否则只有两者都为空才会拦截,单个为空时仍会继续执行导致空引用)。
- `Health`/`PlayerController`/`EnemyController` 上代表血量组件的公开属性,用户主动选择用小写 `health` 而不是 C# 惯例的 `Health`——这是有意为之的风格选择,不是笔误,已同步更新到所有引用它的文件和 `CLAUDE.md` 的命名约定里。
- `PlayerShooter.cs`:算出了 `canFire`(是否处于 Idle/Move,可以开火)但没有写进 `if` 判断条件里,导致"攻击/闪避/受击时不能开枪"这条本周的限制完全没生效,补上 `&& canFire` 后修复。
- 多个新文件里出现了转录时带出来的多余 `using`(`System.Data`、`JetBrains.Annotations`、`UnityEngine.UIElements`、`UnityEditor.ShaderGraph`),其中 `UnityEditor.ShaderGraph` 和第 1 周的 `UnityEditor.Callbacks` 一样,是 Editor-only 命名空间,不删的话正式打包时会编译报错。

**验收结果**:闪避 + 无敌帧、近战攻击范围判定、受击闪红 + 无敌帧、敌人 Patrol/Chase/Attack/Dead 四状态切换(含颜色反馈)、状态期间禁止开枪,均已在 Play 模式下测试通过。第 2 周正式完成。

### 5.2 验收后又发现并修复的两个问题

上面的 checklist 全过之后,又在实际游玩中发现了两个和「物理/渲染」有关的 bug,都和第 1 周留下的设定有关。记录如下。

**问题一:静止时被敌人撞到后,角色获得一个恒定速度、一直漂,停不下来**

- 现象:站着不动被怪撞一下,有概率被推着匀速滑走,按方向键也拽不回来。
- 根因:Player 的 `Rigidbody2D` 是 **Dynamic** 且 **Linear Damping = 0**(速度不会自己衰减);移动用的是 `Rb.MovePosition(...)`,而 `MovePosition` **只改位置、不会清掉刚体自身的 `linearVelocity`**。敌人(也是 Dynamic)撞上来时,物理碰撞会塞给玩家一个 `linearVelocity`,这个速度既不衰减、`Idle`/`Hurt` 状态的 `FixedTick` 又是空的、没人清它 → 于是一直漂。
- 修复:保留 `MovePosition` 的移动方式,只在 `PlayerController.FixedUpdate()` 的**开头加一行 `Rb.linearVelocity = Vector2.zero;`**,再跑 `stateMachine.FixedTick()`。这样每一物理帧先把碰撞塞进来的残留速度清零,`MovePosition` 再负责这一帧该有的位移——碰撞检测(互相不重叠)照常生效,但「被撞飞的残留速度」被彻底消掉。改动只有 1 行,没有改成 velocity 驱动的移动系统。

  ```csharp
  private void FixedUpdate()
  {
      Rb.linearVelocity = Vector2.zero;   // 新增:清掉物理碰撞残留的速度
      stateMachine.FixedTick();
  }
  ```

  > 记这条坑:**只要一个 Dynamic 刚体的位移是用 `MovePosition` 脚本控制的,就要留意物理碰撞会给它累积 `linearVelocity`**——要么每帧清零(本项目的做法),要么冻结相关约束,要么干脆改用 velocity 驱动移动。三选一,别让两套机制打架。

**问题二:被敌人碰到后看不到「白色闪烁」**

- 现象:受击的无敌帧、扣血都正常,但完全看不到闪烁效果。
- 根因:角色的 `SpriteRenderer` 颜色本来就是白色(第 1 周用 `2D Object > Sprites > Square` 建的默认精灵就是纯白)。`PlayerHurtState` 的闪烁是在 `Color.white` 和 `originalColor` 之间切换,而 `originalColor` 记录的正是那个白色 → **白配白,肉眼看不出任何变化**。逻辑一直在正确执行,只是没有视觉差异。
- 修复:把 `PlayerHurtState.Tick()` 里的闪烁色从 `Color.white` 改成 `Color.red`(闪红),和白色底有明显对比,受击时就是「红/白/红/白」交替,很直观。改动 1 行。

  ```csharp
  player.SpriteRenderer.color = flashOn ? Color.red : originalColor;   // 原来是 Color.white
  ```

这两处修复都由用户手动改进 `Assets/`,改完复测:静止被撞不再漂移、松手即停;被撞瞬间可见红白闪烁 + 无敌帧期间连撞不掉血。第 2 周至此彻底收尾。

---

## 6. 常见问题排查

| 现象 | 可能原因 | 排查方法 |
|---|---|---|
| Console 报 `找不到类型或命名空间 PlayerIdleState` 之类 | `StateMachines/Player`(或 `Enemy`)文件夹没建,或者文件建错了位置 | 确认文件夹层级和 `Reference/Scripts/` 里完全一致 |
| 一按 Shift 就报 `NullReferenceException`,提示和 `SpriteRenderer` 有关 | Player/Enemy 身上没有 `SpriteRenderer` 组件 | 正常情况下第 1 周用 `GameObject > 2D Object > Sprites > Square/Triangle` 创建的物体自带这个组件;如果被误删了,重新 `Add Component` 加回来 |
| 闪避/攻击后颜色卡住,一直是黄色或白色不变回来 | 状态的 `Exit()` 没有正确还原颜色,或者中途被报错打断了 | 先看 Console 有没有报错;确认 `Exit()` 里的还原代码有没有敲漏 |
| 敌人一直不追我 | `detectionRange` 太小,或者站的距离超出了范围 | 走近一点测试,或者临时把 Inspector 里的 `Detection Range` 调大一点 |
| 敌人追上来之后卡在原地不攻击也不掉血 | `Player` 引用没拿到(`EnemyController.Start()` 里 `FindGameObjectWithTag("Player")` 没找到) | 确认 Player 物体的 Tag 是不是 `Player`(内置 Tag,注意大小写) |
| 打死敌人后 Console 报错,提示访问已销毁物体 | 某个还在引用这个敌人的脚本,在它 `SetActive(false)` 之后又调用了它的方法 | 目前设计下不应该出现;如果出现了,把完整报错贴给我 |

---

## 7. 下周预告:第 3 周 - 事件系统与数据驱动武器

会正式引入 EventBus(这周 `Health` 里那两个 C# 事件就是它的雏形),把 UI 血条、子弹数这些跨模块的通知都改走事件;武器会用 ScriptableObject 数据 + 策略模式重做,远程武器(子弹)和这周的近战原型会被统一到同一套 `IWeaponStrategy` 接口下,支持切换武器。
