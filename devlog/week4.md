# 第 4 周:命令模式与输入缓冲

> 这周的主题是**把"输入"和"动作"拆开**。
>
> 现在你的输入是散的:`PlayerController` 读 WASD、`PlayerIdleState`/`PlayerMoveState` 读 Shift、`WeaponController` 读左键和数字键。三个地方各读各的,能跑,但有两个问题:① 想改键位/加手柄支持,要翻三个文件;② **想做输入缓冲,根本无从下手**——因为没有任何一个地方"拿得到一个待执行的动作请求"。
>
> 命令模式做的事很简单:**把"一个动作"封装成一个对象**(`DashCommand`、`AttackCommand`……)。一旦动作变成了对象,你就能把它塞进队列里等一会儿再执行(输入缓冲)、能记下来重放(回放)、能让 AI 复用同一套动作。这周先兑现第一个:**输入缓冲**——它直接决定了操作"跟不跟手"。
>
> 照旧:分 4 步,每步可编译、可 Play 验收。参考实现在 `Reference/Scripts/...`,你在 `Assets/Scripts/...` 对应路径创建/替换。

## 本周目标(对齐 README 六周计划)

- **指令封装**:`MoveCommand` / `AttackCommand` / `DashCommand`(+ `GrenadeCommand`),统一 `ICommand` 接口。
- **输入缓冲队列**:离散动作(闪避/手雷)按下时若做不了,先排队等待,条件一满足立刻执行——而不是被丢弃。
- **技能冷却可视化**:手雷冷却的环形遮罩 UI。
- **范围技能(手雷)**:演示"非指向性命令"——扔出去、到点就炸、炸到谁算谁。

完成后:操作手感明显变"跟手"(挥砍到一半按闪避,砍完会立刻闪出去,而不是石沉大海);按 `Q` 扔手雷,飞出去一段后范围爆炸,右下角技能图标转圈冷却。第 1~3 周所有功能保持不变。

---

## 0. 新建目录 + 两个核心概念

### 0.1 新建目录

- `Assets/Scripts/Commands/` —— 命名空间 `Game.Commands`。这是 README 项目结构里最后一个没用上的目录,这周填上。

### 0.2 命令模式:为什么要多这一层?

直接写 `if (按了Shift) Dash();` 不是更短吗?短,但它把**"谁按了什么"**和**"要做什么动作"**焊死在了一起。

命令模式把动作抽成对象:

```csharp
public interface ICommand
{
    bool CanExecute();   // 现在能做吗?
    void Execute();      // 做!
}
```

关键在于 **`CanExecute()` 和 `Execute()` 是分开的**。这意味着你可以**先问、后做**,甚至"先问、问不过就等一会儿再问"。整个输入缓冲机制就建立在这一点上。

一旦动作是对象了,后面这些都变得顺理成章(这周只做第一个,但接口都留好了):

| 能力 | 怎么实现 | 本周做? |
|---|---|---|
| 输入缓冲 / 连招 | 命令进队列,每帧问 `CanExecute()`,能了就执行 | ✅ |
| 回放 / 录像 | 把每帧的命令序列记下来,重放一遍 | 留接口 |
| AI 复用玩家动作 | 让 AI 也产生 `AttackCommand`,走同一套执行路径 | 留接口 |
| 改键位 | 只改 `PlayerInputHandler` 一处 | ✅ |

### 0.3 输入缓冲:它到底解决什么?

考虑这个场景(你现在的游戏里真实存在):

> 你切到近战,左键挥砍。挥砍要 0.25 秒,期间是 `AttackState`,`CanAct` 为 `false`。你在挥砍进行到 0.2 秒时按了 `Shift` 想闪避——**这个输入被直接丢掉了**。因为那一帧 `PlayerIdleState`/`PlayerMoveState` 根本没在跑(当前是 `AttackState`),没人去读那个 Shift。你的感受是:"我明明按了,它没反应。"

于是你只能等动作彻底结束、看准了再按。这就是"不跟手"。

**输入缓冲的做法**:按下 Shift 时,不立刻执行,而是把 `DashCommand` **扔进一个队列**。队列每帧问一次队首命令:"你现在能执行吗?"挥砍结束、`CanAct` 变 `true` 的那一帧,它立刻执行——你的闪避在动作结束的瞬间就出去了,像是"接上了"。这就是**取消后摇 / 连招**的底层原理。

两个必要的约束(否则会从"跟手"变成"诡异"):

1. **命令有寿命**(`bufferDuration`,常见 0.2~0.3 秒)。你 3 秒前随手按的一下闪避,不该在你毫无防备时突然生效。超时就作废。
2. **队列有容量**。狂按键不该攒出一长串动作、然后像录像一样连放。满了就挤掉最老的。

---

## 步骤 1:命令模式 + 输入统一(纯重构,行为不变)

**这一步做什么**:建立命令体系,把散落三处的输入全部收拢到一个 `PlayerInputHandler`。**这一步做完,游戏玩起来应该和第 3 周一模一样**——没有任何新功能。这是有意的:重构和加功能分开做,出了问题好定位。

> 💡 这一步会把手雷相关的三个脚本(`Grenade`、`GrenadeThrower`、`GrenadeCommand`)也一并建好,否则 `PlayerInputHandler` 引用不到它们、编译不过。但**组件和预制体要到步骤 3 才配**,所以这一步按 `Q` 不会有任何反应,这是正常的。

