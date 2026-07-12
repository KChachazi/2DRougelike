# 第 3 周:事件系统与数据驱动武器

> 这周有两条主线:**① EventBus(事件总线)**——把"血量变了""弹药变了""换武器了"这类跨模块通知,从"你调我、我调你"的硬引用,改成"广播 / 订阅";**② 数据驱动武器**——用 ScriptableObject 存武器数值、用策略模式(IWeaponStrategy)存开火行为,把第 2 周散落的远程射击和近战原型统一成一套可切换的武器系统。
>
> 这周比第 2 周大,所以**拆成 4 个步骤,每步做完都能编译、能 Play 验收一个小功能**,别一口气全写完再测——那样出了 bug 很难定位。每一步末尾都有一个 ✅ 验收清单,过了再进下一步。
>
> 照旧:参考实现都在 `Reference/Scripts/...`(与 `Assets/` 同级,不进版本库),你自己在 `Assets/Scripts/...` 对应路径创建/替换。不懂随时问。

## 本周目标(对齐 README 六周计划)

- **EventBus**:轻量事件总线,UI 血条 / 弹药数通过订阅事件更新,不再直接引用 Player。
- **武器 ScriptableObject**:伤害 / 冷却 / 弹夹 / 类型都写在数据资产里,改数值不改代码。
- **策略模式武器切换**:手枪 / 步枪 / 近战统一到 `IWeaponStrategy`,数字键 1/2/3 切换。
- **弹药限制 + 补给拾取**:远程武器有弹夹、打空不能射,地上捡补给回弹药;近战无限。

完成后:左上角有血条(挨打会掉)、右上角显示"当前武器 + 弹药数";按 `1`/`2`/`3` 切手枪/步枪/近战,三者伤害射速手感不同;左键开火消耗弹药,打空了要去捡地上的补给包;近战显示 ∞、挥砍时角色变黄且期间不能移动/开火。第 1、2 周的移动、闪避、受击、敌人 AI 全部保留。

---

## 0. 先建目录 + 认识三个新概念

### 0.1 新建目录

这周会用到两个还不存在的目录,先在 `Assets/Scripts/` 下建好:

- `Assets/Scripts/UI/` —— 放 `HealthBarUI`、`AmmoUI`(命名空间 `Game.UI`)
- `Assets/Data/` —— **不是脚本目录**,是放武器 ScriptableObject 资产(`.asset` 文件)的地方,在 `Assets/` 下新建 `Data` 文件夹即可

`Assets/Scripts/Core/`、`Assets/Scripts/Weapons/` 已经有了,新文件直接往里加。

### 0.2 EventBus 是什么、为什么要它

第 2 周的 `Health` 已经有 `Damaged`/`Died` 两个 C# 事件了,那是"**局部**观察者"——只有同一个物体上的 `PlayerController` 在听。但"血条 UI"这种东西,和 Player 根本不在一个物体上,甚至不该认识 Player。如果让血条 `GetComponent<Health>()` 去读血量,就变成 UI **硬引用**了游戏逻辑,以后血量来源一变、或者想加第二个血条,都要改 UI 代码——这正是 `CLAUDE.md` 架构表里写的"模块间禁止相互持有硬引用"。

**EventBus 的思路**:发布者(Health/武器)只管朝"空中"喊一声"血变成 80/100 了",不关心谁在听;订阅者(UI)只管说"我要听血量变化",不关心是谁喊的。两边通过一个中间站(EventBus)传话,**互相不持有对方的引用**。加一个新 UI、换一套血量系统,只要事件不变,双方都不用改。

### 0.3 ScriptableObject(SO)是什么

`ScriptableObject` 是 Unity 里一种"**不挂在 GameObject 上、以资产文件形式存在**"的数据容器。你可以把它理解成"一张可以在 Project 窗口里双击编辑的 Excel 表"。我们用它存武器数值(伤害/冷却/弹夹),好处:

- 改数值不用改代码、不用进场景,美术/策划也能调;
- "新增一把武器" = 右键 Create 一个新资产 + 填几个数字,不写一行代码;
- 同一份数据可以被多个地方共享引用,不会散落复制。

### 0.4 策略模式(Strategy)是什么

"数据"解决了"这把武器多少伤害",但"**这把武器怎么开火**"(发子弹?还是画个圈砍?)是**行为**,不适合塞进数据里。策略模式就是把"每一种行为封装成一个类,让它们实现同一个接口",运行时想用哪种就换哪种。

我们定义 `IWeaponStrategy`,两个实现:`RangedWeaponStrategy`(发子弹)、`MeleeWeaponStrategy`(画圈判定)。武器数据(SO)里存一个 `WeaponType` 枚举,`WeaponController` 根据类型挑对应策略执行。以后加"霰弹枪""激光"只是再写一个策略类,不动 `WeaponController` 主体——这就是架构表里"新增武器只加数据和一个策略实现,不改主体逻辑"。

---

## 步骤 1:EventBus + 血条 UI

**这一步做什么**:搭好事件总线,让 `Health` 血量变化能广播出去,做一个左上角血条订阅它。做完你能看到:挨打时血条实时下降。

