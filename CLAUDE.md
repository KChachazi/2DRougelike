# CLAUDE.md

> 本文件面向在本仓库中协作的 Claude Code:每次新对话开始时应先读取本文件,用以恢复项目背景、当前进度与既定约定,避免在多次对话之间产生不一致的假设或重复造轮子。
> 若本文件描述与代码库实际状态冲突,**以代码库当前状态为准**;发现冲突或完成里程碑后,请顺手更新本文件对应章节,让下一次对话能无缝衔接。

## 项目一句话简介

俯视角 2D Roguelike 射击游戏(《元气骑士》风格),基于 Unity,目标是用 6 周时间跑通一套低耦合、可迁移的核心架构(V1 2D → V2 3D → V3 联机)。完整背景、六周计划、技术选型见 [README.md](README.md)。

## 当前进度(务必保持最新)

- 状态:**第 1、2、3、4 周均已完成并通过验收;`v1-week1`/`v1-week2`/`v1-week3` 标签已打**。
- 已有内容:
  - Unity 6000.3.19f1 + URP 2D 模板默认工程,`.gitignore`/`.gitattributes` 已提交。
  - 第 1 周:`Assets/Scripts/Core|Entities|Weapons` 下的 8 个文件、场景搭建均已由用户完成并通过验收,详见 `devlog/week1.md`。
  - 第 2 周:`Assets/Scripts/StateMachines/`(`IState.cs`、`StateMachine.cs`、`Player/`、`Enemy/` 共 11 个状态相关文件)已由用户对照 `Reference/Scripts/...` 创建完成;`Health.cs`/`PlayerController.cs`/`EnemyController.cs`/`PlayerShooter.cs` 已重写/更新。核心变化:`PlayerController`/`EnemyController` 从"一个脚本管所有逻辑"重构成状态机的"上下文",`Health` 新增无敌帧和 `Damaged`/`Died` 两个 C# 事件(EventBus 正式引入前的雏形)。场景结构未变,这周没有新增 GameObject/预制体。
  - Play 模式验收通过:闪避 + 无敌帧、近战攻击范围判定、受击闪红 + 无敌帧、敌人 Patrol/Chase/Attack/Dead 四状态切换(含调试用颜色反馈)、状态期间禁止开枪均正常。验收中手敲引入的 bug(状态比较类型不兼容、闪避冷却减法写成赋值、判空条件 `&&`/`||` 写反、`canFire` 算出来没接进判断条件等)已修复,详见 `devlog/week2.md` 第 5 节。
  - 验收通过后又发现并修复两个 bug(`devlog/week2.md` 第 5.2 节):① 静止被敌人撞到后获得恒定残留速度、一直漂——根因是 Dynamic 刚体 + `MovePosition` 不清 `linearVelocity`,修复是在 `PlayerController.FixedUpdate` 开头加一行 `Rb.linearVelocity = Vector2.zero`;② 受击"闪白"看不见——角色本身就是白色 sprite、白配白,已把 `PlayerHurtState` 闪烁色改成 `Color.red`。两处均由用户手动改进 `Assets/`。
  - 命名约定新增一条:代表血量组件的公开属性用小写 `health`(用户主动选择,不是笔误),见下方「目录结构约定」。
  - 第 3 周:分 4 步全部完成并通过 Play 验收(EventBus + 血条 → 数据驱动武器 → 近战收编 → 弹药 UI + 拾取)。新增 9 个脚本(`Core/EventBus`、`Core/GameEvents`、`Weapons/` 下 6 个武器系统文件、`UI/` 下 2 个)、修改 6 个(`Health`、`PlayerController`、`Bullet`、玩家 Idle/Move/Attack 三个状态);新增 `Assets/Data/` 下三个武器 SO、Canvas(血条 + TMP 弹药文本)、`AmmoPickup.prefab`、`Assets/Art/Square` sprite。详见 `devlog/week3.md`。
  - 第 3 周抓到两个**"程序照常跑、验收照常过"的沉默 bug**(`devlog/week3.md`「实际完成记录」第 4、5 条):① `WeaponController` 广播武器名时用了 `CurrentWeapon.name`(SO 的**资产文件名**)而非 `weaponName` 字段——因资产恰好同名而被完全掩盖;② `HealthBarUI.OnDisable` 把 `Unsubscribe` 写成 `Subscribe`,退订变成重复订阅,因血条从未被禁用而不显形。两者都已修正。**这类 bug 测不出来,只能 review 代码抓——以后每周验收后仍要过一遍代码。**
  - 第 4 周:分 4 步全部完成并通过 Play 验收(命令模式重构 → 体会输入缓冲 → 手雷 → 冷却 UI),**外加三个课后练习全做了**(`SwitchWeaponCommand`、`DebugText` 可视化缓冲队列、`CooldownUI` 通用化)。新增 11 个脚本(`Commands/` 8 个、`Weapons/Grenade`+`GrenadeThrower`、`UI/CooldownUI`+`DebugText`)、修改 5 个;新增 `Grenade.prefab`、`GrenadePool`、技能图标 + 环形冷却 UI。详见 `devlog/week4.md`。
  - 第 4 周又抓到三个沉默 bug(`devlog/week4.md`「踩过的坑」4/5/6):① `InputBuffer.Empty()` 写成 `Count > 0`(语义反了);② **`CooldownUI.OnDisable` 又把 `Unsubscribe` 写成 `Subscribe`——和第 3 周 `HealthBarUI` 一模一样的错误,同一个坑踩了两次**;③ `SwitchWeaponCommand` 的 `index <= WeaponCount` off-by-one。**验收后 review 代码这条规矩必须保留。**