### 1.1 新建 `Assets/Scripts/Commands/ICommand.cs`

```csharp
namespace Game.Commands
{
    public interface ICommand
    {
        bool CanExecute();
        void Execute();
    }
}
```

### 1.2 新建四个命令

**`MoveCommand.cs`** —— 注意它是**持续输入**,不进缓冲队列:

```csharp
using Game.Entities;
using UnityEngine;

namespace Game.Commands
{
    public class MoveCommand : ICommand
    {
        private readonly PlayerController player;
        private Vector2 direction;

        public MoveCommand(PlayerController player) { this.player = player; }

        public void SetDirection(Vector2 value) => direction = value;

        public bool CanExecute() => true;   // 移动永远可以"提交";能不能真的动是状态机的事

        public void Execute() => player.SetMoveInput(direction);
    }
}
```

> 移动为什么也要做成命令?**为了统一**。将来做回放时,录下每帧的命令序列就能完整复现一局,不必为移动单开一套记录机制。注意 `direction` 用 `SetDirection` 每帧写入、**命令实例本身复用**(不每帧 `new`)——每帧 new 一个对象会产生大量 GC 垃圾,和对象池是同一个道理。

**`AttackCommand.cs`**:

```csharp
using Game.Weapons;

namespace Game.Commands
{
    public class AttackCommand : ICommand
    {
        private readonly WeaponController weapon;

        public AttackCommand(WeaponController weapon) { this.weapon = weapon; }

        public bool CanExecute() => weapon != null && weapon.CanFire();

        public void Execute() => weapon.Fire();
    }
}
```

> 三个条件(能行动 / 冷却好了 / 有弹药)全部委托给 `WeaponController.CanFire()`,不在命令里重复判断——判断逻辑只写一处。

**`DashCommand.cs`** —— 这个是输入缓冲的主角:

```csharp
using Game.Entities;

namespace Game.Commands
{
    public class DashCommand : ICommand
    {
        private readonly PlayerController player;

        public DashCommand(PlayerController player) { this.player = player; }

        public bool CanExecute() => player.CanAct && player.CanDash;

        public void Execute() => player.TriggerDash();
    }
}
```

**`GrenadeCommand.cs`**:

```csharp
using Game.Entities;
using Game.Weapons;

namespace Game.Commands
{
    public class GrenadeCommand : ICommand
    {
        private readonly PlayerController player;
        private readonly GrenadeThrower thrower;

        public GrenadeCommand(PlayerController player, GrenadeThrower thrower)
        {
            this.player = player;
            this.thrower = thrower;
        }

        // thrower 判空:步骤 3 之前 Player 上还没挂 GrenadeThrower 组件,这里是 null
        public bool CanExecute() => thrower != null && player.CanAct && thrower.CanThrow;

        public void Execute() => thrower.Throw();
    }
}
```

### 1.3 新建 `Assets/Scripts/Commands/InputBuffer.cs`