> ⚠️ 这一步会**顺带拆掉第 2 周"右键近战"的旧实现**(因为近战马上要改成武器,由步骤 2/3 以新形式回归)。所以步骤 1 做完到步骤 3 之前,**右键不再有近战**,这是正常的中间状态,别以为是 bug。左键远程射击(旧的 `PlayerShooter`)在步骤 2 之前仍然可用。

### 1.1 新建 `Assets/Scripts/Core/EventBus.cs`

参考实现:`Reference/Scripts/Core/EventBus.cs`。

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public static class EventBus
    {
        // key = 事件类型, value = 该类型所有订阅者组成的委托链
        private static readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handlers.TryGetValue(typeof(T), out Delegate existing))
                handlers[typeof(T)] = (Action<T>)existing + handler;
            else
                handlers[typeof(T)] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (!handlers.TryGetValue(typeof(T), out Delegate existing)) return;

            Action<T> updated = (Action<T>)existing - handler;
            if (updated == null)
                handlers.Remove(typeof(T));
            else
                handlers[typeof(T)] = updated;
        }

        public static void Publish<T>(T evt)
        {
            if (handlers.TryGetValue(typeof(T), out Delegate existing))
                ((Action<T>)existing)?.Invoke(evt);
        }

        public static void Clear() => handlers.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => handlers.Clear();
    }
}
```

讲解:
- `static class` + 静态字典:全局唯一,任何地方 `EventBus.Publish(...)` 都能用,不用在场景里挂物体。
- 用 `Dictionary<Type, Delegate>`:key 是事件的类型(比如 `AmmoChangedEvent`),value 是所有订阅这个类型的方法串起来的委托链。`Subscribe` 就是 `+ handler`(往链上加一个),`Unsubscribe` 就是 `- handler`。
- `<T>` 泛型保证**类型安全**:订阅 `AmmoChangedEvent` 的方法,只会收到 `AmmoChangedEvent`,不会串味。
- 最后那个 `ResetStatics`:静态字段在"关闭 domain reload 的快速进入 Play"模式下不会自动清空,上次运行残留的订阅者会串场导致诡异 bug。`[RuntimeInitializeOnLoadMethod]` 让每次进入 Play 前自动清一次,属于"防坑"代码,现在不深究,记住 static 单例都该考虑这个问题即可。

### 1.2 新建 `Assets/Scripts/Core/GameEvents.cs`

参考实现:`Reference/Scripts/Core/GameEvents.cs`。这里集中定义本周所有事件的数据结构。

```csharp
namespace Game.Core
{
    public readonly struct PlayerHealthChangedEvent
    {
        public readonly int Current;
        public readonly int Max;
        public PlayerHealthChangedEvent(int current, int max) { Current = current; Max = max; }
    }

    public readonly struct AmmoChangedEvent
    {
        public readonly int Current;
        public readonly int Max; // 约定:-1 表示无限弹药(近战)
        public AmmoChangedEvent(int current, int max) { Current = current; Max = max; }
    }

    public readonly struct WeaponChangedEvent
    {
        public readonly string WeaponName;
        public WeaponChangedEvent(string weaponName) { WeaponName = weaponName; }
    }
}
```

讲解:每个事件是 `readonly struct`。用 `struct`(值类型)而不是 `class`,是因为事件会频繁发布,`struct` 不产生堆分配、不给 GC 添麻烦;`readonly` 表示事件数据一旦创建不可改,订阅者只能读。要加新通知,就在这里加一个新 `struct`。

### 1.3 替换 `Assets/Scripts/Entities/Health.cs`

参考实现:`Reference/Scripts/Entities/Health.cs`。相比第 2 周,只加了 **`Max` 属性** 和 **`HealthChanged` 事件**,并在 `TakeDamage` 里触发它。

```csharp
using System;
using UnityEngine;