- 目录已全部落地:`Core`/`Entities`/`Weapons`/`StateMachines`/`UI`/`Commands` + `Data`/`Art`/`Prefabs`。
- 下一步:第 5 周——房间生成与关卡流程(`RoomConfig` SO + 简单工厂 + 房间切换;计划引入 Cinemachine,需先装包)。

**更新规则**:每完成一项里程碑(一周任务,或用户认可的阶段性成果)后:
1. 更新本节的"已有内容 / 尚未创建 / 下一步";
2. 在 `devlog/week<N>.md` 写入该周的**实际完成记录**(不是计划,是发生了什么);
3. 若达到 README 中周任务的验收标准,同步更新 README 底部"开发日志与标签"表,并询问用户是否要打 `v1-week<N>` 标签(打标签会写入共享历史,先确认再执行)。

## 当前代码结构快照(截至第 4 周末,写代码前速查)

> "代码实际长什么样"的速查表,方便新对话快速定位。真实进度以 `Assets/` 为准;下面每条都对应已经落地的文件。第 5 周起有新增/重构时同步更新本节。

**脚本清单(`Assets/Scripts/`,共 39 个 `.cs`)**

- `Core/`
  - `GameManager.cs`——单例(`Instance`),`[SerializeField] player` 只读暴露为 `Player`。目前很轻,只做单例 + Player 引用。
  - `ObjectPool.cs`——通用对象池(`Queue<GameObject>` + `prewarmCount` 预热),`Get(pos,rot)`/`Release`;同文件定义 `IPoolable` 接口(池化对象持有回自己池的引用,取出时自动回填)。
  - `CameraFollow.cs`——`LateUpdate` 跟随 `target` + `offset`,锁 Z。
  - **`EventBus.cs`**(第 3 周)——`static` 泛型事件总线,`Dictionary<Type, Delegate>` 按事件类型分发;`Subscribe<T>`/`Unsubscribe<T>`/`Publish<T>`/`Clear`,外加 `[RuntimeInitializeOnLoadMethod]` 在进 Play 时清空(防 domain reload 关闭时 static 残留)。
  - **`GameEvents.cs`**(第 3 周,第 4 周扩充)——`readonly struct` 事件集中定义:`PlayerHealthChangedEvent(Current,Max)`、`AmmoChangedEvent(Current,Max)`(**`Max = -1` 约定为无限弹药**)、`WeaponChangedEvent(WeaponName)`、**`SkillCooldownStartedEvent(SkillId, Cooldown)`**(第 4 周)。同文件还有 **`SkillId` 枚举**(`Dash`/`Grenade`)——**技能标识用枚举不用字符串**(编译期检查 + Inspector 下拉,不会拼错)。新增跨模块通知就在这里加 struct。