参考实现:`Reference/Scripts/Commands/InputBuffer.cs`。**纯 C# 类,不是 MonoBehaviour**(和 `StateMachine` 一样,由 `PlayerInputHandler` 内部 `new` 一个)。

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Game.Commands
{
    public class InputBuffer
    {
        private readonly struct BufferedCommand
        {
            public readonly ICommand Command;
            public readonly float ExpireTime;

            public BufferedCommand(ICommand command, float expireTime)
            {
                Command = command;
                ExpireTime = expireTime;
            }
        }

        private readonly Queue<BufferedCommand> queue = new Queue<BufferedCommand>();
        private readonly int capacity;
        private readonly float bufferDuration;

        public int Count => queue.Count;

        public InputBuffer(int capacity, float bufferDuration)
        {
            this.capacity = capacity;
            this.bufferDuration = bufferDuration;
        }

        public void Enqueue(ICommand command)
        {
            if (queue.Count >= capacity)
            {
                queue.Dequeue();   // 满了:挤掉最老的
            }
            queue.Enqueue(new BufferedCommand(command, Time.time + bufferDuration));
        }

        public void Tick()
        {
            while (queue.Count > 0)
            {
                BufferedCommand head = queue.Peek();

                if (Time.time > head.ExpireTime)
                {
                    queue.Dequeue();   // 按早了太久,作废,看下一条
                    continue;
                }

                if (!head.Command.CanExecute())
                {
                    return;            // 还不能执行(比如正在挥砍),留在队里等下一帧
                }

                queue.Dequeue();
                head.Command.Execute();
                return;                // 一帧最多放一个
            }
        }

        public void Clear() => queue.Clear();
    }
}
```

讲解——`Tick()` 里那个 `while` 循环是全部精髓,三条规则:

1. **过期就丢**(`Time.time > ExpireTime`):`continue` 去看下一条。
2. **不能执行就等**(`!CanExecute()`):直接 `return`,命令**留在队列里**,下一帧再问。这是缓冲的本质。
3. **一帧只执行一个**:执行完立刻 `return`。否则同一帧可能又闪避又扔雷。

### 1.4 新建 `Assets/Scripts/Commands/PlayerInputHandler.cs`

参考实现:`Reference/Scripts/Commands/PlayerInputHandler.cs`。**这是全项目唯一读键盘鼠标的地方**。

```csharp
using Game.Entities;
using Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Commands
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("输入缓冲")]
        [Tooltip("一个离散命令在队列里最多等多久(秒);超时未执行就丢弃")]
        [SerializeField] private float bufferDuration = 0.25f;
        [Tooltip("队列最多存几条命令;满了会挤掉最老的")]
        [SerializeField] private int bufferCapacity = 3;

        private PlayerController player;
        private WeaponController weapon;
        private GrenadeThrower grenadeThrower;

        private InputBuffer buffer;

        // 命令实例只 new 一次、反复复用 —— 每帧 new 会产生 GC 垃圾
        private MoveCommand moveCommand;
        private AttackCommand attackCommand;
        private DashCommand dashCommand;
        private GrenadeCommand grenadeCommand;

        public InputBuffer Buffer => buffer;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            weapon = GetComponent<WeaponController>();
            grenadeThrower = GetComponent<GrenadeThrower>();   // 步骤 3 之前为 null,命令内部判空

            buffer = new InputBuffer(bufferCapacity, bufferDuration);

            moveCommand = new MoveCommand(player);
            attackCommand = new AttackCommand(weapon);
            dashCommand = new DashCommand(player);
            grenadeCommand = new GrenadeCommand(player, grenadeThrower);
        }

        private void Update()
        {
            ReadMove();
            ReadContinuousFire();
            ReadDiscreteActions();
            ReadWeaponSwitch();

            buffer.Tick();   // 每帧推一次缓冲队列
        }

        /* -------- 持续动作:直接执行,不缓冲 -------- */

        private void ReadMove()
        {
            Vector2 direction = Vector2.zero;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                        - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
                float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                        - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
                direction = new Vector2(x, y).normalized;
            }

            moveCommand.SetDirection(direction);
            moveCommand.Execute();
        }

        private void ReadContinuousFire()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed) return;

            if (attackCommand.CanExecute())
            {
                attackCommand.Execute();
            }
        }

        /* -------- 离散动作:进缓冲队列 -------- */

        private void ReadDiscreteActions()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // 必须用 wasPressedThisFrame,不能用 isPressed ——
            // 否则按住 Shift 会每帧往队列塞一条,瞬间灌满
            if (kb.leftShiftKey.wasPressedThisFrame) buffer.Enqueue(dashCommand);
            if (kb.qKey.wasPressedThisFrame) buffer.Enqueue(grenadeCommand);
        }

        /* -------- 切枪:立即生效,不走命令 -------- */

        private void ReadWeaponSwitch()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || weapon == null) return;

            if (kb.digit1Key.wasPressedThisFrame) weapon.SwitchTo(0);
            else if (kb.digit2Key.wasPressedThisFrame) weapon.SwitchTo(1);
            else if (kb.digit3Key.wasPressedThisFrame) weapon.SwitchTo(2);
        }
    }
}
```

**为什么移动和开火不进缓冲队列?**

因为它们是**持续输入**——"这一帧我想不想动/想不想开枪"。缓冲一个 0.2 秒前的移动请求是荒谬的(你早就松手了)。而**离散动作**(闪避/手雷)是"我要做这一下",它有前置条件、常常在按下那一刻做不了,缓冲才有意义。**这个区分是这周设计上最重要的判断,不是偷懒。**

**切枪为什么不做成命令?** 它没有前置条件、立即生效,也没人会想"0.2 秒后再帮我切枪"。把它也做成 `SwitchWeaponCommand` 是个很好的练习(见文末)。

### 1.5 替换 `Assets/Scripts/Entities/PlayerController.cs`

参考实现:`Reference/Scripts/Entities/PlayerController.cs`。改动:

1. **删掉 `ReadMoveInput()` 整个方法**和 `Update` 里对它的调用(输入归 `PlayerInputHandler`);
2. **删掉 `ConsumeDashPressed()`**(闪避输入归 `DashCommand`);
3. 新增三个"动作入口",供命令调用:

```csharp
/// <summary>由 MoveCommand 每帧写入移动方向。</summary>
public void SetMoveInput(Vector2 value) => MoveInput = value;

/// <summary>由 DashCommand 调用(它已经用 CanAct && CanDash 判断过了)。</summary>
public void TriggerDash() => stateMachine.ChangeState(DashState);

/// <summary>由 WeaponController 在近战开火时调用(第 3 周就有了)。</summary>
public void TriggerAttack() => stateMachine.ChangeState(AttackState);
```

改完后 `Update` 变成:

```csharp
private void Update()
{
    RotateTowardsMouse();   // "朝向鼠标"是表现,不是动作命令,留在这里

    if (dashCooldownTimer > 0f)
    {
        dashCooldownTimer -= Time.deltaTime;
    }

    stateMachine.Tick();
}
```

> `MoveInput` 属性本身保留(`public Vector2 MoveInput { get; private set; }`),只是**它的值现在由外部通过 `SetMoveInput` 写入**,而不是自己读键盘得来。状态机那边完全不用改——它读 `MoveInput` 的方式没变。这是重构的漂亮之处:换掉数据来源,不惊动使用者。

### 1.6 替换 `Assets/Scripts/Weapons/WeaponController.cs`

参考实现:`Reference/Scripts/Weapons/WeaponController.cs`。改动:

1. **`Update` 里删掉所有输入读取**(左键、数字键),只剩冷却倒计时;
2. 删掉 `using UnityEngine.InputSystem;`(不再读输入了)和 `HandleSwitchInput()` 方法;
3. 把 `FireCurrentWeapon()` 改名为 **`public void Fire()`**,新增 **`public bool CanFire()`**,`SwitchTo` 改成 **`public`**:

```csharp
private void Update()
{
    if (cooldownTimer > 0f)
    {
        cooldownTimer -= Time.deltaTime;
    }
}