namespace Game.Entities
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        public int Current { get; private set; }
        public int Max => maxHealth;              // 新增:UI 要用上限算百分比
        public bool isDead => Current <= 0;
        public bool isInvincible { get; private set; }

        public event Action<int> Damaged;
        public event Action Died;
        public event Action<int, int> HealthChanged;   // 新增:血量变化(current, max)

        private float invincibleTimer;

        private void Awake() { Current = maxHealth; }

        private void Update()
        {
            if (!isInvincible) return;
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f) isInvincible = false;
        }

        public void SetInvincible(float duration)
        {
            isInvincible = true;
            invincibleTimer = duration;
        }

        public void TakeDamage(int amount)
        {
            if (isDead || isInvincible) return;
            Current = Mathf.Max(0, Current - amount);
            Damaged?.Invoke(amount);
            HealthChanged?.Invoke(Current, maxHealth);   // 新增:扣完血广播一次
            if (isDead) Die();
        }

        private void Die()
        {
            Died?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
```

> 注意 `Health` 仍然是**通用组件**——它只喊"我血变成 X/Y 了",不认识"玩家""UI"。"把玩家的血量变化转发给全局 UI"这一步,由玩家自己(`PlayerController`)做,见下。这样敌人也能复用 `Health`,不会莫名其妙给 UI 发消息。

### 1.4 替换 `Assets/Scripts/Entities/PlayerController.cs`

参考实现:`Reference/Scripts/Entities/PlayerController.cs`。这次改动:

1. 加 `using Game.Core;`;
2. `OnEnable/OnDisable` 里订阅/退订 `health.HealthChanged`,在回调里把它转成全局 `PlayerHealthChangedEvent` 发布;
3. `Start` 里额外广播一次初始血量(否则 UI 要等你第一次挨打才更新);
4. **删掉**第 2 周的近战字段 `attackRange`/`attackDamage` 和对应属性、以及 `ConsumeAttackPressed()`(近战改由武器系统管);
5. 新增 `TriggerAttack()`,供 `WeaponController` 在近战开火时切到 `AttackState` 做表现。

```csharp
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
        [SerializeField] private float attackDuration = 0.25f;   // 只剩表现时长,伤害/范围搬到 WeaponData

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
            EventBus.Publish(new PlayerHealthChangedEvent(health.Current, health.Max));   // 广播初始满血
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
            Rb.linearVelocity = Vector2.zero;   // 清掉物理碰撞残留速度(week2 第 5.2 节)
            stateMachine.FixedTick();
        }

        public void Move(float speed)
        {
            Rb.MovePosition(Rb.position + MoveInput * speed * Time.fixedDeltaTime);
        }

        public void StartDashCooldown() => dashCooldownTimer = dashCooldown;

        public bool ConsumeDashPressed()
        {
            return Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
        }

        // 供 WeaponController 在近战开火时调用:切到 AttackState 播放"变黄 + 阻断"表现
        public void TriggerAttack() => stateMachine.ChangeState(AttackState);

        private void ReadMoveInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) { MoveInput = Vector2.zero; return; }
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

        private void OnDamaged(int amount) => stateMachine.ChangeState(HurtState);

        private void OnHealthChanged(int current, int max)
        {
            EventBus.Publish(new PlayerHealthChangedEvent(current, max));
        }
    }
}
```

> **订阅放 `OnEnable`、初始广播放 `Start`,顺序很关键**:Unity 保证所有物体的 `OnEnable` 都跑在所有 `Start` 之前。所以血条 UI 在 `OnEnable` 里先订好,Player 在 `Start` 里广播初始血量时,UI 一定已经在听了,不会漏掉第一帧的初始值。这是用 EventBus 时最容易踩的时序坑,记住这个搭配。

### 1.5 替换三个玩家状态类(删掉旧近战输入/判定)

因为上面 `PlayerController` 删了 `ConsumeAttackPressed()` 和 `AttackRange/AttackDamage`,这三个引用它们的状态类必须同步替换,否则编译不过。参考实现在 `Reference/Scripts/StateMachines/Player/` 对应文件。

**`PlayerIdleState.cs`** 和 **`PlayerMoveState.cs`**:只是删掉中间那段 `if (player.ConsumeAttackPressed()) { ChangeState(AttackState); return; }`。以 `PlayerIdleState` 为例,改后 `Tick` 变成:

```csharp
public void Tick()
{
    if (player.ConsumeDashPressed() && player.CanDash)
    {
        stateMachine.ChangeState(player.DashState);
        return;
    }

    if (player.MoveInput.sqrMagnitude > 0.01f)
    {
        stateMachine.ChangeState(player.MoveState);
    }
}
```

`PlayerMoveState.Tick` 同理删掉近战分支(最后一个判断保持"没输入就回 Idle")。

**`PlayerAttackState.cs`**:删掉 `PerformHit()` 整个方法和 `Enter()` 里对它的调用——伤害判定搬去 `MeleeWeaponStrategy` 了,这个状态现在只负责"变黄 + 计时 + 到点回 Move/Idle":

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
    }
}
```

### 1.6 新建 `Assets/Scripts/UI/HealthBarUI.cs`

参考实现:`Reference/Scripts/UI/HealthBarUI.cs`。

```csharp
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;   // Image Type 设为 Filled

        private void OnEnable()  => EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        private void OnDisable() => EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);

        private void OnHealthChanged(PlayerHealthChangedEvent e)
        {
            if (fillImage == null || e.Max <= 0) return;
            fillImage.fillAmount = (float)e.Current / e.Max;
        }
    }
}
```

讲解:整个类只做一件事——听 `PlayerHealthChangedEvent`,把 `current/max` 换算成 `Image.fillAmount`(0~1)。它**没有任何对 Player / Health 的引用**,这就是 EventBus 解耦的直接体现。`(float)e.Current / e.Max` 里的 `(float)` 别漏,否则整数相除会得到 0 或 1(整数除法截断)。

### 1.7 Unity 编辑器操作:搭血条

1. **建 Canvas**:Hierarchy 右键 `UI > Canvas`。Unity 会自动创建一个 `Canvas`(Render Mode 默认 `Screen Space - Overlay`,正合适)和一个 `EventSystem`(留着别删)。
2. **建血条底板**:右键刚建的 `Canvas` → `UI > Image`,改名 `HealthBar_BG`。在 Inspector 里:
   - 找到 `Rect Transform` 左上角的锚点方块,按住 `Alt+Shift` 点左上角那个预设,把它锚定并对齐到屏幕左上角;
   - 设 `Pos X = 120`、`Pos Y = -40`、`Width = 200`、`Height = 24`;
   - `Image` 组件的 `Color` 调成深灰(当底色)。