- `Entities/`
  - `Health.cs`——通用血量组件。`Current`/`Max`/`isDead`/`isInvincible`,`SetInvincible(duration)` 无敌帧,`TakeDamage`(无敌或已死直接跳过);三个 C# 事件:`Damaged(int)`、`Died()`、**`HealthChanged(current,max)`**(第 3 周加)。**Health 保持通用、不认识"玩家"/"UI",只发本地事件**。
  - `PlayerController.cs`——玩家状态机"上下文":持有 `Rb`/`health`/`SpriteRenderer`/`MoveInput` + 5 个状态实例 + `stateMachine`;`Update` 只做**朝向鼠标 + 冷却 + `Tick`**(**第 4 周起不再读键盘**),`FixedUpdate` **先 `Rb.linearVelocity = Vector2.zero` 再跑 `FixedTick`**。供外部调用的入口:`SetMoveInput(v)`(由 `MoveCommand` 写入)、`TriggerDash()`(由 `DashCommand` 调)、`TriggerAttack()`(由 `WeaponController` 近战时调);只读暴露 `CanAct`(仅 Idle/Move 为真)、`CanDash`。**它还负责把 `health.HealthChanged` 桥接成全局 `PlayerHealthChangedEvent`**(`Start` 广播一次初始血量),并在 `StartDashCooldown()` 里广播 `SkillCooldownStartedEvent(SkillId.Dash, ...)`。
  - `EnemyController.cs`——敌人状态机"上下文":持有 4 个状态 + `SpawnPosition` + `Player` 引用;`MoveTowards`/`DistanceToPlayer` 工具方法。
- `Weapons/`(第 3 周重构成"数据 + 策略")
  - `WeaponData.cs`——武器 SO(`[CreateAssetMenu]` → `Create > Game > Weapon Data`),字段 `weaponName`/`type`/`damage`/`cooldown`/`maxAmmo`/`range`,外加 `WeaponType { Ranged, Melee }` 枚举。资产在 `Assets/Data/`:`Pistol`/`Rifle`/`Sword`。
  - `IWeaponStrategy.cs`——`void Fire(WeaponController controller, WeaponData data)`。**策略无状态**,冷却/弹药由 controller 管,数值从 data 读。
  - `RangedWeaponStrategy.cs`——从 `controller.BulletPool` 取子弹、按 `FirePoint` 朝向发射,并 `bullet.SetDamage(data.damage)`。
  - `MeleeWeaponStrategy.cs`——角色前方 `range` 处 `OverlapCircleAll(range)`,圈内 `Enemy` 扣血(逻辑从第 2 周 `PlayerAttackState.PerformHit` 搬来)。
  - `WeaponController.cs`——**武器系统主体**(挂 Player,取代已移除的 `PlayerShooter` 组件)。持有 `WeaponData[] weapons` + `int[] currentAmmo`(每把武器各记一份,切枪不清零)+ `Dictionary<WeaponType, IWeaponStrategy>`。**第 4 周起不再读输入**:`Update` 里只剩冷却倒计时,对外暴露**能力**——`CanFire()`(= `CanAct && cooldown<=0 && HasAmmo()`)、`Fire()`、`SwitchTo(index)`、`AddAmmo(n)`、`WeaponCount`。近战开火后额外 `playerController.TriggerAttack()`。弹药/武器变化通过 `EventBus` 广播,**它不认识 UI**。
  - `Bullet.cs`——`IPoolable` 子弹,`FixedUpdate` 用 `linearVelocity` 前进 + 计时回收,`OnTriggerEnter2D` 命中 `Enemy` 扣血后回池;**`SetDamage(int)` 让武器 SO 覆盖伤害**(所以不需要为每把枪做子弹预制体)。
  - `AmmoPickup.cs`——`OnTriggerEnter2D` 碰到 `Player` → `WeaponController.AddAmmo(amount)` → `Destroy`。无限弹药武器(近战)会被 `AddAmmo` 直接跳过。
  - **`Grenade.cs`**(第 4 周)——`IPoolable` 手雷。`OnEnable` 给初速 + 阻尼,引信烧完 `Explode()`:`OverlapCircleAll` 范围伤害(**非指向性,不需要 Collider2D**),然后藏本体、放出爆炸圈、计时回池。**表现是两个子物体**(`Body` 方块 / `Explosion` 圆),爆炸圈大小由脚本按 `explosionRadius` 算(`localScale = 半径 × 2`),**保证视觉圆 == 判定圆**。
  - **`GrenadeThrower.cs`**(第 4 周,挂 Player)——只管冷却 + 从池里取一颗手雷丢出去;`Throw()` 里广播 `SkillCooldownStartedEvent(SkillId.Grenade, cooldown)`。
  - `PlayerShooter.cs`——**已废弃**,组件已从 Player 移除,文件保留未删。新代码不要再用它。