/* ---------- 对外能力(供命令调用) ---------- */

public bool CanFire()
{
    bool canAct = playerController == null || playerController.CanAct;
    return canAct && cooldownTimer <= 0f && HasAmmo();
}

public void Fire()
{
    WeaponData data = CurrentWeapon;

    strategies[data.type].Fire(this, data);
    cooldownTimer = data.cooldown;

    if (data.maxAmmo >= 0)
    {
        currentAmmo[currentIdx]--;
        BroadcastAmmo();
    }

    if (data.type == WeaponType.Melee && playerController != null)
    {
        playerController.TriggerAttack();
    }
}

public void SwitchTo(int index) { /* 内容不变,只是从 private 改成 public */ }
```

> 这就是命令模式带来的分层:**输入层**(谁按了什么)和**能力层**(能做什么、怎么做)彻底分开。于是同一个 `Fire()` 既能被玩家按键触发,也能被 AI、被回放系统、被缓冲队列触发——它不再关心"是谁让我开的枪"。

### 1.7 替换两个状态类

`PlayerIdleState.cs` / `PlayerMoveState.cs`:**删掉读 Shift 的闪避分支**(第 3 周删了近战分支,这周删闪避分支)。改完后 `Tick` 只剩状态转换判断:

```csharp
// PlayerIdleState
public void Tick()
{
    if (player.MoveInput.sqrMagnitude > 0.01f)
    {
        stateMachine.ChangeState(player.MoveState);
    }
}

// PlayerMoveState
public void Tick()
{
    if (player.MoveInput.sqrMagnitude < 0.01f)
    {
        stateMachine.ChangeState(player.IdleState);
    }
}
```

> 到这里,状态类**彻底不读输入了**,只根据"当前数据"决定状态转换。职责比第 2 周干净得多。

### 1.8 新建手雷的两个脚本(占位,步骤 3 才配置)

`Assets/Scripts/Weapons/GrenadeThrower.cs` 和 `Assets/Scripts/Weapons/Grenade.cs` —— 完整代码见步骤 3(和 `Reference/Scripts/Weapons/`)。**这一步必须先把文件建出来**,否则 `GrenadeCommand` 和 `PlayerInputHandler` 引用不到、编译不过。

### 1.9 Unity 编辑器操作

**A. 挂脚本**

1. 选中 Player → `Add Component` → `PlayerInputHandler`。
2. Inspector 里确认 `Buffer Duration = 0.25`、`Buffer Capacity = 3`。

**B. ⚠️ 设置脚本执行顺序(这一步很关键,不做会有 1 帧延迟)**

`PlayerInputHandler.Update` **写** `MoveInput`,`PlayerController.Update` **读** `MoveInput`。但 Unity **不保证**两个 MonoBehaviour 的 `Update` 谁先跑——如果 `PlayerController` 先跑,它读到的是**上一帧**的输入,移动会有 1 帧延迟(轻微发飘,但确实存在)。

修法——显式指定执行顺序:

1. 菜单 `Edit > Project Settings...` → 左侧选 **`Script Execution Order`**。
2. 点右下角 **`+`**,在弹出的列表里选 **`PlayerInputHandler`**。
3. 把它的数值改成 **`-100`**(负数 = 比默认的 `Default Time` 更早执行)。
4. 点 `Apply`。

> 这是 Unity 里一个重要但容易被忽略的机制:**只要 A 脚本写、B 脚本读同一份数据,它们的执行顺序就必须是确定的**。以后凡是遇到"数据慢了一帧"的怪现象,先来这里看看。

### ✅ 步骤 1 验收(重构:行为应该和第 3 周完全一样)

- [ ] WASD 移动正常,**不发飘、不延迟**(如果发飘,回去检查 Script Execution Order)。
- [ ] 左键开火正常(手枪/步枪连发手感不变),弹药正常消耗。
- [ ] `1`/`2`/`3` 切枪正常。
- [ ] `Shift` 闪避正常(此时已经走缓冲队列了,但你还感觉不出区别——步骤 2 才验收它)。
- [ ] 近战(`3` + 左键)挥砍正常,挥砍期间不能移动/开火。
- [ ] 受击、血条、弹药 UI 一切照旧。
- [ ] 按 `Q` 没反应(正常——手雷组件步骤 3 才配)。
- [ ] Console 无报错。

**这一步的验收标准就是"什么都没变"。** 如果有任何行为变化,说明重构出了问题,先解决再往下走。

---

## 步骤 2:体会输入缓冲(不写代码,做对比实验)

**这一步做什么**:代码步骤 1 已经全写好了,这一步是**验证缓冲确实在工作**,并通过调参亲身体会它带来的手感差异。这是这周最值得花时间的一步——**理解为什么要有这个机制,比写出它更重要**。

### 2.1 感受"有缓冲"

1. 按 `3` 切到近战。
2. 靠近敌人,左键挥砍(角色变黄 0.25 秒)。
3. **在角色还黄着的时候**按 `Shift`。
4. 观察:黄色一消失(`AttackState` 结束),角色**立刻闪了出去**。

你按 Shift 的那一刻,`DashCommand.CanExecute()` 是 `false`(`CanAct` 为假,因为在 `AttackState`),命令没有被丢弃,而是在队列里等着;`AttackState` 结束、状态回到 `Idle`/`Move` 的那一帧,`CanExecute()` 变 `true`,队列立刻执行了它。

**这就是"取消后摇""连招"的底层机制。** 动作游戏里"手感跟不跟手",很大程度上就取决于这套缓冲有没有做、窗口开多大。

### 2.2 感受"没有缓冲"(对比实验)

1. 选中 Player,把 `PlayerInputHandler` 的 **`Buffer Duration` 改成 `0`**。
2. 重复上面的操作:挥砍中按 `Shift`。
3. 观察:**什么都没发生**。你的输入被丢掉了——这就是第 2、3 周的手感。

原理:`bufferDuration = 0` 意味着命令入队时 `ExpireTime = Time.time + 0`,下一帧 `Tick()` 一检查就已经过期了,直接丢弃。

改回 `0.25` 再玩一次,对比一下。**这个差别,就是这周所有代码的价值。**

### 2.3 调参建议

| `Buffer Duration` | 手感 |
|---|---|
| `0` | 输入经常"丢",要等动作结束看准了再按,很别扭 |
| `0.15` ~ `0.3` | 跟手。大多数动作游戏在这个区间 |
| `> 0.5` | 开始"诡异":你半秒前随手按的闪避会在你不想要的时候突然触发 |

`Buffer Capacity = 3` 一般够用。想试试它的作用,可以把它调成 `10`,然后在挥砍期间狂按 Shift——会攒出一串闪避,砍完之后连着闪好几下,那就是"队列太长"的坏处。

### ✅ 步骤 2 验收

- [ ] 挥砍期间按 `Shift`,挥砍一结束角色**立刻闪避**(缓冲生效)。
- [ ] `Buffer Duration = 0` 时,同样操作**没有任何反应**(对比确认缓冲确实是它在起作用)。
- [ ] 站着不动、隔很久按一下 `Shift`,不会莫名其妙攒出多次闪避。
- [ ] 改回 `Buffer Duration = 0.25`,保存场景。

---

## 步骤 3:手雷(非指向性范围技能)

**这一步做什么**:实现 `Q` 键扔手雷——飞出去、滑停、引信烧完、范围爆炸。这是 README 里"演示非指向性命令"的部分。

### 3.1 指向性 vs 非指向性

对比一下你已经有的两种伤害:

| | 子弹(`Bullet`) | 手雷(`Grenade`) |
|---|---|---|
| 怎么打中 | `OnTriggerEnter2D`——**碰到谁伤谁** | `OverlapCircleAll`——**到点了,圈里的都倒霉** |
| 需要目标吗 | 需要沿着枪口方向飞、撞上才算 | 不需要,扔出去就不管了 |
| 需要 Collider2D 吗 | 需要(靠碰撞检测) | **不需要**(伤害靠爆炸瞬间的范围检测) |

手雷这种"发射后不管"(fire-and-forget)的技能,是命令模式最自然的用法:一个**无参对象**,进队列、出队列、执行,干干净净——`GrenadeCommand` 里连一个参数都没有。

### 3.2 新建 `Assets/Scripts/Weapons/GrenadeThrower.cs`

参考实现:`Reference/Scripts/Weapons/GrenadeThrower.cs`。挂在 Player 上,只管冷却 + 从对象池取一颗手雷丢出去。

```csharp
using Game.Core;
using UnityEngine;