3. **建血条填充**:右键 `HealthBar_BG` → `UI > Image`,改名 `HealthBar_Fill`。
   - `Rect Transform` 点锚点预设里的 `stretch/stretch`(右下角那个),然后把 `Left/Right/Top/Bottom` 都设为 0,让它铺满底板;
   - `Image` 的 `Color` 调成红色或绿色;
   - ⚠️ **先设 `Source Image`,否则下一条的 `Image Type` 根本不会出现**:点 `Source Image` 右边的小圆圈,搜 `UISprite`(Unity 内置 UI 图)并选中。搜不到就把选择窗口切到 `All` 标签页再搜,或者随便选一个白色 `Square` sprite 也行。
     > 这是 Unity 的隐藏字段行为:`Source Image` 为 `None` 时,`Image` 组件只显示 Color / Material / Raycast Target,**不显示 `Image Type`**。给了图它才冒出来。
   - **关键**:`Image` 组件的 `Image Type` 改成 `Filled`,`Fill Method` 选 `Horizontal`,`Fill Origin` 选 `Left`,`Fill Amount` 拖到 1。
4. **挂脚本**:选中 `HealthBar_BG`(或 Canvas 都行)→ `Add Component` → `HealthBarUI`。把 `HealthBar_Fill` 拖到脚本的 `Fill Image` 槽里。
5. 保存场景。

### ✅ 步骤 1 验收

- [ ] 进入 Play,左上角血条是满的。
- [ ] 走到敌人旁边故意挨打,血条**实时下降**(每次掉血一截),且和受击闪红同步。
- [ ] Console 没有报错。
- [ ] (中间态确认)右键此时没有近战——正常,步骤 3 会以武器形式回来。

过了再进步骤 2。

---

## 步骤 2:数据驱动武器系统(远程先跑通)

**这一步做什么**:建立武器数据(SO)+ 策略 + `WeaponController`,把左键射击接管过来,支持数字键切换。这一步先只配**手枪、步枪**两把远程武器验收;近战武器脚本也一并写好,但放到步骤 3 再配数据、单独验收。

### 2.1 新建武器系统脚本

一次性新建这 5 个文件(参考实现都在 `Reference/Scripts/Weapons/`),它们互相引用,建齐了才编译得过:

**`WeaponData.cs`**(数据 SO):

```csharp
using UnityEngine;

namespace Game.Weapons
{
    public enum WeaponType { Ranged, Melee }

    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName = "Pistol";
        public WeaponType type = WeaponType.Ranged;
        public int damage = 10;
        public float cooldown = 0.25f;
        [Tooltip("弹夹容量;近战填 -1 表示无限")]
        public int maxAmmo = 30;
        [Tooltip("近战判定半径;远程忽略")]
        public float range = 1f;
    }
}
```

`[CreateAssetMenu(...)]` 这一行是关键:它让你能在 Project 窗口右键 `Create > Game > Weapon Data` 生成武器资产。

**`IWeaponStrategy.cs`**(行为接口):

```csharp
namespace Game.Weapons
{
    public interface IWeaponStrategy
    {
        void Fire(WeaponController controller, WeaponData data);
    }
}
```

**`RangedWeaponStrategy.cs`**(远程:发子弹):

```csharp
using UnityEngine;

namespace Game.Weapons
{
    public class RangedWeaponStrategy : IWeaponStrategy
    {
        public void Fire(WeaponController controller, WeaponData data)
        {
            GameObject bulletObj = controller.BulletPool.Get(
                controller.FirePoint.position, controller.FirePoint.rotation);
            if (bulletObj.TryGetComponent(out Bullet bullet))
                bullet.SetDamage(data.damage);
        }
    }
}
```

**`MeleeWeaponStrategy.cs`**(近战:画圈判定,逻辑就是从第 2 周 `PerformHit` 搬来的):

```csharp
using Game.Entities;
using UnityEngine;

namespace Game.Weapons
{
    public class MeleeWeaponStrategy : IWeaponStrategy
    {
        public void Fire(WeaponController controller, WeaponData data)
        {
            Vector2 origin = (Vector2)controller.transform.position
                           + (Vector2)controller.transform.right * data.range;
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, data.range);
            foreach (Collider2D hit in hits)
            {
                if (!hit.CompareTag("Enemy")) continue;
                if (hit.TryGetComponent(out Health health))
                    health.TakeDamage(data.damage);
            }
        }
    }
}
```

**`WeaponController.cs`**(核心:管当前武器/冷却/弹药/输入):