- **`Commands/`**(第 4 周新建,命名空间 `Game.Commands`)
  - `ICommand.cs`——`bool CanExecute()` + `void Execute()`。**两者分开是为了"先问后做"——输入缓冲全靠这个。**
  - `InputBuffer.cs`——**纯 C# 类**(由 `PlayerInputHandler` 内部 `new`)。`Queue<BufferedCommand>`,三条规则:**过期就丢**(`Time.time > ExpireTime`)、**不能执行就留在队里等**、**一帧最多执行一个**。另有 `Count`/`Empty()`/`Peek()`(供 Debug UI 用)。
  - `MoveCommand`/`AttackCommand`/`DashCommand`/`GrenadeCommand`/`SwitchWeaponCommand`——命令实例**只 new 一次、反复复用**(避免每帧 GC)。
  - `PlayerInputHandler.cs`(挂 Player)——**全项目唯一读键盘鼠标的地方**。**持续动作**(移动/按住左键连发)每帧直接 `Execute()`,**不入队**;**离散动作**(闪避 `Shift` / 手雷 `Q`)`buffer.Enqueue(...)` 排队;**切枪**(1/2/3)立即执行。`Update` 末尾 `buffer.Tick()`。
- `UI/`(第 3 周新建)
  - `HealthBarUI.cs`——订阅 `PlayerHealthChangedEvent`,设 `Image.fillAmount = (float)Current/Max`。**对 Player/Health 零引用**。
  - `AmmoUI.cs`——订阅 `AmmoChangedEvent` + `WeaponChangedEvent`(两个事件分别到达,各自缓存后 `Refresh()` 重拼文本);`Max < 0` 显示 `∞`。
  - **`CooldownUI.cs`**(第 4 周)——**通用技能冷却环形遮罩**。`[SerializeField] SkillId skill` 决定自己盯哪个技能,订阅 `SkillCooldownStartedEvent` 后 `if (e.Skill != skill) return;` 过滤。**一个脚本服务任意技能**:加新技能只需加枚举值 + 挂个组件选中它,UI 代码不改。收到事件后**自己倒计时**(不靠每帧广播)。
  - **`DebugText.cs`**(第 4 周,调试用)——显示输入缓冲队列长度 + 队首命令名,`display` 开关控制显隐。
