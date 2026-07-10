# CLAUDE.md

> 本文件面向在本仓库中协作的 Claude Code:每次新对话开始时应先读取本文件,用以恢复项目背景、当前进度与既定约定,避免在多次对话之间产生不一致的假设或重复造轮子。
> 若本文件描述与代码库实际状态冲突,**以代码库当前状态为准**;发现冲突或完成里程碑后,请顺手更新本文件对应章节,让下一次对话能无缝衔接。

## 项目一句话简介

俯视角 2D Roguelike 射击游戏(《元气骑士》风格),基于 Unity,目标是用 6 周时间跑通一套低耦合、可迁移的核心架构(V1 2D → V2 3D → V3 联机)。完整背景、六周计划、技术选型见 [README.md](README.md)。

## 当前进度(务必保持最新)

- 状态:**第 1、2 周均已完成并通过验收;第 1 周打了 `v1-week1` 标签,第 2 周是否打 `v1-week2` 待用户确认**。
- 已有内容:
  - Unity 6000.3.19f1 + URP 2D 模板默认工程,`.gitignore`/`.gitattributes` 已提交。
  - 第 1 周:`Assets/Scripts/Core|Entities|Weapons` 下的 8 个文件、场景搭建均已由用户完成并通过验收,详见 `devlog/week1.md`。
  - 第 2 周:`Assets/Scripts/StateMachines/`(`IState.cs`、`StateMachine.cs`、`Player/`、`Enemy/` 共 11 个状态相关文件)已由用户对照 `Reference/Scripts/...` 创建完成;`Health.cs`/`PlayerController.cs`/`EnemyController.cs`/`PlayerShooter.cs` 已重写/更新。核心变化:`PlayerController`/`EnemyController` 从"一个脚本管所有逻辑"重构成状态机的"上下文",`Health` 新增无敌帧和 `Damaged`/`Died` 两个 C# 事件(EventBus 正式引入前的雏形)。场景结构未变,这周没有新增 GameObject/预制体。
  - Play 模式验收通过:闪避 + 无敌帧、近战攻击范围判定、受击闪红 + 无敌帧、敌人 Patrol/Chase/Attack/Dead 四状态切换(含调试用颜色反馈)、状态期间禁止开枪均正常。验收中手敲引入的 bug(状态比较类型不兼容、闪避冷却减法写成赋值、判空条件 `&&`/`||` 写反、`canFire` 算出来没接进判断条件等)已修复,详见 `devlog/week2.md` 第 5 节。
  - 验收通过后又发现并修复两个 bug(`devlog/week2.md` 第 5.2 节):① 静止被敌人撞到后获得恒定残留速度、一直漂——根因是 Dynamic 刚体 + `MovePosition` 不清 `linearVelocity`,修复是在 `PlayerController.FixedUpdate` 开头加一行 `Rb.linearVelocity = Vector2.zero`;② 受击"闪白"看不见——角色本身就是白色 sprite、白配白,已把 `PlayerHurtState` 闪烁色改成 `Color.red`。两处均由用户手动改进 `Assets/`。
  - 命名约定新增一条:代表血量组件的公开属性用小写 `health`(用户主动选择,不是笔误),见下方「目录结构约定」。
- 尚未创建:`Assets/Scripts/Commands/`、`Assets/Scripts/UI/`、`Assets/Data/`(第 3 周及以后按需创建)。
- 下一步:开始第 3 周——事件系统与数据驱动武器(正式引入 EventBus、武器 ScriptableObject、策略模式切换武器、弹药限制)。

**更新规则**:每完成一项里程碑(一周任务,或用户认可的阶段性成果)后:
1. 更新本节的"已有内容 / 尚未创建 / 下一步";
2. 在 `devlog/week<N>.md` 写入该周的**实际完成记录**(不是计划,是发生了什么);
3. 若达到 README 中周任务的验收标准,同步更新 README 底部"开发日志与标签"表,并询问用户是否要打 `v1-week<N>` 标签(打标签会写入共享历史,先确认再执行)。

## 当前代码结构快照(截至第 2 周末,写代码前速查)

> "代码实际长什么样"的速查表,方便新对话快速定位。真实进度以 `Assets/` 为准;下面每条都对应已经落地的文件。第 3 周起有新增/重构时同步更新本节。

**脚本清单(`Assets/Scripts/`,共 19 个 `.cs`)**