```csharp
using System.Collections.Generic;
using Game.Core;
using Game.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Weapons
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private ObjectPool bulletPool;
        [SerializeField] private Transform firePoint;
        [Tooltip("按顺序对应数字键 1/2/3;元素 0 是初始武器")]
        [SerializeField] private WeaponData[] weapons;

        public ObjectPool BulletPool => bulletPool;
        public Transform FirePoint => firePoint;

        private PlayerController playerController;
        private readonly Dictionary<WeaponType, IWeaponStrategy> strategies =
            new Dictionary<WeaponType, IWeaponStrategy>();

        private int currentIndex;
        private int[] currentAmmo;
        private float cooldownTimer;

        private WeaponData CurrentWeapon => weapons[currentIndex];

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            strategies[WeaponType.Ranged] = new RangedWeaponStrategy();
            strategies[WeaponType.Melee]  = new MeleeWeaponStrategy();

            currentAmmo = new int[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
                currentAmmo[i] = weapons[i].maxAmmo;
        }

        private void Start()
        {
            BroadcastWeapon();
            BroadcastAmmo();
        }

        private void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

            HandleSwitchInput();

            bool canAct = playerController == null || playerController.CanAct;
            bool firePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (firePressed && canAct && cooldownTimer <= 0f && HasAmmo())
                FireCurrentWeapon();
        }

        private void FireCurrentWeapon()
        {
            WeaponData data = CurrentWeapon;
            strategies[data.type].Fire(this, data);
            cooldownTimer = data.cooldown;

            if (data.maxAmmo >= 0)
            {
                currentAmmo[currentIndex]--;
                BroadcastAmmo();
            }

            if (data.type == WeaponType.Melee && playerController != null)
                playerController.TriggerAttack();
        }

        private void HandleSwitchInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) SwitchTo(0);
            else if (kb.digit2Key.wasPressedThisFrame) SwitchTo(1);
            else if (kb.digit3Key.wasPressedThisFrame) SwitchTo(2);
        }

        private void SwitchTo(int index)
        {
            if (index < 0 || index >= weapons.Length || index == currentIndex) return;
            currentIndex = index;
            cooldownTimer = 0f;
            BroadcastWeapon();
            BroadcastAmmo();
        }

        public void AddAmmo(int amount)
        {
            WeaponData data = CurrentWeapon;
            if (data.maxAmmo < 0) return;
            currentAmmo[currentIndex] = Mathf.Min(currentAmmo[currentIndex] + amount, data.maxAmmo);
            BroadcastAmmo();
        }

        private bool HasAmmo() => CurrentWeapon.maxAmmo < 0 || currentAmmo[currentIndex] > 0;

        private void BroadcastAmmo() =>
            EventBus.Publish(new AmmoChangedEvent(currentAmmo[currentIndex], CurrentWeapon.maxAmmo));

        private void BroadcastWeapon() =>
            EventBus.Publish(new WeaponChangedEvent(CurrentWeapon.weaponName));
    }
}
```

讲解要点:
- **策略只负责"这一下怎么打",冷却/弹药由 `WeaponController` 统一管**。所以策略是无状态的,每种 `new` 一个共用实例放进字典,按 `data.type` 取用。
- `currentAmmo` 是数组、每把武器各记一份,切枪不清零(切回来弹药还在)。
- 远程和近战都走左键 + `HasAmmo` + `cooldown`;区别在 `FireCurrentWeapon` 末尾:近战额外 `TriggerAttack()` 切到 `AttackState`(变黄 + 攻击期间 `CanAct` 为 false → 不能再开火/移动,自然形成挥砍节奏);远程不切状态,保持能边走边连发。
- UI 需要的信息全走 `EventBus.Publish`,`WeaponController` 不认识 UI。

### 2.2 替换 `Assets/Scripts/Weapons/Bullet.cs`

参考实现:`Reference/Scripts/Weapons/Bullet.cs`。只加一个方法,让武器数据能覆盖子弹伤害:

```csharp
// 在 Bullet 类里,ObjectPool/Pool 属性附近加:
public void SetDamage(int value) => damage = value;
```

其余不变。这样手枪子弹打 12、步枪子弹打 7,靠武器 SO 决定,不用做两种子弹预制体。

### 2.3 Unity 编辑器操作:建武器 SO + 挂 WeaponController

1. **建武器数据资产**:在 `Assets/Data/` 里右键 → `Create > Game > Weapon Data`,建 3 个(先填前两个的值,近战 `Sword` 步骤 3 再细调):

   | 资产名 | weaponName | type | damage | cooldown | maxAmmo | range |
   |---|---|---|---|---|---|---|
   | `Pistol` | Pistol | Ranged | 12 | 0.25 | 30 | 1 |
   | `Rifle`  | Rifle  | Ranged | 7  | 0.08 | 90 | 1 |
   | `Sword`  | Sword  | Melee  | 30 | 0.4  | **-1** | 1 |

2. **记下旧引用**:选中 Player,看现在挂着的 `PlayerShooter` 组件,记下它 `Bullet Pool` 和 `Fire Point` 分别拖的是哪个物体(等下 `WeaponController` 要用同样的)。
3. **加 WeaponController**:Player → `Add Component` → `WeaponController`。
   - `Bullet Pool`、`Fire Point` 拖成和刚才 `PlayerShooter` 一样的对象;
   - `Weapons` 数组 `Size` 设 3,元素 0/1/2 依次拖 `Pistol`/`Rifle`/`Sword`(顺序决定数字键 1/2/3)。
4. **删除 PlayerShooter 组件**:确认 `WeaponController` 配好后,在 Player 上把旧的 `PlayerShooter` 组件移除(组件右上角三个点 → Remove Component)。`Assets/Scripts/Weapons/PlayerShooter.cs` 脚本文件可以留着不删,只是不再挂用。
5. 保存场景。

### ✅ 步骤 2 验收

- [ ] 进入 Play,左键能射击(和以前一样),子弹能打死敌人。
- [ ] 按 `2` 切到步枪:射速明显变快(连发更密)、单发伤害变低;按 `1` 切回手枪:射速慢、单发高。
- [ ] 切枪后再切回来,不报错。
- [ ] Console 无报错。(按 `3` 现在会切到近战,但表现/伤害留到步骤 3 验收)

---

## 步骤 3:近战收编进武器系统