namespace Game.Weapons
{
    public class GrenadeThrower : MonoBehaviour
    {
        [SerializeField] private ObjectPool grenadePool;
        [Tooltip("投掷起点;留空就用玩家自己的 transform")]
        [SerializeField] private Transform throwPoint;
        [SerializeField] private float cooldown = 3f;

        private float cooldownTimer;

        public bool CanThrow => cooldownTimer <= 0f && grenadePool != null;
        public float Cooldown => cooldown;

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }
        }

        public void Throw()
        {
            Transform origin = throwPoint != null ? throwPoint : transform;

            // 手雷沿生成时的 rotation(= 玩家当前朝向)飞出,初速在 Grenade.OnEnable 里给
            grenadePool.Get(origin.position, origin.rotation);

            cooldownTimer = cooldown;
            EventBus.Publish(new GrenadeThrownEvent(cooldown));   // 通知 UI:开始转冷却圈
        }
    }
}
```

### 3.3 新建 `Assets/Scripts/Weapons/Grenade.cs`

参考实现:`Reference/Scripts/Weapons/Grenade.cs`。

```csharp
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
        [Tooltip("阻尼:越大手雷停得越快")]
        [SerializeField] private float drag = 3f;

        [Header("爆炸")]
        [Tooltip("引信时间:扔出去多久后爆炸")]
        [SerializeField] private float fuseTime = 1f;
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private int damage = 40;
        [Tooltip("爆炸特效停留多久后回池")]
        [SerializeField] private float effectDuration = 0.15f;

        [Header("表现(两个子物体)")]
        [Tooltip("手雷本体:方块 sprite,飞行时显示")]
        [SerializeField] private GameObject bodyVisual;
        [Tooltip("爆炸范围:圆形 sprite,大小由脚本按 explosionRadius 设置")]
        [SerializeField] private GameObject explosionVisual;

        private Rigidbody2D rb;
        private float timer;
        private bool exploded;

        public ObjectPool Pool { get; set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            // 池化对象:所有会变的状态都必须复位!
            timer = 0f;
            exploded = false;

            bodyVisual.SetActive(true);         // 本体露出来
            explosionVisual.SetActive(false);   // 爆炸圈藏起来

            rb.linearDamping = drag;
            rb.linearVelocity = transform.right * throwSpeed;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (!exploded)
            {
                if (timer >= fuseTime) Explode();
                return;
            }

            if (timer >= fuseTime + effectDuration) ReturnToPool();
        }

        private void Explode()
        {
            exploded = true;
            rb.linearVelocity = Vector2.zero;

            // 非指向性伤害:以自己为圆心画圈,圈里的 Enemy 全部扣血
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (Collider2D hit in hits)
            {
                if (!hit.CompareTag("Enemy")) continue;
                if (hit.TryGetComponent(out Health health))
                {
                    health.TakeDamage(damage);
                }
            }

            // 本体消失,爆炸圈按判定半径撑到正确大小。
            // 圆形 sprite 直径 = 1 unit,所以 localScale = 半径 * 2 = 直径 —— 视觉圆严格等于判定圆。
            bodyVisual.SetActive(false);
            explosionVisual.transform.localScale = Vector3.one * explosionRadius * 2f;
            explosionVisual.SetActive(true);
        }

        private void ReturnToPool()
        {
            if (Pool != null) Pool.Release(gameObject);
            else Destroy(gameObject);
        }

        // 在 Scene 视图里选中手雷时画出真实判定范围,方便调参 / 核对表现是否对得上
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
```

**为什么表现要拆成两个子物体?**

因为**手雷本体是方的,爆炸范围是圆的**。如果复用同一个 `SpriteRenderer`"把自己撑大",本体什么形状、爆炸就什么形状——而伤害判定是 `OverlapCircleAll`(圆),方块爆炸圈会让玩家误判范围(四个角看着能炸到、实际炸不到)。**表现必须和判定对得上**,所以本体(方块)和爆炸圈(圆)各用一个子物体,爆炸时藏掉前者、放出后者。

爆炸圈的大小由脚本按 `explosionRadius` 算(`localScale = 半径 × 2 = 直径`),**不在 Inspector 里手填**——这样你以后调整爆炸半径,视觉圈会自动跟上,永远不会和判定脱节。

**⚠️ 池化对象的老规矩**:`OnEnable` 里把 `timer`、`exploded`、**两个子物体的显隐**全部复位。手雷爆炸时会把本体藏起来、把爆炸圈放出来,如果不复位,**下一颗从池里取出来的手雷会顶着上一颗的爆炸造型出场**(一个巨大的橙色圆飞出去)。这是对象池最经典的 bug,你在 `Bullet` 里没遇到只是因为子弹不改变自己的外观。

### 3.4 修改 `Assets/Scripts/Core/GameEvents.cs`

加一个事件:

```csharp
// 手雷扔出的那一刻发一次,带上冷却总时长。UI 收到后自己倒计时。
public readonly struct GrenadeThrownEvent
{
    public readonly float Cooldown;

    public GrenadeThrownEvent(float cooldown)
    {
        Cooldown = cooldown;
    }
}
```

> **为什么不每帧广播"还剩几秒"?** 因为"事件"表达的应该是**发生了什么**(手雷被扔了),而不是**当前状态是什么**(还剩 2.3 秒)。每帧广播状态会把事件总线变成一条嘈杂的状态推送管道,订阅者一多就吵。发一次、让 UI 自己倒计时,更干净。

### 3.5 Unity 编辑器操作

**A. 做手雷预制体**

目标结构(**本体和爆炸圈是两个子物体**,原因见 3.3 末尾):

```
Grenade            ← root: Scale 必须 (1,1,1)! Rigidbody2D + Grenade.cs,没有 SpriteRenderer
├── Body           ← Square sprite, Scale (0.3, 0.3, 1), 深绿色      手雷本体
└── Explosion      ← Circle sprite, 橙色半透明, 默认 inactive         爆炸范围
```

1. Hierarchy 右键 `Create Empty`,改名 `Grenade`。确认它的 `Transform > Scale` 是 **`(1, 1, 1)`**。
   > ⚠️ **root 的 Scale 必须是 1**:子物体的 `localScale` 会被父级叠乘。root 若是 0.3,脚本给爆炸圈算出来的大小会再被乘 0.3,直接缩水成一小坨,和判定半径对不上。**缩放要放在 `Body` 子物体上,root 保持 1。**
2. `Add Component` → `Rigidbody2D`:
   - `Body Type` = **`Dynamic`**;
   - `Gravity Scale` = **`0`**(俯视角,没有重力);
   - `Linear Damping` 不用管(脚本里会设成 `drag`)。
3. **不要加 Collider2D**——手雷飞行途中不和任何东西交互,伤害完全靠爆炸瞬间的 `OverlapCircleAll`。
4. **建本体**:右键 `Grenade` → `2D Object > Sprites > Square`,改名 `Body`。
   - `Scale` 设 `(0.3, 0.3, 1)`(小一点,像颗手雷);
   - `SpriteRenderer > Color` 调成深绿或黑色。
5. **建爆炸圈**:右键 `Grenade` → `2D Object > Sprites > Circle`,改名 `Explosion`。
   - `SpriteRenderer > Color` 设成**半透明橙**(比如 RGBA = `255, 100, 0, 130`);
   - `Scale` 填多少都无所谓——**脚本会按 `explosionRadius` 覆盖它**;
   - 在 Inspector **最顶部取消勾选**(默认隐藏,爆炸时才由脚本激活)。
6. 选中 root `Grenade` → `Add Component` → `Grenade` 脚本:
   - 参数用默认值(`throwSpeed 8`、`fuseTime 1`、`explosionRadius 2.5`、`damage 40`);
   - **`Body Visual` 拖入子物体 `Body`**;
   - **`Explosion Visual` 拖入子物体 `Explosion`**。
7. 把 `Grenade` 从 Hierarchy **拖进 `Assets/Prefabs/`** 做成预制体,然后**把场景里的那个删掉**。

> 💡 想确认视觉和判定对齐:Play 时在 Scene 视图里选中飞行中的手雷,`OnDrawGizmosSelected` 会画一个**红色线框圆**(真实判定范围)。爆炸时的橙色圆应该和它**完全重合**。

**B. 做手雷对象池**

1. Hierarchy 右键 `Create Empty`,改名 `GrenadePool`。
2. `Add Component` → `ObjectPool`。
3. `Prefab` 拖入刚做好的 `Grenade` 预制体;`Prewarm Count` 设 `5`(手雷不像子弹那么频繁)。

**C. 给 Player 挂投掷器**

1. 选中 Player → `Add Component` → `GrenadeThrower`。
2. `Grenade Pool` 拖入场景里的 `GrenadePool`。
3. `Throw Point` 拖入 Player 的 `FirePoint`(和枪口共用即可;留空也行,会用玩家自己的位置)。
4. `Cooldown` 设 `3`。

> ⚠️ `PlayerInputHandler` 在 `Awake` 里 `GetComponent<GrenadeThrower>()`。你现在才把组件挂上去,**必须重新进一次 Play** 才能拿到引用(`Awake` 只在启动时跑一次)。

### ✅ 步骤 3 验收

- [ ] 按 `Q`:一颗手雷从角色朝向飞出去,飞一段后**减速滑停**。
- [ ] 大约 1 秒后**爆炸**:手雷撑大成一个橙色半透明圆(那就是爆炸范围),范围内的敌人**掉血**(40)。
- [ ] 爆炸圆很快消失(回池)。
- [ ] 冷却期间(3 秒)按 `Q` **没反应**;冷却结束后又能扔。
- [ ] **连扔两颗**,第二颗**不是**一个巨大的橙色球(如果是,说明 `OnEnable` 里的状态复位漏了)。
- [ ] 挥砍/受击期间按 `Q`,手雷会在动作结束后**补上**(缓冲队列同样对手雷生效)。
- [ ] Console 无报错。

---

## 步骤 4:技能冷却可视化

**这一步做什么**:右下角放一个手雷图标,冷却时用环形遮罩转圈。

### 4.1 新建 `Assets/Scripts/UI/CooldownUI.cs`

参考实现:`Reference/Scripts/UI/CooldownUI.cs`。

```csharp
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class CooldownUI : MonoBehaviour
    {
        [Tooltip("盖在技能图标上的遮罩;Image Type = Filled, Fill Method = Radial 360")]
        [SerializeField] private Image cooldownMask;

        private float remaining;
        private float total;

        private void OnEnable()  => EventBus.Subscribe<GrenadeThrownEvent>(OnGrenadeThrown);
        private void OnDisable() => EventBus.Unsubscribe<GrenadeThrownEvent>(OnGrenadeThrown);

        private void OnGrenadeThrown(GrenadeThrownEvent e)
        {
            total = e.Cooldown;
            remaining = e.Cooldown;
        }

        private void Update()
        {
            if (cooldownMask == null || total <= 0f) return;

            if (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                cooldownMask.fillAmount = Mathf.Clamp01(remaining / total);
            }
            else if (cooldownMask.fillAmount != 0f)
            {
                cooldownMask.fillAmount = 0f;   // 冷却完毕,遮罩收干净
            }
        }
    }
}
```

> 再次注意 `OnEnable` 订阅 / `OnDisable` **`Unsubscribe`**(第 3 周在这里栽过——写成了 `Subscribe`,编译器一个字都不会提醒你)。

### 4.2 Unity 编辑器操作

1. 右键 `Canvas` → `UI > Image`,改名 `GrenadeIcon`。
   - 锚点用 `Alt+Shift` 定到**右下角**,`Pos X = -80`、`Pos Y = 80`,`Width/Height = 64`;
   - `Source Image` 选你的 `Square`(第 3 周建的那个纯白方块);
   - `Color` 调成深绿(代表手雷)。
2. 右键 `GrenadeIcon` → `UI > Image`,改名 `CooldownMask`。
   - `Rect Transform` 锚点选 `stretch/stretch`,`Left/Right/Top/Bottom` 全设 `0`(铺满图标);
   - `Source Image` 同样选 `Square`;
   - `Color` 设成**半透明黑**(比如 RGBA = `0, 0, 0, 180`);
   - **`Image Type` = `Filled`**,**`Fill Method` = `Radial 360`**,`Fill Origin` = `Top`,`Fill Amount` 先设 `0`(初始没有冷却)。
3. 选中 `GrenadeIcon` → `Add Component` → `CooldownUI`,把 `CooldownMask` 拖进 `Cooldown Mask` 槽。
4. 保存场景。

### ✅ 步骤 4 验收

- [ ] 右下角有一个手雷图标,平时**没有遮罩**(可用状态)。
- [ ] 按 `Q` 扔出手雷的瞬间,遮罩**立刻盖满**图标,然后**像钟表一样转圈收缩**,3 秒后收干净。
- [ ] 遮罩转完(冷却结束)的那一刻,正好又能扔手雷了——UI 和实际冷却**同步**。
- [ ] 连续玩几分钟,UI 不会错位或卡住。
- [ ] Console 无报错。

---

## 常见问题排查

| 现象 | 可能原因 | 排查 |
|---|---|---|
| 移动发飘 / 感觉慢半拍 | `PlayerInputHandler` 比 `PlayerController` **晚**执行,`MoveInput` 慢一帧 | `Project Settings > Script Execution Order` 把 `PlayerInputHandler` 设成 `-100` |
| 完全不能移动 | `PlayerController.ReadMoveInput()` 删了,但没挂 `PlayerInputHandler` | Player 上 `Add Component > PlayerInputHandler` |
| 按 `Shift` 闪避没反应 | `Idle/MoveState` 的 dash 分支删了,但 `PlayerInputHandler` 没 `Enqueue(dashCommand)` | 检查 `ReadDiscreteActions()` |
| 按住 `Shift` 会连续闪避好几次 | 用了 `isPressed` 而不是 `wasPressedThisFrame`,每帧都在入队 | 离散动作必须用 `wasPressedThisFrame` |
| 缓冲不起作用(挥砍中按 Shift 无效) | `bufferDuration` 是 `0`,或 `buffer.Tick()` 没在 `Update` 里调 | 检查 Inspector 的 `Buffer Duration`;确认 `Update` 末尾有 `buffer.Tick()` |
| 按 `Q` 没反应(步骤 3 之后) | `GrenadeThrower` 组件是在 Play 之后才挂的;或 `Grenade Pool` 没拖 | **重新进一次 Play**(`Awake` 里 `GetComponent` 只跑一次);检查 Inspector 引用 |
| 第二颗手雷是个巨大的橙色球 | `OnEnable` 里没复位 `localScale` / `color` / `exploded` | 池化对象**所有会变的状态**都必须在 `OnEnable` 复位 |
| 爆炸范围显示成**矩形** | 本体和爆炸圈复用了同一个 `SpriteRenderer`(把方块本体撑大),但判定是 `OverlapCircleAll`(圆) | 拆成两个子物体:`Body`(方块)+ `Explosion`(圆),爆炸时藏前者、放后者。见步骤 3.5 A |
| 爆炸圈明显比判定范围**小一圈** | 预制体 **root 的 Scale 不是 1**,子物体的 `localScale` 被父级叠乘缩小了 | root 保持 `(1,1,1)`,把缩放放到 `Body` 子物体上 |
| 手雷扔出去不动 | `Rigidbody2D` 的 `Body Type` 不是 `Dynamic`,或 `throwSpeed` 是 0 | 检查预制体的刚体设置 |
| 手雷往下掉 | `Gravity Scale` 不是 0 | 预制体 `Rigidbody2D > Gravity Scale = 0` |
| 冷却遮罩不转 / 不出现 | `Image Type` 不是 `Filled`,或 `Fill Method` 不是 `Radial 360` | 见步骤 4.2 第 2 条;`Source Image` 为空时 `Image Type` 会被隐藏(第 3 周的坑) |

---

## 本周验收总 checklist

- [ ] 所有玩家输入统一由 `PlayerInputHandler` 采集,`PlayerController` / 状态类 / `WeaponController` **都不再读键盘鼠标**。
- [ ] 四个命令(`Move`/`Attack`/`Dash`/`Grenade`)实现同一个 `ICommand` 接口。
- [ ] 输入缓冲生效:挥砍期间按 `Shift`,挥砍结束立刻闪避;`bufferDuration = 0` 时输入被丢弃(对比确认)。
- [ ] 按 `Q` 扔手雷:飞行 → 滑停 → 引信 → 范围爆炸 → 回池,连扔不出现"巨型橙球"。
- [ ] 手雷冷却 UI 环形遮罩与实际冷却同步。
- [ ] 第 1~3 周的功能(移动/闪避/射击/切枪/近战/受击/血条/弹药/拾取)全部未被破坏。

## 课后练习(选做,但很值得)

1. **`SwitchWeaponCommand`**:把切枪也做成命令(带一个 `index` 参数)。这会让你想清楚一个问题:**带参数的命令,实例还能复用吗?** (提示:可以为每把武器各建一个命令实例,或者像 `MoveCommand` 那样用 `SetIndex`。)
2. **在屏幕上显示缓冲队列长度**:`PlayerInputHandler` 已经暴露了 `Buffer` 属性(`buffer.Count`)。做一个 Debug 文本显示它,你就能**亲眼看到**命令在队列里排队、执行、过期的全过程——对理解这套机制帮助极大。
3. **闪避冷却也做个 UI**:`PlayerController` 已经有 `CanDash` 和 `dashCooldown` 了,照着 `CooldownUI` 再做一个。会遇到一个设计问题:**要不要为闪避也发一个事件?** 还是把 `CooldownUI` 改成通用的(能接受任意技能的冷却事件)?

## 下周预告:第 5 周 - 房间生成与关卡流程

`RoomConfig`(ScriptableObject)数据驱动房间的敌人/道具布局,简单工厂生成敌群;房间清空后开门、切换到下一个房间。房间切换、敌群刷新的通知会走第 3 周建好的 `EventBus`;摄像机过渡计划引入 Cinemachine(需要先通过 Package Manager 安装)。