- `Core/`
  - `GameManager.cs`——单例(`Instance`),`[SerializeField] player` 只读暴露为 `Player`。目前很轻,只做单例 + Player 引用。
  - `ObjectPool.cs`——通用对象池(`Queue<GameObject>` + `prewarmCount` 预热),`Get(pos,rot)`/`Release`;同文件定义 `IPoolable` 接口(池化对象持有回自己池的引用,取出时自动回填)。
  - `CameraFollow.cs`——`LateUpdate` 跟随 `target` + `offset`,锁 Z。
- `Entities/`
  - `Health.cs`——血量组件。`Current`/`isDead`/`isInvincible`(均小写开头),`SetInvincible(duration)` 无敌帧(`Update` 里倒计时),`TakeDamage`(无敌或已死直接跳过),`event Action<int> Damaged` + `event Action Died`(EventBus 雏形);死亡时先广播 `Died` 再 `SetActive(false)`。
  - `PlayerController.cs`——玩家状态机"上下文":持有 `Rb`/`health`/`SpriteRenderer`/`MoveInput` + 5 个状态实例 + `stateMachine`;`Update` 采集输入(WASD/方向键)+ 朝向鼠标 + 跑 `Tick`,`FixedUpdate` **先 `Rb.linearVelocity = Vector2.zero` 再跑 `FixedTick`**;暴露 `CanAct`(仅 Idle/Move 为真,供开火判断)、`CanDash`、各状态时长/数值参数。
  - `EnemyController.cs`——敌人状态机"上下文":持有 4 个状态 + `SpawnPosition`(出生点,巡逻围绕它)+ `Player` 引用(`Start` 里 `FindGameObjectWithTag("Player")`);`MoveTowards`/`DistanceToPlayer` 工具方法。
- `Weapons/`
  - `Bullet.cs`——`IPoolable` 子弹,`FixedUpdate` 用 `linearVelocity` 前进 + 计时回收,`OnTriggerEnter2D` 命中 `Enemy` 扣血后回池。
  - `PlayerShooter.cs`——左键 `isPressed` 连发,受 `fireCooldown` 与 `playerController.CanAct` 限制,从 `bulletPool.Get` 取子弹。