**这一步做什么**:其实脚本步骤 2 全写好了,这一步主要是**确认近战数据 + 单独验收近战手感**。近战 = `Sword`(weapons 数组第 3 个,数字键 `3`)。

### 3.1 确认/微调 Sword 数据

选中 `Assets/Data/Sword`,确认:`type = Melee`、`maxAmmo = -1`(无限)、`range = 1`、`damage = 30`、`cooldown = 0.4`。`range` 就是第 2 周近战的判定半径(圆心在角色前方 `range` 处、半径 `range`),想打得更远/更大就调大它。

### 3.2 它是怎么跑起来的(回顾数据流)

按 `3` 切到 `Sword` → 左键按下 → `WeaponController.FireCurrentWeapon`:
1. `strategies[Melee].Fire(...)` → `MeleeWeaponStrategy` 在角色前方画圈,圈里 `Enemy` 扣 30;
2. `cooldownTimer = 0.4`;
3. `maxAmmo < 0`,不消耗弹药;
4. 是近战 → `playerController.TriggerAttack()` → 切 `AttackState`(角色变黄 0.25 秒,期间 `CanAct=false` → 这段时间不能再开火、不能移动)。

所以近战的"一下一下、挥砍时定身"手感,是靠 `AttackState` 期间 `CanAct` 为 false 实现的,和第 2 周一致——只是伤害判定从状态类搬到了策略类。

### ✅ 步骤 3 验收

- [ ] 按 `3` 切到近战,左键点一下:角色短暂变黄,面前一圈内的敌人掉血(站远点确认打不到)。
- [ ] 挥砍那 0.25 秒内,不能移动、也不能再触发下一下(动作有节奏,不是狂点就狂掉血)。
- [ ] 近战不消耗弹药(连点很多下也不会"打空")。
- [ ] 切回手枪/步枪,远程一切正常。

---

## 步骤 4:弹药 UI + 补给拾取

**这一步做什么**:右上角显示"武器名 + 弹药数",开火递减、打空不能射;地上放补给包,捡了回弹药;近战显示 ∞。

### 4.1 新建 `Assets/Scripts/UI/AmmoUI.cs`

参考实现:`Reference/Scripts/UI/AmmoUI.cs`。它订阅两个事件——换武器更新名字、弹药变化更新数字:

```csharp
using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class AmmoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private string weaponName = "";
        private int current;
        private int max;

        private void OnEnable()
        {
            EventBus.Subscribe<AmmoChangedEvent>(OnAmmoChanged);
            EventBus.Subscribe<WeaponChangedEvent>(OnWeaponChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AmmoChangedEvent>(OnAmmoChanged);
            EventBus.Unsubscribe<WeaponChangedEvent>(OnWeaponChanged);
        }

        private void OnAmmoChanged(AmmoChangedEvent e) { current = e.Current; max = e.Max; Refresh(); }
        private void OnWeaponChanged(WeaponChangedEvent e) { weaponName = e.WeaponName; Refresh(); }

        private void Refresh()
        {
            if (label == null) return;
            string ammoText = max < 0 ? "∞" : $"{current}/{max}";
            label.text = $"{weaponName}  {ammoText}";
        }
    }
}
```

讲解:两个事件是**分别**到达的(换武器时先发 `WeaponChangedEvent` 再发 `AmmoChangedEvent`),所以名字和弹药各自缓存成字段,任一变化都 `Refresh()` 重拼一次文本。`max < 0` 时显示 `∞`(近战无限)。

### 4.2 新建 `Assets/Scripts/Weapons/AmmoPickup.cs`

参考实现:`Reference/Scripts/Weapons/AmmoPickup.cs`。

```csharp
using UnityEngine;

namespace Game.Weapons
{
    [RequireComponent(typeof(Collider2D))]
    public class AmmoPickup : MonoBehaviour
    {
        [SerializeField] private int amount = 15;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (other.TryGetComponent(out WeaponController weapon))
                weapon.AddAmmo(amount);
            Destroy(gameObject);
        }
    }
}
```

讲解:玩家碰到(Trigger)就给**当前武器**补弹药、然后销毁自己。注意 `AddAmmo` 里对"无限弹药(近战)"直接跳过——所以近战状态下踩补给不会有反应,得切到远程武器再捡才有效(这是 V1 的简化,以后可以改成"补所有远程武器"或"补给绑定弹药类型")。

### 4.3 Unity 编辑器操作

**A. 弹药文本(TMP)**

1. 右键 `Canvas` → `UI > Text - TextMeshPro`。**第一次用 TMP 会弹一个窗口让你 `Import TMP Essentials`,点它导入**(只需一次),导完关掉窗口。
2. 改名 `AmmoText`,`Rect Transform` 锚点用 `Alt+Shift` 定到右上角,`Pos X = -120`、`Pos Y = -40`;`Text` 内容随便填(运行时会被覆盖),字号调到看得清,对齐方式设右对齐。
3. 选中 `AmmoText`(或 Canvas)→ `Add Component` → `AmmoUI`,把 `AmmoText` 自己(它带 `TextMeshProUGUI` 组件)拖到脚本的 `Label` 槽。

**B. 补给包**