- `StateMachines/`
  - `IState.cs`(Enter/Tick/FixedTick/Exit)+ `StateMachine.cs`(`CurrentState`/`ChangeState`/`Tick`/`FixedTick`,**纯 C# 类,非 MonoBehaviour**,controller 内部 `new` 一个)。
  - `Player/`:Idle / Move / Dash / Attack / Hurt 五态。**状态类已彻底不读输入**(第 3 周删了右键近战分支和 `PerformHit`,第 4 周删了读 Shift 的闪避分支),现在只根据当前数据决定状态转换。 `Enemy/`:Patrol / Chase / Attack / Dead 四态。

**几个已确立的实现事实/约定(改代码前注意)**

1. **状态机 = 上下文模式**:`PlayerController`/`EnemyController` 只持有数据 + 每帧转发 `Tick`/`FixedTick`,具体行为在状态类里;状态类构造函数吃 `(controller, stateMachine)`。新增状态 = 新建一个类 + 在 controller 的 `Awake` 里 `new` 出来 + 暴露成属性。
2. **玩家/敌人移动 = `MovePosition` + 每帧清零 velocity**:两者刚体都是 Dynamic;靠 `MovePosition` 位移。玩家在 `FixedUpdate` 开头 `Rb.linearVelocity = Vector2.zero` 消除碰撞残留速度(否则被撞会一直漂,见 week2 第 5.2 节)。之后若要"击退",需显式设计,不能靠残留速度。
3. **两层事件,别混用**:**局部 C# 事件**(`Health.Damaged`/`Died`/`HealthChanged`)用于**同一实体内部**的反应——`Damaged` → 切 `HurtState`、`Died` → 切 `DeadState`,controller 在 `OnEnable/OnDisable` 订阅/退订;**全局 `EventBus`** 用于**跨模块**通知(UI/拾取/武器)。`Health` 这种通用组件只发局部事件,"我是玩家、我的血要给 UI 看"这层身份由 `PlayerController` 桥接后 `EventBus.Publish`。**新代码遵循这个分层,别让通用组件直接 Publish 全局事件。**
4. **EventBus 时序铁律**:**订阅放 `OnEnable`、初始值广播放 `Start`**(Unity 保证所有 `OnEnable` 先于所有 `Start`),否则 UI 收不到初始值。**`Subscribe`/`Unsubscribe` 必须成对**——写反了编译器不报错,但会累积重复订阅、切场景时抱着已销毁对象崩溃(week3 踩过)。
5. **武器 = SO 数据 + 无状态策略**:新增一把武器 = 建一个 `WeaponData` 资产;新增一种开火方式 = 加一个 `IWeaponStrategy` 实现 + 在 `WeaponController.Awake` 的字典里注册。**不要把冷却/弹药状态塞进策略,也不要把开火行为塞进 SO。** 从 SO 取显示名用 `weaponName` 字段,**不是 `.name`**(那是资产文件名,week3 踩过)。
6. **伤害判定分三条路**:玩家近战 = `MeleeWeaponStrategy` 的 `OverlapCircleAll`;玩家远程 = `Bullet.OnTriggerEnter2D`(伤害由 `SetDamage` 从武器 SO 注入);敌人 = `EnemyAttackState` 距离判定 + `attackCooldown` 周期扣血(**不走物理碰撞回调**)。
7. **调试用颜色反馈(临时)**:各状态 `Enter` 改 `SpriteRenderer.color`(玩家攻击黄、受击红闪;敌人巡逻白/追击橙/攻击红/死亡灰),`Exit` 还原。有动画后会替换掉,状态机逻辑不依赖颜色。
8. **输入全部收拢在 `PlayerInputHandler`**(第 4 周确立):`PlayerController`、状态类、`WeaponController` **一律不读键盘鼠标**,它们只对外暴露"能力"(`TryXxx`/`CanXxx`),由命令来调。新增一个玩家动作 = 加一个 `ICommand` 实现 + 在 `PlayerInputHandler` 里绑定按键。**别再在别的地方写 `Keyboard.current`。**
9. **输入 API**:一次性动作(闪避/手雷/切枪)用 `wasPressedThisFrame`,持续动作(开火/移动)用 `isPressed`。全部走新版 Input System。**近战和远程都是左键开火**(由当前武器决定行为)。
10. **命令入不入队,看它会不会被拒绝**:**离散动作**(闪避/手雷——有 `CanAct`/冷却前置条件,按下时常常做不了)进 `InputBuffer` 排队,条件一满足立刻执行(这就是"跟手"的来源);**持续动作**(移动/连发)和**无条件动作**(切枪)直接执行,缓冲对它们只会带来延迟。
11. **队列存的是引用,不是快照**(第 4 周踩过):带参数的命令若"共享实例 + 可变字段",**绝不能入队**——排队期间参数会被后来的操作改掉。要么每个参数值一个实例 + `readonly` 字段(能安全入队),要么保证立即执行(`MoveCommand`/`SwitchWeaponCommand` 走的这条)。
12. **`PlayerInputHandler` 的执行顺序必须是 `-100`**:它写 `MoveInput`、`PlayerController` 读 `MoveInput`,Unity 不保证两个 `Update` 的先后。这个设置存在 `.meta` 里(会随 git 提交),不是全局配置。**凡是 A 写 B 读同一份数据,执行顺序就必须显式指定。**

**场景(`SampleScene.unity`)关键物体**:Player(Tag `Player`,挂 `PlayerController`/`Health`/`WeaponController`/**`PlayerInputHandler`**/**`GrenadeThrower`**/Rigidbody2D[Dynamic,Damping 0]/SpriteRenderer)、Enemy(Tag `Enemy`)、子弹对象池 + **手雷对象池**(各挂一个 `ObjectPool`)、GameManager、Camera(挂 `CameraFollow`)、**Canvas**(血条、TMP 弹药文本、**手雷/闪避两个技能图标 + 环形冷却遮罩**、Debug 文本)。预制体:`AmmoPickup.prefab`、**`Grenade.prefab`**(root Scale 必须 1 + Rigidbody2D[Dynamic,Gravity 0,**无 Collider2D**] + 子物体 `Body`/`Explosion`)。

**UI 素材注意**:血条填充用的是自建的 `Assets/Art/Square`(纯白无圆角)。**别用 Unity 内置的 `UISprite`**——那是带圆角的九宫格图,配 `Image Type = Filled` 时圆角会被裁切拉伸成脏边(week3 踩过)。环形冷却遮罩用 `Image Type = Filled` + `Fill Method = Radial 360`。

**表现必须和判定对得上**(week4 踩过):手雷的伤害判定是圆(`OverlapCircleAll`),所以爆炸的**视觉**也必须是圆——本体(方块)和爆炸圈(圆)拆成两个子物体,且爆炸圈大小**由脚本按判定半径算**,不在 Inspector 手填(手填的数字迟早和判定脱节)。调不明白手感时,先用 `OnDrawGizmosSelected` 把判定范围画出来看一眼。

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

- 当前阶段:**第 4 周已完成并验收(含三个课后练习),准备进入第 5 周 - 房间生成与关卡流程**(见 README「六周开发路线」)。
- 第 5 周目标产出:`RoomConfig`(ScriptableObject)数据驱动房间的敌人/道具布局、简单工厂生成敌群、房间清空后开门并切换到下一个房间。房间切换/敌群刷新的通知走 `EventBus`。**计划引入 Cinemachine 做摄像机过渡——需要先通过 Package Manager 安装,装完要在 CLAUDE.md「工程与版本约定」和 README「运行要求」里补一笔。**
- **分步下发的做法已连续两周验证有效,继续沿用**:大周拆成若干"每步可编译、可 Play 验收"的小步(第 3、4 周都是 4 步),每步末尾给 ✅ 验收清单,用户做完一步回来验收再进下一步。**纯重构的步骤,验收标准就写"行为和上周完全一样"**(第 4 周步骤 1 这么做的,效果很好)。
- **课后练习值得继续出**:第 4 周出了三道(`SwitchWeaponCommand`、可视化缓冲队列、通用化 `CooldownUI`),用户全做了,而且第一道让他真正撞上了"队列存引用不是快照"这个坑——**比直接讲有效得多**。
- **每步验收通过后必须 review 代码**:目前已经抓到 7 个"程序照常跑、验收照常过"的沉默 bug(`.name` vs `weaponName`、`Unsubscribe` 写成 `Subscribe` **两次**、`Empty()` 语义反了、off-by-one 等)。**这类问题测不出来,只能读代码。别因为"玩起来没问题"就跳过。**
- **`Subscribe`/`Unsubscribe` 配对已经错过两次**(week3 `HealthBarUI`、week4 `CooldownUI`)。快速自查命令:`grep -rn "EventBus.Subscribe\|EventBus.Unsubscribe" Assets/Scripts/`——一眼就能看出哪个文件订了没退。若再犯,应考虑引入 `EventListener<T>` 基类把配对封进去,**让写错变得不可能**。
- **分步下发的做法在第 3 周被验证有效,继续沿用**:大周拆成若干"每步可编译、可 Play 验收"的小步(第 3 周是 4 步),每步末尾给 ✅ 验收清单,用户做完一步回来验收再进下一步。
- **每步验收通过后仍要 review 代码**:第 3 周两个最严重的 bug(`.name` vs `weaponName`、`Unsubscribe` 写成 `Subscribe`)都是"程序照常跑、验收照常过"的沉默 bug,只能靠读代码抓出来。别因为"玩起来没问题"就跳过代码检查。

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