- `StateMachines/`
  - `IState.cs`(Enter/Tick/FixedTick/Exit)+ `StateMachine.cs`(`CurrentState`/`ChangeState`/`Tick`/`FixedTick`,**纯 C# 类,非 MonoBehaviour**,controller 内部 `new` 一个)。
  - `Player/`:Idle / Move / Dash / Attack / Hurt 五态。 `Enemy/`:Patrol / Chase / Attack / Dead 四态。

**几个已确立的实现事实/约定(改代码前注意)**

1. **状态机 = 上下文模式**:`PlayerController`/`EnemyController` 只持有数据 + 每帧转发 `Tick`/`FixedTick`,具体行为在状态类里;状态类构造函数吃 `(controller, stateMachine)`,通过 controller 读写数据。新增状态 = 新建一个类 + 在 controller 的 `Awake` 里 `new` 出来 + 暴露成属性。
2. **玩家/敌人移动 = `MovePosition` + 每帧清零 velocity**:两者刚体都是 Dynamic;靠 `MovePosition` 位移。玩家在 `FixedUpdate` 开头 `Rb.linearVelocity = Vector2.zero` 消除碰撞残留速度(否则被撞会一直漂,见 week2 第 5.2 节的坑)。改动移动/物理时别破坏这个前提;之后若要"击退"效果,需要显式设计,不能靠残留速度。
3. **事件驱动的状态切换**:`Health.Damaged` → `PlayerController.OnDamaged` → 切 `HurtState`;`Health.Died` → `EnemyController.OnDied` → 切 `DeadState`。controller 在 `OnEnable/OnDisable` 订阅/退订。这套 C# 事件是第 3 周正式 EventBus 的雏形,届时会迁移。
4. **伤害判定分三条路**:玩家近战 = `PlayerAttackState.Enter` 瞬间 `Physics2D.OverlapCircleAll` 一次;玩家远程 = `Bullet.OnTriggerEnter2D`;敌人 = `EnemyAttackState` 距离判定 + `attackCooldown` 周期扣血(**不走物理碰撞回调**)。
5. **调试用颜色反馈(临时)**:各状态 `Enter` 改 `SpriteRenderer.color`(玩家攻击黄、受击红闪;敌人巡逻白/追击橙/攻击红/死亡灰),`Exit` 负责还原。有动画后会替换掉,状态机逻辑不依赖颜色。
6. **输入 API**:一次性动作(闪避/近战)用 `wasPressedThisFrame`,持续动作(开火/移动)用 `isPressed`。全部走新版 Input System(`Keyboard.current`/`Mouse.current`)。

**场景(`SampleScene.unity`)关键物体**:Player(Tag `Player`,挂 Controller/Health/Shooter/Rigidbody2D[Dynamic,Damping 0]/SpriteRenderer[白])、Enemy(Tag `Enemy`)、子弹对象池(挂 `ObjectPool`)、GameManager、Camera(挂 `CameraFollow`)。第 2 周没有新增 GameObject/预制体。

## 协作背景与文档要求(重要,长期有效)

- 用户背景:**这是用户的第一个 Unity 个人项目**,对 Unity 编辑器操作、C#、游戏开发相关技术都不熟悉,属于边做边学。不要假设用户知道任何"理所当然"的操作或术语。
- 因此每一周的任务产出,不能只给代码或简单的任务清单,必须尽可能详细,具体包括:
  1. **这一周做什么、为什么这么做**——不只是步骤,也要讲设计意图,帮助用户建立理解而不是照抄。
  2. **Unity 编辑器内的具体操作步骤**——菜单路径、Inspector 里要设置哪些字段、GameObject/组件层级怎么搭、预制体怎么建,都要写清楚,不能一句"在编辑器里配置好"带过。
  3. **完整、可直接使用的代码**——给完整脚本而不是片段或伪代码,并对关键部分做讲解(为什么这么写、对应到架构约定里的哪个模式)。
  4. 这些详细说明写入 `devlog/week<N>.md` 或对话中皆可,以用户方便查阅为准;`devlog` 里同时要保留"实际完成了什么"的记录(见上一节更新规则)。
- 用户在过程中会提出较多问题(概念不懂、报错、操作不确定等),应耐心详细解答,必要时补充背景概念,不要用一两句话打发。
- 这条约定不是第 1 周专属,**后续每一周都按此标准执行**,除非用户明确说不需要这么详细了。

## Reference 参考代码约定(重要,长期有效)

- **Claude 不直接把游戏代码写进 `Assets/` 里**。用户希望自己动手在 `Assets/Scripts/` 下创建文件、亲自敲代码来学习,而不是打开项目发现代码已经全部写好。
- Claude 的示例/参考实现统一写在仓库根目录的 **`Reference/`** 文件夹——与 `Assets/` 同级(不在其内部),已加入 `.gitignore`,不会被 Unity 编译、也不会进版本库。目录结构镜像 `Assets/Scripts/`,例如 `Reference/Scripts/Core/ObjectPool.cs` 对应用户将来要在 `Assets/Scripts/Core/ObjectPool.cs` 创建的内容。
- 每周文档(`devlog/week<N>.md`)里的代码讲解章节,标题指向 `Assets/Scripts/...`(目标路径),正文提示参考实现在 `Reference/Scripts/...`,并说明这是需要用户自己创建的文件,不是已经落地的实现。
- `Reference/` 里的内容会随周数推进被覆盖/更新,只代表"当前这一步应该长成什么样",不代表用户项目的真实进度——**真实进度以 `Assets/` 里用户实际创建的文件为准**。
- 例外:场景文件(`.unity`)、预制体(`.prefab`)等 Unity 二进制/YAML 资产不适合放参考副本,继续沿用「Claude 不直接编辑场景文件,由用户在编辑器里操作」的既有约定。

## 开发节奏

- 当前阶段:**第 2 周已完成,准备进入第 3 周 - 事件系统与数据驱动武器**(见 README「六周开发路线」)。
- 第 3 周目标产出:EventBus 接入(UI 血条/子弹数事件更新)、武器 ScriptableObject(伤害/冷却/子弹类型)、策略模式武器切换(手枪/步枪/近战——把第 2 周写在 `PlayerAttackState` 里的近战原型和现有远程射击统一到 `IWeaponStrategy` 接口下)、弹药限制 + 补给拾取。

## 架构约定(写代码前必读)

以下模式取自 README 的技术架构表,是本项目预设的实现方式。新增系统前先检查能否复用/遵循这些模式,而不是引入新的替代方案:

| 模块 | 约定 | 落地要点 |
|---|---|---|
| Game Loop | Update / FixedUpdate 明确分离 | 物理与位移逻辑放 FixedUpdate,输入采集与表现类逻辑放 Update,不要混用 |
| 状态机 (FSM) | 玩家/敌人用枚举+Switch 或状态类驱动 | 优先状态类(每个状态一个类)便于后续扩展,状态很少时才用枚举+Switch |
| 事件总线 (EventBus) | 轻量观察者模式 | UI、血量、拾取等跨模块通信走 EventBus,模块间禁止相互持有硬引用 |
| 命令模式 | 输入封装为 Command 对象 | 支持输入缓冲队列,为后续连招/回放留接口 |
| 对象池 | 子弹/特效/敌人通用对象池 | 频繁 Instantiate/Destroy 的对象一律走对象池,不直接 New/Destroy |
| 武器策略 | `IWeaponStrategy` 接口 + ScriptableObject 数据 | 新增武器只加数据和一个策略实现,不改主体逻辑 |
| 房间生成 | `RoomConfig`(ScriptableObject)+ 简单工厂 | 房间/敌群配置数据驱动,不写死在场景里 |
| 资源管理 | V1 用 Resources,V2 再迁移 Addressables | 现阶段不要提前引入 Addressables |

## 目录结构约定

按 README 的规划创建目录(当前均不存在,首次用到时再创建):

```
Assets/Scripts/Core/          # GameManager, EventBus, ObjectPool...
Assets/Scripts/Entities/      # 玩家、敌人、NPC
Assets/Scripts/Weapons/       # 武器接口、策略、SO 定义
Assets/Scripts/Commands/      # 命令模式相关类
Assets/Scripts/StateMachines/ # 状态机实现
Assets/Scripts/UI/            # UI 控制脚本
Assets/Prefabs/  Assets/Scenes/  Assets/Data/  Assets/Art/  Assets/Audio/  Assets/ThirdParty/
```

新脚本按职责放入对应目录,不要都堆在 `Assets/Scripts` 根目录。

**命名空间约定**(第 1 周确立):子目录与命名空间一一对应——`Game.Core`、`Game.Entities`、`Game.Weapons`、`Game.Commands`、`Game.StateMachines`、`Game.UI`。看 `using` 就能判断这个类归哪个目录管,新文件按此规则加命名空间。

**属性命名约定**(第 2 周确立,用户明确要求):`PlayerController`/`EnemyController` 上暴露 `Health` 组件引用的公开属性用**小写开头**的 `health`(不是 C# 惯例的 `Health`)。这是用户主动选择的风格,不是笔误,新代码(包括 Claude 给的参考实现)一律跟随这个写法,不要擅自"改正"回 PascalCase。

## 工程与版本约定

- 引擎版本锁定 **6000.3.19f1**,不要因为个人环境不同而修改 `ProjectSettings/ProjectVersion.txt`。
- 使用新版 Input System(`com.unity.inputsystem`),不要引入旧版 `UnityEngine.Input` API。
- 渲染管线为 URP 2D,特效/Shader 工作基于 URP 2D Renderer。
- 目前未安装 Cinemachine 和 Addressables(README 第 5 周计划用 Cinemachine 做摄像机过渡)。用到时需先通过 Package Manager 添加,并在本文件和 README「运行要求」里补充说明。

## Git / 协作约定

- 分支:`main`(稳定,仅接受 PR)、`dev`(开发主线)、`feature/<功能名>`。
- 场景(`.unity`)与预制体(`.prefab`)冲突由 UnityYAMLMerge 处理;自动合并失败时在编辑器里手动解决后再提交,不要用命令行强行二选一。
- 根目录已有 `.gitignore`(忽略 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、IDE 生成文件等)与 `.gitattributes`(LFS 规则 + `merge=unityyamlmerge` 属性)。**这两个文件本身已提交**,但 UnityYAMLMerge 的合并驱动路径是本机配置,不会随仓库同步——每台开发机克隆后需按 README「冲突处理」一节各自执行一次 `git config merge.unityyamlmerge.driver ...`。
- 首次在新机器上使用前仍需执行 `git lfs install`(注册全局 LFS 过滤器),这一步不属于仓库内容,不能靠 `.gitattributes` 自动完成。
- **`git commit` 一律由用户自己执行**(第 2 周确立,长期有效):Claude 可以 `git add`、准备好提交信息、告诉用户要跑什么命令,但不要代替用户执行 `git commit`。打 tag(不涉及重写历史)、`git status`/`git log` 这类只读或低风险操作不受此限制。

## 给协作者的提示

- README.md 面向人(项目目标、周计划、参考资料),CLAUDE.md 面向 Claude(当前进度、既定约定、踩过的坑)。改变项目目标/计划改 README;改变当前进度/约定/注意事项改本文件。
- 项目仍处于起步阶段:任何"架构约定"一旦被用户在对话中修改或否决,应立即更新本文件对应表格/条目,避免下次对话重复同样的分歧。