1. Hierarchy 右键 `2D Object > Sprites > Circle`(或 Square),改名 `AmmoPickup`,`Transform` 缩小一点(比如 Scale 0.5),`SpriteRenderer` 颜色调成醒目的(比如青色),放在场景里玩家够得到的位置。
2. 它自带一个 `Collider2D`(Circle/Box)。在该 Collider 上勾选 **`Is Trigger`**。
3. `Add Component` → `AmmoPickup`,`Amount` 填 15。
4. 复制几个摆在不同位置。想反复用建议做成预制体(拖进 `Assets/Prefabs/`),但不是必须。
5. 保存场景。

> 触发前提:玩家身上有 `Rigidbody2D`(已有)+ `Collider2D`(已有),补给包 `Collider2D` 勾了 `Is Trigger`,且玩家 Tag 是 `Player`——三者齐了 `OnTriggerEnter2D` 才会触发。

### ✅ 步骤 4 验收

- [ ] 右上角显示当前武器名 + 弹药,例如 `Pistol 30/30`。
- [ ] 左键开火,数字递减;打到 0 后左键**射不出来**。
- [ ] 切到步枪,显示变成 `Rifle 90/90`,各自弹药独立;切回手枪弹药还是切走时的数。
- [ ] 切到近战 `3`,显示 `Sword ∞`,连砍不消耗。
- [ ] 走过去踩补给包(需处于远程武器):当前武器弹药增加(不超过弹夹上限),补给包消失。
- [ ] 全程 Console 无报错。

---

## 常见问题排查

| 现象 | 可能原因 | 排查 |
|---|---|---|
| Image 组件里**找不到 `Image Type`** | `Source Image` 是 `None` 时该字段会被隐藏 | 先给 `Source Image` 指定一张图(内置 `UISprite`,或任意 Square sprite),`Image Type` 就会出现 |
| 血条/弹药 UI 一直不更新 | UI 脚本没订阅成功,或发布时它还没订 | 确认 UI 的 `Subscribe` 在 `OnEnable`、Player/WeaponController 的初始广播在 `Start`;确认事件类型对得上 |
| `Create > Game > Weapon Data` 菜单没有 | `WeaponData` 没编译过,或 `[CreateAssetMenu]` 写错 | 等编译完;检查 Console 有无报错 |
| 切武器/开火报 `IndexOutOfRange` | `weapons` 数组是空的或长度不够 | Inspector 里给 `WeaponController.Weapons` 至少填 1 个元素;数字键 3 对应要有第 3 个 |
| 左键完全不开火 | `WeaponController` 的 `Bullet Pool`/`Fire Point` 没拖,或 `PlayerShooter` 和 `WeaponController` 都在抢左键 | 确认引用已拖;确认旧 `PlayerShooter` 组件已移除 |
| 打空了还能射 | `HasAmmo()` 没接进开火条件,或武器 `maxAmmo` 填了 -1 | 远程武器 `maxAmmo` 要 ≥ 0;检查 `Update` 里的 `&& HasAmmo()` |
| 踩补给没反应 | 当前是近战(无限弹药会跳过),或 Collider 没勾 Is Trigger,或玩家 Tag 不对 | 切到远程再踩;勾 `Is Trigger`;确认玩家 Tag = `Player` |
| TMP 文本是乱码/不显示 | 没导入 TMP Essentials | 菜单 `Window > TextMeshPro > Import TMP Essential Resources` |
| 子弹伤害没按武器变 | `Bullet.SetDamage` 没加,或 `RangedWeaponStrategy` 没调 | 确认 `Bullet` 有 `SetDamage`,策略里 `bullet.SetDamage(data.damage)` |
| 报 `CS0104: EventBus 是二义性引用` | 文件里有 `using Unity.VisualScripting;`——那个包里也有个 `EventBus` 类 | **删掉这个没用到的 using**(根因)。别急着用 `using EventBus = Game.Core.EventBus;` 打补丁,那只是掩盖问题 |
| UI 显示的武器名改不动 | 广播时用了 `CurrentWeapon.name`(SO 的**资产文件名**)而不是 `weaponName`(你定义的字段) | `WeaponChangedEvent` 里传 `CurrentWeapon.weaponName`;两者恰好同名时这个 bug 会被完美掩盖 |

---

## 本周验收总 checklist

- [x] EventBus 建好,血条随受击实时下降(UI 无任何对 Player 的硬引用)。
- [x] 三把武器 SO 建好,数字键 1/2/3 切换,伤害/射速手感不同。
- [x] 远程发子弹、近战画圈判定,分别由两个策略实现;新增武器只需加 SO(+ 必要时加策略)。
- [x] 弹药显示 / 消耗 / 打空拦截 / 补给拾取 / 近战 ∞ 全部正常。
- [x] 第 1、2 周的移动 / 闪避 / 受击 / 敌人 AI 均未被破坏。

**四步全部 Play 验收通过,第 3 周完成。**

---

## 实际完成记录(这一节是"发生了什么",不是计划)

### 落地的文件

**新增(9 个)**

- `Core/EventBus.cs`、`Core/GameEvents.cs`——static 泛型事件总线 + 三个 `readonly struct` 事件。
- `Weapons/WeaponData.cs`(SO + `WeaponType` 枚举)、`Weapons/IWeaponStrategy.cs`、`Weapons/RangedWeaponStrategy.cs`、`Weapons/MeleeWeaponStrategy.cs`、`Weapons/WeaponController.cs`、`Weapons/AmmoPickup.cs`。
- `UI/HealthBarUI.cs`、`UI/AmmoUI.cs`。

**修改(6 个)**

- `Entities/Health.cs`——加 `Max` 属性 + `HealthChanged` 事件。
- `Entities/PlayerController.cs`——桥接 `HealthChanged` → 全局 `PlayerHealthChangedEvent`;`Start` 广播初始血量;新增 `TriggerAttack()`;删掉 `attackRange`/`attackDamage`/`ConsumeAttackPressed()`。
- `StateMachines/Player/PlayerIdleState.cs`、`PlayerMoveState.cs`——删掉右键近战分支。
- `StateMachines/Player/PlayerAttackState.cs`——删掉 `PerformHit()`,只剩"变黄 + 计时"的表现。
- `Weapons/Bullet.cs`——加 `SetDamage(int)`,让武器 SO 决定子弹伤害。

**资产 / 场景**

- `Assets/Data/` 下三个武器 SO:`Pistol`(Ranged/12/0.25/30)、`Rifle`(Ranged/7/0.08/90)、`Sword`(Melee/30/0.4/**-1**/range 1)。
- Player 上 `PlayerShooter` 组件已移除,换成 `WeaponController`(脚本文件 `PlayerShooter.cs` 保留未删,只是不再挂用)。
- 场景新增 Canvas(血条 `HealthBar_BG` + `HealthBar_Fill`、TMP 弹药文本 `AmmoText`);`Assets/Prefabs/AmmoPickup.prefab`(BoxCollider2D + Is Trigger + Gravity Scale 0)。
- 血条 sprite 用自建的 `Assets/Art/Square`(纯白无圆角),不用内置 `UISprite`——见下面第 3 条坑。

### 踩过的坑

1. **`Image Type` 在 Inspector 里找不到**——`Source Image` 为 `None` 时,Unity 会把 `Image Type` 整个字段隐藏掉。给 `Source Image` 指定任意一张 sprite,它才出现,然后才能选 `Filled`。

2. **血条边缘发脏**——一开始 `Source Image` 用了 Unity 内置的 `UISprite`,那是一张**带圆角的九宫格图**,而 `Image Type = Filled` 不走九宫格逻辑、直接把整张图连圆角一起横向裁切拉伸,于是填充左端出现一道压扁的暗边。改用 `Create > 2D > Sprites > Square` 生成的纯白无圆角方块后干净了。**结论:做纯色条状 UI,别用带圆角的图。**

3. **`CS0104: EventBus 是二义性引用`**——`HealthBarUI` 里被 IDE 自动加了一句 `using Unity.VisualScripting;`,而那个包里**也有一个叫 `EventBus` 的类**,和我们 `Game.Core.EventBus` 撞名,编译器不知道该用哪个。当时用 `using EventBus = Game.Core.EventBus;` 起别名绕过去了,**但根因是那句用不到的 `using`**,后来直接删掉它,别名也一并删了。教训:遇到二义性,先看是不是引入了多余的 `using`,别急着打别名补丁。

4. **`WeaponChangedEvent` 广播了资产文件名而不是武器数据**——`WeaponController.BroadcastWeapon()` 里写成了 `CurrentWeapon.name`。`.name` 是所有 `UnityEngine.Object` 都有的属性(= SO 的**文件名**),而 `weaponName` 才是我们在 `WeaponData` 里定义的字段。因为三个资产恰好取了同名(`Pistol.asset` 的 `weaponName` 也叫 `Pistol`),**这个 bug 被完美掩盖、验收全过**——直到你想把显示名改成中文才会发现怎么改都不生效。已改回 `CurrentWeapon.weaponName`。

5. **`HealthBarUI.OnDisable` 里把 `Unsubscribe` 写成了 `Subscribe`**——复制粘贴 `OnEnable` 那行时漏改。后果是禁用时不退订、反而**又订阅一次**,委托链只增不减。因为血条从没被禁用过、场景也没重载,验收阶段完全看不出来;一旦以后加了场景切换,EventBus 就会攥着已销毁的 UI 对象报 `MissingReferenceException`。已修正。**`OnEnable` 订、`OnDisable` 退必须成对,写反了编译器一个字都不会提醒。**

6. **`AmmoPickup` 的 `RequireComponent` 写成了 `Rigidbody2D`**——真正不可缺的是 `Collider2D`(没它就没有 `OnTriggerEnter2D`);`Rigidbody2D` 是可选的,因为 Trigger 只要求碰撞双方**至少一方**有刚体,而玩家已经有了。功能上没出问题(刚体 `Gravity Scale` 设了 0,补给包不会掉下去),但那是个每帧参与物理模拟的 Dynamic 刚体,纯属浪费。

> 4 和 5 是这周最值得记的两个:**都属于"程序照常跑、验收照常过"的沉默 bug**。它们不会报错、不会崩,只会在几周后以完全无关的症状(改名不生效 / 切场景崩溃)找上门。这类问题只能靠 review 代码抓,测不出来。

## 下周预告:第 4 周 - 命令模式与输入缓冲

会把输入封装成 Command 对象(为连招 / 输入缓冲 / 回放留接口),并开始碰房间生成(`RoomConfig` + 简单工厂)。EventBus 这周打好的地基,后面房间切换、敌群刷新的通知都会走它。
