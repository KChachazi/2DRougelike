# AGENTS.md

> 本文件面向在本仓库中协作的 Codex:每次新对话开始时应先读取本文件,用以恢复项目背景、当前进度与既定约定,避免在多次对话之间产生不一致的假设或重复造轮子。
> 若本文件描述与代码库实际状态冲突,**以代码库当前状态为准**;发现冲突或完成里程碑后,请顺手更新本文件对应章节,让下一次对话能无缝衔接。

## 项目一句话简介

俯视角 2D Roguelike 射击游戏(《元气骑士》风格),基于 Unity,目标是用 6 周时间跑通一套低耦合、可迁移的核心架构(V1 2D → V2 3D → V3 联机)。完整背景、六周计划、技术选型见 [README.md](README.md)。

## 当前进度(务必保持最新)

- 状态:**六周全部完成并通过验收;`v1-week1`~`v1-week6` 六个标签已全部补打(2026-07-25,本地),尚未 `push` 到远程。V1(2D 版本)收官,V1.5(深化)进行中——方向①(武器/伤害深化)已完成并通过 Play 验收(2026-08-04);方向②(敌人 AI 深化)已完成静态 review、设计修正、回归检查与 Play 验收(2026-08-11)。下一步进入方向③(程序化关卡生成)。**
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
  - 第 5 周:分 4 步全部完成并通过 Play 验收(RoomConfig SO + 工厂 + Room → Door + LevelManager → Cinemachine 过渡 → 小地图 + Boss 房)。新增 6 个脚本(`Level/` 5 个 + `UI/MinimapUI`)、修改 `GameEvents`(加 `RoomType` + 5 个关卡事件);新增 `Room.prefab`(**房间做成了预制体**,3 个实例各 override config/位置)、`Boss.prefab`、`RoomIcon.prefab`、`BossRoomConfig` 等 3 个 RoomConfig;装了 **Cinemachine 3.1.7**。详见 `devlog/week5.md`。
  - 第 5 周的坑(`devlog/week5.md`「踩过的坑」):① `OnDestory` **拼写错**(Unity 魔法方法靠方法名匹配,拼错则永不调用,编译器不报);② `LevelManager.currentIndex` 初始值写成 `1`(应为 `-1`)——**第 8 个沉默 bug**,被"正好有 3 个房间"掩盖着;③ **修 bug 引入回归**:为了让 Boss 显紫,直接删掉 `EnemyPatrolState` 里的 `Color.white`,结果普通敌人追击后永远卡在橙色——正解是记 `OriginalColor` 而非硬编码本色。
  - 第 6 周:分 4 步全部完成并通过验收(手感三件套 → 粒子特效 → Profiler/GC 优化 → 架构图 + 打包)。新增 4 个脚本(`Core/HitStop`+`ScreenShake`+`CombatFeedback`+`PooledParticle`)、修改 5 个;新增 `HitSpark`/`DeathSpark` 粒子预制体 + 池;README 加了 Mermaid 架构图;**成功构建出可运行的 Windows exe**。详见 `devlog/week6.md`。
  - 第 6 周的坑:① `ScreenShake.OnDisable` 漏复位 `localPosition` → 震动中切房间画面留歪;② **Unity 6 废弃了 `OverlapCircleNonAlloc`(→`OverlapCircle`+`ContactFilter2D`)和 `ContactFilter2D.NoFilter()`(→静态属性 `noFilter`)**——2D 物理 API 清理,碰到的代码都要跟着改(但只涉及 2 个文件,因为物理查询集中在 `MeleeWeaponStrategy`+`Grenade`);③ Boss `detectionRange 8 > loseSightRange 6` 破坏迟滞 → 状态频闪(退出阈值必须比进入阈值宽);④ `ObjectPool` 预热对象漏登记 `inPool` → 诊断误报。用户还自主把"命中震屏"改成"仅击杀震屏"(手感判断优于文档默认值)。
- **通关收尾**(2026-07-25):新增 `UI/VictoryUI.cs`(订阅 `LevelCompletedEvent`,通关弹出恭喜面板 + 冻结时间);修复 `MinimapUI` 离开最终房间后 `currentIndex` 不重置导致 Boss 房图标卡在黄色的问题。**V1 至此才真正"从头到尾完整"。**
- 目录已全部落地:`Core`/`Entities`/`Weapons`/`StateMachines`/`UI`/`Commands`/`Level`/**`AI`** + `Data`/`Art`/`Prefabs`。
- **2026-07-25 换机说明**:项目从另一台电脑通过 GitHub 拉取到当前机器,`Reference/`(gitignore,不进版本库)在新机器上是空的,已根据本文件记录 + `Assets/Scripts/` 现状 1:1 复原(`Reference/Scripts/` 镜像 `Assets/Scripts/`,51 个文件)。同时发现旧机器上的 `v1-week*` 标签从未真正打过/未推送,已在本机补打并核对 README 状态描述。
- **V1.5-1 武器/伤害深化**(2026-08-04):**已完成并写入 `Assets/`,Play 验收通过**。新增 8 个脚本、更新 10 个脚本。核心变更:① 新增 `Core/StatusEffectTypes`(枚举+配置 struct) + `Core/DamageInfo`(伤害包 readonly struct,替代裸传 int);② 新增 `Entities/StatusEffectManager`(状态异常管理器:DoT Tick、减速/易伤倍率);③ 新增 `Weapons/WeaponStrategyDecorator`(抽象基类)+ `BurningDecorator`/`FreezingDecorator`/`KnockbackDecorator` 三个具体装饰器(文件保留,暂不参与自动装配——留给运行时 Buff 系统);④ `IWeaponStrategy.Fire` 签名从 `(WeaponController, WeaponData)` 改为 `(WeaponController, DamageInfo)`,整条伤害链路统一走 DamageInfo;⑤ `WeaponData` 新增 7 个元素效果字段(Burn/Freeze/Knockback,全部默认 0 向后兼容);⑥ `WeaponController` 新增 `BuildDamageInfo()` 作为 SO→DamageInfo 唯一翻译点,`BuildStrategy` 已删除(Decorator 不自动包裹);⑦ `Health` 新增 `TakeDamage(DamageInfo)` 重载(自动读易伤倍率);⑧ `EnemyController.MoveTowards` 自动乘 `SpeedMultiplier`(冰冻减速);⑨ `GameEvents` 预埋 `StatusAppliedEvent`/`StatusExpiredEvent`。**验收后修正**:击退从 `StatusEffectManager.ApplyKnockback`(velocity 方式,被 MovePosition 覆盖)改为独立状态 `EnemyKnockbackState`(线性衰减位移,走 MovePosition 统一移动方式)。Decorator 文件保留但不自动包裹 SO 字段,留给未来 BuffManager 运行时调用。详细设计+完整代码见 `devlog/V1.5-1.md`。
- **V1.5-2 敌人 AI 深化**(2026-08-11):**已完成并通过 Play 验收**。`Assets/Scripts/AI/` 当前 27 个脚本(通用行为树框架、近战/远程/自爆节点、Boss 阶段与技能节点、感知 Blackboard 数据、演示组件),另新增 `Entities/EnemyBehaviour.cs` + `EnemyBrain.cs`、`StateMachines/Enemy/EnemyFreeState.cs`、`Weapons/EnemyProjectile.cs`;`EnemyController` 已改成“行为树负责主动决策 + FSM 只负责 Free/Knockback/Dead 被动打断”,旧 `EnemyPatrolState`/`EnemyChaseState`/`EnemyAttackState` 已删除。已创建 `MeleeBehavior`/`RangedBehavior`/`BomberBehavior`/`BossBehavior` 四份 SO、`EnemyMeleed`/`EnemyRanged`/`EnemyBomber`/`EnemyProjectile` 预制体,并更新 `Boss.prefab` 与房间配置。最终行为包括近战、保持距离射击、自爆冲脸、Boss 半血切远程灼烧技能;视野采用每敌人 Blackboard 感知快照并恢复进入/退出双半径迟滞;Boss 阶段阈值来自 SO。真实代码继续使用英式拼法 `EnemyBehaviour`/`BehaviourTree`;选择节点类 `SelectorNode` 当前位于 `SelectorState.cs`,近战预制体当前名为 `EnemyMeleed`,不要擅自复制美式拼法平行类型。完整实际完成记录见 `devlog/V1.5-2.md` 第 10 节。
- **V1.5-2 首轮静态 review**(2026-08-09):`dotnet build Assembly-CSharp.csproj --no-restore` 结果为 **0 error / 3 warning**(warning 均为既有未使用字段),且多敌人预制体/SO/房间配置的主要 GUID 引用已核对。但发现至少 7 项待处理:① `EnemyBehaviour.cs` 错误引用 `UnityEditor.EditorTools`,Editor 编译可过但 Player 构建有风险;② `EnemyBrain.Update` 驱动行为树,移动节点却调用 `Rigidbody2D.MovePosition` + `fixedDeltaTime`,造成物理时序/帧率问题;③ `EnemyBrain` 与 `EnemyController` 的 `Awake` 无执行顺序保障,`PatrolAction` 构造时可能在出生点初始化前选首个目标;④ `lostSightRange` 完全未参与行为树,迟滞失效;⑤ `ShootAction` 从池取出错误预制体后不归还,配置错时会逐帧泄漏;⑥ `BehaviourTreeTester` 多嵌套了一层 `Sequence`,演示语义变成“近时攻击后又巡逻、远时什么也不做”;⑦ `phaseThresholds` 未使用,Boss 阈值硬编码 0.5。另有英/美拼法、文件名/类名和无用 `using` 等低优先级整理项。**尚未向 `Assets/` 写入任何修复;若要提供修复代码,只能先写到 `Reference/` 供用户手动同步。**
- **V1.5-2 第二轮复查**(2026-08-09):用户已手动修复首轮问题①②③⑤:移除 `UnityEditor` 引用;建树从 `Awake` 延后到 `Start`;行为树改在 `FixedUpdate` 驱动;`ShootAction` 组件错误分支会归还池对象。同时把投射物类统一为 `EnemyProjectile`,并同步两处调用。再次静态编译仍为 **0 error / 3 个既有 warning**。**仍待处理**:④ `lostSightRange`/迟滞;⑥ 错误的 `BehaviourTreeTester` 结构;⑦ 未使用的 `phaseThresholds`/硬编码 0.5;低优先级命名与无用 `using`;以及在 Unity 中打开 `EnemyProjectile.prefab` 确认脚本组件未丢失并保存——当前 YAML 的 `m_EditorClassIdentifier` 仍是旧的 `Game.Weapons.enemyProjectile`,因为预制体在类重命名后尚未重存。**2026-08-10 已为问题④与⑦完成正式设计修正并更新参考实现**:视野采用“感知更新 → 每敌人 Blackboard → 无状态条件节点”;Boss 阶段条件改为接收一基的阶段编号，并由节点从 `EnemyBehaviour.phaseThresholds` 换算出该阶段的完整血量区间，建树时一次性校验配置。两项都尚待用户手动同步进 `Assets/`。
- **V1.5-2 最终复查与验收**(2026-08-11):问题④/⑥/⑦均已同步修复;`EnemyProjectile.prefab` 已重存且类型标识更新为 `Game.Weapons.EnemyProjectile`;Boss 阶段 1 遗留的错误 `Inverter` 与阶段配置失败后的空树路径已在回归 review 中修正。`dotnet build Assembly-CSharp.csproj --no-restore` 为 **0 error / 3 个既有 warning**。用户确认近战、远程、自爆、Boss 两阶段、击退打断、视野迟滞与投射物回池 Play 验收均无问题。方向②正式完成。
- 下一步:**V1(2D 核心玩法)收官,进入 V1.5(深化阶段)**,而不是立刻做 V2(3D 化)——用户判断"核心玩法刚搭起来,3D 化为时过早",且学习目标是把游戏编程模式吃透,武器系统这类还有明显深挖空间。V2/V3 推迟为更后面的大版本,`EventBus`/`ICommand`/`IWeaponStrategy`/`RoomConfig` 这几层设计上不认识 2D Sprite/物理,理论上可原样迁移,不会因为推迟而过时。
  - **V1.5 四个方向,已确定优先级**(2026-07-25 讨论确定):
    1. **武器/伤害深化**(Decorator 装饰器 + 状态异常系统)——**已完成(2026-08-04)**。给武器叠加"燃烧/冰冻/击退"等附加效果,`Health` 上加 DoT/减速/易伤,击退改为独立状态 `EnemyKnockbackState`。
    2. 敌人 AI 深化(行为树/技能系统)——现有敌人是固定 4 态 FSM,换成行为树支持多敌人类型/远近战编队/Boss 多阶段技能。难度最高,放在①之后是为了复用状态异常系统。
    3. 程序化关卡生成——`LevelManager` 从固定房间数组换成图结构/规则驱动拓扑(分支、宝箱房、商店房)。放在①②之后,是因为地图生成的价值取决于房间里能放的敌人/武器池够不够丰富。
    4. 存档系统(Memento + 云存模式)——难度最低但最独立,放最后是因为越往后做,要序列化的状态(强化、图鉴、进度)越稳定,能少返工。
  - **当前状态**:方向①与方向②均已完成并通过 Play 验收。下一步进入**方向③程序化关卡生成**:先基于现有 `Room`/`RoomConfig`/`LevelManager` 设计图结构与生成规则,再按“每步可编译、可 Play 验收”的方式拆分实施。方向④存档系统继续排在其后。

**更新规则**:每完成一项里程碑(一周任务,或用户认可的阶段性成果)后:
1. 更新本节的"已有内容 / 尚未创建 / 下一步";
2. 在 `devlog/week<N>.md` 写入该周的**实际完成记录**(不是计划,是发生了什么);
3. 若达到 README 中周任务的验收标准,同步更新 README 底部"开发日志与标签"表,并询问用户是否要打 `v1-week<N>` 标签(打标签会写入共享历史,先确认再执行)。

## 当前代码结构快照(V1.5-2 已完成并通过验收,2026-08-11)

> "代码实际长什么样"的速查表,方便新对话快速定位。真实进度以 `Assets/` 为准;下面每条都对应已经落地的文件。

**脚本清单(`Assets/Scripts/`,当前共 88 个 `.cs`)**

- `Core/`
  - `GameManager.cs`——单例(`Instance`),`[SerializeField] player` 只读暴露为 `Player`。目前很轻,只做单例 + Player 引用。
  - `ObjectPool.cs`——通用对象池(`Queue<GameObject>` + `prewarmCount` 预热),`Get(pos,rot)`/`Release`;同文件定义 `IPoolable` 接口。**第 6 周加了泄漏诊断**:`debugMode` + `inPool` HashSet 检测重复 Release(**预热时也要 `inPool.Add`,否则误报**)、`ActiveCount` 统计(只涨不落=泄漏)。发布前把 `debugMode` 关掉。
  - `CameraFollow.cs`——`LateUpdate` 跟随 `target` + `offset`,锁 Z。
  - **`EventBus.cs`**(第 3 周)——`static` 泛型事件总线,`Dictionary<Type, Delegate>` 按事件类型分发;`Subscribe<T>`/`Unsubscribe<T>`/`Publish<T>`/`Clear`,外加 `[RuntimeInitializeOnLoadMethod]` 在进 Play 时清空(防 domain reload 关闭时 static 残留)。
  - **`GameEvents.cs`**(第 3 周起,逐周扩充)——`readonly struct` 事件集中定义:血量/弹药/武器/技能冷却(3~4 周)、关卡 5 个事件(5 周)、**战斗反馈 3 个(6 周):`EnemyDamagedEvent(Position,Damage)`/`EnemyDiedEvent(Position)`(语义事件)、`ScreenShakeEvent(Intensity,Duration)`(表现指令)**。同文件有 `SkillId`(`Dash`/`Grenade`)和 `RoomType`(`Normal`/`Boss`)枚举——**标识用枚举不用字符串**;**枚举/`RoomType` 放 `Core` 因为 `GameEvents` 要用它,底层不能依赖上层**。新增跨模块通知就在这里加 struct。
  - **`HitStop.cs`**(第 6 周)——静态单例,`Do(duration)` 把 `timeScale`→0、**`WaitForSecondsRealtime`**(绝不能用 `WaitForSeconds`,否则永久卡死)后恢复;重入 `StopCoroutine` 打断上一次。
  - **`ScreenShake.cs`**(第 6 周)——**挂每台 RoomCamera**,订阅 `ScreenShakeEvent`,抖 vcam 自己的 `localPosition`(不碰 Cinemachine API);**`unscaledDeltaTime`**(HitStop 时 timeScale=0)、`Mathf.Max` 不累加、`OnDisable` 复位。只有当前房间的 vcam active、会响应——inactive 的自动退订。
  - **`CombatFeedback.cs`**(第 6 周,挂 GameManager)——订阅语义事件 `EnemyDamagedEvent`/`EnemyDiedEvent`,翻译成表现(`HitStop.Do` + 发 `ScreenShakeEvent` + 从池取粒子)。**手感参数(停顿时长/震动强度)全集中在这一个 Inspector 面板**——伤害源不该知道"打中要震多少屏"。
  - **`PooledParticle.cs`**(第 6 周)——`IPoolable` 一次性粒子,`lifeTime` 从 `ParticleSystem` 自己算(不手填);预制体 `Stop Action` 必须 `None`(不能 `Destroy`)、取消 `Looping`。
- **`AI/`**(V1.5-2 新建,命名空间 `Game.AI`,当前 27 个 `.cs`,**已 review 并通过 Play 验收**)
  - 通用框架:`NodeState` + `Node`,组合节点 `SequenceNode`/`SelectorNode`,装饰节点 `InverterNode`/`CooldownNode`,叶节点基类 `ConditionNode`/`ActionNode`,以及 `Blackboard`/`BehaviourTree`。这些都是纯 C# 决策逻辑;`BehaviourTreeTester` 是步骤 1 演示组件,目前仍保留。
  - `AI/Enemy/` 11 个通用敌人节点:视野/攻击/射击/自爆范围条件,追击/巡逻/近战/保持距离/射击/自爆动作。`BossPhaseCondition` 与 `BossSkillAction` 当前实际放在 `AI/` 根目录。
  - 四棵树由 `EnemyBrain` 按 `EnemyBehaviour.type` 组装:近战=攻击→追击→巡逻;远程=过近后退→射击→追击→巡逻;自爆=引爆→追击→巡逻;Boss=半血后远程灼烧技能→半血前近战技能→追击→巡逻。
- `Entities/`
  - `Health.cs`——通用血量组件。`Current`/`Max`/`isDead`/`isInvincible`,`SetInvincible(duration)` 无敌帧,`TakeDamage`(无敌或已死直接跳过);三个 C# 事件:`Damaged(int)`、`Died()`、**`HealthChanged(current,max)`**(第 3 周加)。**Health 保持通用、不认识"玩家"/"UI",只发本地事件**。
  - `PlayerController.cs`——玩家状态机"上下文":持有 `Rb`/`health`/`SpriteRenderer`/`MoveInput` + 5 个状态实例 + `stateMachine`;`Update` 只做**朝向鼠标 + 冷却 + `Tick`**(**第 4 周起不再读键盘**),`FixedUpdate` **先 `Rb.linearVelocity = Vector2.zero` 再跑 `FixedTick`**。供外部调用的入口:`SetMoveInput(v)`(由 `MoveCommand` 写入)、`TriggerDash()`(由 `DashCommand` 调)、`TriggerAttack()`(由 `WeaponController` 近战时调);只读暴露 `CanAct`(仅 Idle/Move 为真)、`CanDash`。**它还负责把 `health.HealthChanged` 桥接成全局 `PlayerHealthChangedEvent`**(`Start` 广播一次初始血量),并在 `StartDashCooldown()` 里广播 `SkillCooldownStartedEvent(SkillId.Dash, ...)`。
  - **`EnemyBehaviour.cs`**(V1.5-2)——敌人行为档案 SO,实际类名使用英式拼法;`EnemyType { Melee, Ranged, Bomber, Boss }`,集中巡逻/追击/近战/击退/远程/自爆/Boss 技能参数。资产是 `MeleeBehavior`/`RangedBehavior`/`BomberBehavior`/`BossBehavior`。
  - **`EnemyBrain.cs`**(V1.5-2)——敌人主动决策入口;`Awake` 创建每敌人 Blackboard/感知对象,`Start` 按 `EnemyController.Behaviour.type` 构建四类行为树,`FixedUpdate` 先更新感知迟滞再评估树;`IsActionLocked` 时暂停。Boss 建树前校验阶段阈值,失败则禁用 Brain。远程/Boss 投射物通过序列化的 `ObjectPool` 获取。
  - `EnemyController.cs`——V1.5-2 后成为敌人的共享数据/能力上下文:从 `EnemyBehaviour` 暴露参数,持有 `Rb`/`health`/`StatusManager`/`Player`/出生点/本色,提供 `MoveTowards`/`DistanceToPlayer`/`TriggerKnockback`;FSM 只保留 `FreeState`/`KnockbackState`/`DeadState`,`IsActionLocked` 让击退/死亡抢占行为树。继续桥接 `Health.Damaged`/`Died` → 全局战斗反馈事件。**`detectionRange < lostSightRange` 仍是迟滞约束。**
- `Weapons/`(第 3 周重构成"数据 + 策略")
  - `WeaponData.cs`——武器 SO(`[CreateAssetMenu]` → `Create > Game > Weapon Data`),字段 `weaponName`/`type`/`damage`/`cooldown`/`maxAmmo`/`range`,外加 `WeaponType { Ranged, Melee }` 枚举。资产在 `Assets/Data/`:`Pistol`/`Rifle`/`Sword`。
  - `IWeaponStrategy.cs`——V1.5-1 后签名为 `void Fire(WeaponController controller, DamageInfo damageInfo)`。**策略无状态**,冷却/弹药由 controller 管,SO→伤害包只由 controller 翻译。
  - `RangedWeaponStrategy.cs`——从 `controller.BulletPool` 取子弹、按 `FirePoint` 朝向发射,并 `bullet.SetDamageInfo(damageInfo)`。
  - `MeleeWeaponStrategy.cs`——角色前方 `range` 处 `Physics2D.OverlapCircle(点,半径,filter,复用缓冲区)`(第 6 周从 `OverlapCircleAll` 改成零分配版),圈内 `Enemy` 扣血。**filter 用 `ContactFilter2D.noFilter`**(Unity 6:`NonAlloc`/`NoFilter()` 都已废弃,分别改成 `OverlapCircle` 重载 / 静态属性 `noFilter`)。缓冲区固定 16,超出被无视。
  - `WeaponController.cs`——**武器系统主体**(挂 Player,取代已移除的 `PlayerShooter` 组件)。持有 `WeaponData[] weapons` + `int[] currentAmmo`(每把武器各记一份,切枪不清零)+ `Dictionary<WeaponType, IWeaponStrategy>`。**第 4 周起不再读输入**:`Update` 里只剩冷却倒计时,对外暴露**能力**——`CanFire()`(= `CanAct && cooldown<=0 && HasAmmo()`)、`Fire()`、`SwitchTo(index)`、`AddAmmo(n)`、`WeaponCount`。近战开火后额外 `playerController.TriggerAttack()`。弹药/武器变化通过 `EventBus` 广播,**它不认识 UI**。
  - `Bullet.cs`——`IPoolable` 玩家子弹,`FixedUpdate` 用 `linearVelocity` 前进 + 计时回收,命中敌人后同时走 `Health.TakeDamage(DamageInfo)` + `StatusEffectManager.ApplyEffects`,再回池;主要注入入口是 `SetDamageInfo`,旧 `SetDamage(int)` 仍作兼容。
  - **`EnemyProjectile.cs`**(V1.5-2)——池化敌人子弹,`Launch` 注入速度与 `DamageInfo`;命中 Player 后造成伤害并施加状态异常。类名与预制体序列化标识均已统一为 `EnemyProjectile`。
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
  - **`MinimapUI.cs`**(第 5 周)——一排格子代表房间。订阅 `LevelStartedEvent`(按房间数生成格子)/`RoomEnteredEvent`(高亮当前)/`RoomClearedEvent`(标记已清)/**`LevelCompletedEvent`(通关重置 currentIndex)**。**不认识 `LevelManager`、不认识 `Room`**。颜色优先级:当前(黄) > 已清空(绿) > Boss 未清(红) > 没去过(灰)。
  - **`VictoryUI.cs`**(第 6 周通关收尾)——挂 Canvas 根物体,订阅 `LevelCompletedEvent`,显示胜利面板 + 可选冻结时间(`freezeTimeOnVictory`)。**和 `HealthBarUI`/`MinimapUI` 同一套路:只订阅 EventBus,不认识任何游戏逻辑。**
- **`Level/`**(第 5 周新建,命名空间 `Game.Level`)
  - `RoomConfig.cs`——房间 SO(`Create > Game > Room Config`):`roomName`/`type`/`enemySpawns[]`/`pickupSpawns[]`。生成位置用**相对房间中心的 `localPosition`**(所以同一份配置能被任意位置的房间复用)。同文件有 `EnemySpawn`/`PickupSpawn` 两个 `[System.Serializable] struct`。资产在 `Assets/Data/`:`Room_1_Config`/`Room_2_Config`/`BossRoomConfig`。
  - `EnemyFactory.cs`——**静态简单工厂**,`Create(prefab, worldPos, parent)`。现在只包了一层 `Instantiate`,价值在于**以后改生成逻辑(池化/按难度调血/生成时注册)只有这一个入口**。**敌人刻意不走对象池**——生成频率极低(进房间时一次性一批),池化收益接近零。
  - `Room.cs`——场景里的房间。按 `config` 生成内容、**订阅每个敌人的 `Health.Died` 计数**(不轮询)、清空后 `door.Unlock()` + 发**局部事件** `RoomCleared`;`Enter()`/`Exit()` 开关自己的 `roomCamera`。**它不知道自己是第几个房间、不认识 `LevelManager`**。`spawned` 标记保证只生成一次;**空房间靠 `Enter()` 末尾的 `if (aliveCount <= 0) MarkCleared()` 兜底**(否则门永远不开、玩家卡死)。
  - `Door.cs`——锁着时 `OnTriggerEnter2D` 直接 return;`Unlock()` 后碰到 Player 就发 `DoorEnteredEvent`。**它不知道自己通向哪**——去哪儿是 `LevelManager` 的事。所以门能随便复制到任何房间。
  - `LevelManager.cs`——**唯一知道房间顺序的人**。`Start` 广播 `LevelStartedEvent` + `EnterRoom(0)`;订阅 `DoorEnteredEvent` → `EnterRoom(currentIndex + 1)`;**把 `Room.RoomCleared` 局部事件桥接成带 index 的全局 `RoomClearedEvent`**(`Array.IndexOf` 查身份)。`currentIndex` 初始 **`-1`**(= 还没进过任何房间)。传送玩家用 `rb.position` + 清 `linearVelocity`。
- `StateMachines/`
  - `IState.cs`(Enter/Tick/FixedTick/Exit)+ `StateMachine.cs`(`CurrentState`/`ChangeState`/`Tick`/`FixedTick`,**纯 C# 类,非 MonoBehaviour**,controller 内部 `new` 一个)。
  - `Player/`:Idle / Move / Dash / Attack / Hurt 五态。**状态类已彻底不读输入**(第 3 周删了右键近战分支和 `PerformHit`,第 4 周删了读 Shift 的闪避分支),现在只根据当前数据决定状态转换。
  - `Enemy/`:V1.5-2 后只剩 Free / Knockback / Dead 三态;Patrol / Chase / Attack 已由行为树节点取代并从 `Assets/` 删除。`FreeState` 是行为树接管主动决策时的空闲占位,击退结束回 Free,死亡保持锁定。

**几个已确立的实现事实/约定(改代码前注意)**

1. **玩家 FSM;敌人 = 行为树主动决策 + FSM 被动打断**:`PlayerController` 仍把 Idle/Move/Dash/Attack/Hurt 行为放在状态类里。V1.5-2 起 `EnemyBrain` 的行为树决定巡逻/追击/攻击/射击/自爆/Boss 技能,`EnemyController` 内的 FSM 只负责 Free/Knockback/Dead;`IsActionLocked` 是两套系统的协调点。不要重新创建敌人 Patrol/Chase/Attack 状态类。
2. **玩家/敌人移动 = `MovePosition` + 每帧清零 velocity**:两者刚体都是 Dynamic;靠 `MovePosition` 位移。玩家在 `FixedUpdate` 开头 `Rb.linearVelocity = Vector2.zero` 消除碰撞残留速度(否则被撞会一直漂,见 week2 第 5.2 节)。之后若要"击退",需显式设计,不能靠残留速度。
3. **两层事件,别混用**:**局部 C# 事件**(`Health.Damaged`/`Died`/`HealthChanged`)用于**同一实体内部**的反应——`Damaged` → 切 `HurtState`、`Died` → 切 `DeadState`,controller 在 `OnEnable/OnDisable` 订阅/退订;**全局 `EventBus`** 用于**跨模块**通知(UI/拾取/武器)。`Health` 这种通用组件只发局部事件,"我是玩家、我的血要给 UI 看"这层身份由 `PlayerController` 桥接后 `EventBus.Publish`。**新代码遵循这个分层,别让通用组件直接 Publish 全局事件。**
4. **EventBus 时序铁律**:**订阅放 `OnEnable`、初始值广播放 `Start`**(Unity 保证所有 `OnEnable` 先于所有 `Start`),否则 UI 收不到初始值。**`Subscribe`/`Unsubscribe` 必须成对**——写反了编译器不报错,但会累积重复订阅、切场景时抱着已销毁对象崩溃(week3 踩过)。
5. **武器 = SO 数据 + 无状态策略**:新增一把武器 = 建一个 `WeaponData` 资产;新增一种开火方式 = 加一个 `IWeaponStrategy` 实现 + 在 `WeaponController.Awake` 的字典里注册。**不要把冷却/弹药状态塞进策略,也不要把开火行为塞进 SO。** 从 SO 取显示名用 `weaponName` 字段,**不是 `.name`**(那是资产文件名,week3 踩过)。
6. **伤害判定现在是多入口、统一伤害包**:玩家近战=`MeleeWeaponStrategy` 物理范围查询;玩家远程=`Bullet.OnTriggerEnter2D`;普通敌人近战=`MeleeAttackAction` 距离判定;远程敌人/二阶段 Boss=`EnemyProjectile.OnTriggerEnter2D`;自爆=`ExplodeAction`;一阶段 Boss=`BossSkillAction` 直接命中。V1.5-1 后可携带元素效果的路径都应传 `DamageInfo`,由命中点分别调用 `Health.TakeDamage` 与 `StatusEffectManager.ApplyEffects`。
7. **调试用颜色反馈(临时)**:玩家状态仍会改色;敌人的主动行为节点与 Knockback/Dead 也会改色,退出/恢复时必须回 `EnemyController.OriginalColor`,**绝不能硬编码 `Color.white`**。旧 `EnemyPatrolState` 已删除,当前恢复本色的责任分散在行为节点/被动状态中,review 时要检查所有抢占与退出路径。
8. **输入全部收拢在 `PlayerInputHandler`**(第 4 周确立):`PlayerController`、状态类、`WeaponController` **一律不读键盘鼠标**,它们只对外暴露"能力"(`TryXxx`/`CanXxx`),由命令来调。新增一个玩家动作 = 加一个 `ICommand` 实现 + 在 `PlayerInputHandler` 里绑定按键。**别再在别的地方写 `Keyboard.current`。**
9. **输入 API**:一次性动作(闪避/手雷/切枪)用 `wasPressedThisFrame`,持续动作(开火/移动)用 `isPressed`。全部走新版 Input System。**近战和远程都是左键开火**(由当前武器决定行为)。
10. **命令入不入队,看它会不会被拒绝**:**离散动作**(闪避/手雷——有 `CanAct`/冷却前置条件,按下时常常做不了)进 `InputBuffer` 排队,条件一满足立刻执行(这就是"跟手"的来源);**持续动作**(移动/连发)和**无条件动作**(切枪)直接执行,缓冲对它们只会带来延迟。
11. **队列存的是引用,不是快照**(第 4 周踩过):带参数的命令若"共享实例 + 可变字段",**绝不能入队**——排队期间参数会被后来的操作改掉。要么每个参数值一个实例 + `readonly` 字段(能安全入队),要么保证立即执行(`MoveCommand`/`SwitchWeaponCommand` 走的这条)。
12. **`PlayerInputHandler` 的执行顺序必须是 `-100`**:它写 `MoveInput`、`PlayerController` 读 `MoveInput`,Unity 不保证两个 `Update` 的先后。这个设置存在 `.meta` 里(会随 git 提交),不是全局配置。**凡是 A 写 B 读同一份数据,执行顺序就必须显式指定。**
13. **"局部事件 + 上层桥接"是本项目的固定套路**(第 3 周确立,第 5 周第二次用):通用组件只发局部 C# 事件(`Health.HealthChanged`、`Room.RoomCleared`),因为它不知道自己的"身份";由知道身份的上层(`PlayerController`、`LevelManager`)接住、补上身份、再 `EventBus.Publish` 成全局事件。**新模块遵循这个套路,别让底层组件直接广播带身份的全局事件。**
14. **依赖会变的外部库时,只依赖它最稳定的那一面**(第 5 周 Cinemachine 踩过):`Room` 的摄像机字段是 `GameObject` + `SetActive`,**不引用任何 Cinemachine API**。于是 CM 2.x→3.x 把类名/命名空间/字段全改了,我们的代码依然编译得过(只有文档里的菜单路径要更新)。
15. **池化看频率不看类型**:对象池的收益是"避免**高频**创建销毁的 GC 抖动"。子弹每秒几十发、命中粒子每命中一个→池化;敌人进房间时一次性生成一批→池化收益接近零,却要多管一套生命周期,所以**不池化**。**判断标准是"多频繁",不是"是什么"**——架构约定是工具不是教条。
16. **语义事件 vs 表现指令**(第 6 周确立):`EnemyDamagedEvent`("敌人受伤了",只陈述事实)是**语义事件**;`ScreenShakeEvent`("震屏 X 强度")是**表现指令**。伤害源只发语义事件,由 `CombatFeedback` 决定"该有什么表现"再发表现指令。**伤害源不该知道"打中要震多少屏"**;手感参数全集中在 `CombatFeedback` 一个面板。
17. **`timeScale=0` 期间要继续走的东西必须用 unscaled 时间**:`HitStop` 用 `WaitForSecondsRealtime`(不是 `WaitForSeconds`),`ScreenShake` 用 `Time.unscaledDeltaTime`——否则命中停顿会让它们当场冻住(HitStop 甚至永久卡死)。
18. **迟滞(hysteresis)**:任何"进入条件"和"退出条件"用不同阈值的地方,退出阈值要比进入阈值宽,留出缓冲区,否则临界点附近会抖动。敌人 AI 的 `detectionRange`(进入追击,小)< `loseSightRange`(退出追击,大)就是例子。

**场景(`SampleScene.unity`)关键物体**:Player(Tag `Player`,挂 `PlayerController`/`Health`/`StatusEffectManager`/`WeaponController`/`PlayerInputHandler`/`GrenadeThrower`/Rigidbody2D[Dynamic,Damping 0]/SpriteRenderer)、子弹池 + 手雷池 + **命中/死亡粒子池**(各挂 `ObjectPool`)、**GameManager(挂 `HitStop` + `CombatFeedback`)**、**`Main Camera`(挂 `CinemachineBrain`,`CameraFollow` 已禁用)**、**`LevelManager`**(rooms 数组按顺序拖 3 个房间)、**3 个 `Room.prefab` 实例**(x = 0/30/60,各 override `config`,每个 `RoomCamera` 上挂 `ScreenShake`)、**Canvas**(血条、TMP 弹药文本、技能图标 + 环形冷却遮罩、Debug 文本、`Minimap`)。**敌人不手摆在场景里**——由 `RoomConfig` 生成;V1.5-2 已把房间配置中的敌人引用换成多类型预制体,生成组合已通过 Play 验收。

**预制体(`Assets/Prefabs/`)**:V1.5-2 的敌人族为 `EnemyMeleed`(当前拼法)/`EnemyRanged`/`EnemyBomber`/`Boss`,以及池化的 `EnemyProjectile`;旧 `Enemy.prefab` 已删除。其余包括 `Bullet`、`Grenade`(root Scale 1 + Rigidbody2D[无 Collider2D] + 子物体 `Body`/`Explosion`)、`AmmoPickup`、`CooldownIcon`、`RoomIcon`、`Room`(围墙 + `EntryPoint` + `Contents` + `Door` + `RoomCamera`[inactive])、**`HitSpark`/`DeathSpark`**(粒子,`Stop Action=None`、取消 `Looping`)。**`Assets/Data/` 只放 ScriptableObject,预制体一律放 `Assets/Prefabs/`**。

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

- **Codex 不直接把游戏代码写进 `Assets/` 里**。用户希望自己动手在 `Assets/Scripts/` 下创建文件、亲自敲代码来学习,而不是打开项目发现代码已经全部写好。
- Codex 的示例/参考实现统一写在仓库根目录的 **`Reference/`** 文件夹——与 `Assets/` 同级(不在其内部),已加入 `.gitignore`,不会被 Unity 编译、也不会进版本库。目录结构镜像 `Assets/Scripts/`,例如 `Reference/Scripts/Core/ObjectPool.cs` 对应用户将来要在 `Assets/Scripts/Core/ObjectPool.cs` 创建的内容。
- 每周文档(`devlog/week<N>.md`)里的代码讲解章节,标题指向 `Assets/Scripts/...`(目标路径),正文提示参考实现在 `Reference/Scripts/...`,并说明这是需要用户自己创建的文件,不是已经落地的实现。
- `Reference/` 里的内容会随周数推进被覆盖/更新,只代表"当前这一步应该长成什么样",不代表用户项目的真实进度——**真实进度以 `Assets/` 里用户实际创建的文件为准**。
- 例外:场景文件(`.unity`)、预制体(`.prefab`)等 Unity 二进制/YAML 资产不适合放参考副本,继续沿用「Codex 不直接编辑场景文件,由用户在编辑器里操作」的既有约定。

## 开发节奏

- 当前阶段:**六周全部完成,V1(2D)收官,V1.5 方向①与方向②均已验收完成,准备进入方向③程序化关卡生成**。六篇 `devlog/week1~6.md` 均已写完实际完成记录;`v1-week1`~`v1-week6` 六个标签已本地补打(未 push)。
- **下一步**:V1.5 的武器/伤害深化与敌人 AI 深化均已完成,现在进入程序化关卡生成,之后再做存档系统。README 规划的 V2(3D 化)、V3(联机)推迟到 V1.5 之后:迁移时 `EventBus`/`ICommand`/`IWeaponStrategy`/`RoomConfig`/`StateMachine` 这几层不认识 2D Sprite/物理,理论上可原样搬;要改的是 `Bullet`/`MoveTowards`/`OverlapCircle` 这些碰 2D 物理和渲染的地方。
- **发布前清理清单**(答疑用):`ObjectPool.debugMode` 关掉、`DebugText.display` 关掉、清临时 `Debug.Log`;打包时 `Build Settings > Scenes In Build` 必须勾上场景(否则黑屏),构建目录别选在项目内(会递归导入)。
- **分步下发的做法已连续三周验证有效,继续沿用**:大周拆成若干"每步可编译、可 Play 验收"的小步(第 3、4、5 周都是 4 步),每步末尾给 ✅ 验收清单,用户做完一步回来验收再进下一步。**纯重构的步骤,验收标准就写"行为和上周完全一样"**(第 4 周步骤 1 这么做的,效果很好)。**"不写代码、只做对比实验"的步骤也很有价值**(第 4 周步骤 2 让用户把 `bufferDuration` 调成 0 体会差异)。
- **课后练习值得继续出**:第 4 周出了三道,用户全做了,而且第一道让他真正撞上了"队列存引用不是快照"这个坑——**比直接讲有效得多**。第 5 周出了三道(清空奖励该放哪、Boss 血条要不要新事件、房间可重进要处理哪些状态)。
- **每步验收通过后必须 review 代码**:目前已经抓到 **8 个**"程序照常跑、验收照常过"的沉默 bug(`.name` vs `weaponName`、`Unsubscribe` 写成 `Subscribe` **两次**、`Empty()` 语义反了、`index <= Count` off-by-one、`currentIndex` 初始值写成 1、`OnDestory` 拼写等)。**这类问题测不出来,只能读代码。**
- **验收/答疑时的高频诊断顺序**:① Console 的 `Log` 过滤按钮是不是被关了(第 5 周虚惊过);② Inspector 改了没 `Ctrl+S`(第 5 周踩过两次——场景和 SO 资产都要存);③ Unity 魔法方法拼写(`OnDestory`);④ 才是代码逻辑。
- **`Subscribe`/`Unsubscribe` 配对曾错过两次**(week3 `HealthBarUI`、week4 `CooldownUI`),**第 5 周 `MinimapUI` 的三对订阅一次做对了**。自查命令:`grep -rn "EventBus.Subscribe\|EventBus.Unsubscribe" Assets/Scripts/`。
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

当前目录结构:

```
Assets/Scripts/Core/          # GameManager, EventBus, GameEvents, ObjectPool...
Assets/Scripts/Entities/      # 玩家、敌人、NPC
Assets/Scripts/Weapons/       # 武器接口、策略、SO 定义、手雷
Assets/Scripts/Commands/      # 命令模式相关类
Assets/Scripts/StateMachines/ # 状态机实现
Assets/Scripts/Level/         # 房间/关卡系统(第 5 周新增)
Assets/Scripts/UI/            # UI 控制脚本
Assets/Scripts/AI/            # 行为树框架 + 敌人决策节点(V1.5-2 新增)
Assets/Prefabs/  Assets/Scenes/  Assets/Data/  Assets/Art/  Assets/Audio/  Assets/ThirdParty/
```

新脚本按职责放入对应目录,不要都堆在 `Assets/Scripts` 根目录。**`Core` 是"谁都可能用到的地基"(EventBus/对象池/事件定义),不是"什么都往里扔的杂物间"**——关卡这种独立模块要单开目录(第 5 周确立)。

**`Assets/Data/` 只放 ScriptableObject 资产,预制体一律放 `Assets/Prefabs/`**(第 4、5 周各放错过一次:`Grenade.prefab`、`Room.prefab`)。

**命名空间约定**(第 1 周确立):子目录与命名空间一一对应——`Game.Core`、`Game.Entities`、`Game.Weapons`、`Game.Commands`、`Game.StateMachines`、`Game.Level`、`Game.UI`、`Game.AI`。看 `using` 就能判断这个类归哪个目录管,新文件按此规则加命名空间。

**属性命名约定**(第 2 周确立,用户明确要求):`PlayerController`/`EnemyController` 上暴露 `Health` 组件引用的公开属性用**小写开头**的 `health`(不是 C# 惯例的 `Health`)。这是用户主动选择的风格,不是笔误,新代码(包括 Codex 给的参考实现)一律跟随这个写法,不要擅自"改正"回 PascalCase。

## 工程与版本约定

- 引擎版本锁定 **6000.3.19f1**,不要因为个人环境不同而修改 `ProjectSettings/ProjectVersion.txt`。
- 使用新版 Input System(`com.unity.inputsystem`),不要引入旧版 `UnityEngine.Input` API。
- 渲染管线为 URP 2D,特效/Shader 工作基于 URP 2D Renderer。
- **Cinemachine 3.1.7 已安装**(第 5 周,`Packages/manifest.json` 里的 `com.unity.cinemachine`)。**注意 3.x 和 2.x 差异很大**:`CinemachineCamera`(旧 `CinemachineVirtualCamera`)、`Tracking Target`(旧 `Follow`+`Look At`)、命名空间 `Unity.Cinemachine`(旧 `Cinemachine`)。**项目代码刻意不引用任何 Cinemachine API**——`Room` 的摄像机字段类型是 `GameObject`,靠 `SetActive` 切换让 Brain 自动 blend,从而不被版本差异绑架。**新代码请沿用这个做法,不要 `using Unity.Cinemachine`。**
- Addressables 仍未安装,现阶段不要引入(V1 用 Resources,V2 再迁移)。

## Git / 协作约定

- 分支:`main`(稳定,仅接受 PR)、`dev`(开发主线)、`feature/<功能名>`。
- 场景(`.unity`)与预制体(`.prefab`)冲突由 UnityYAMLMerge 处理;自动合并失败时在编辑器里手动解决后再提交,不要用命令行强行二选一。
- 根目录已有 `.gitignore`(忽略 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、IDE 生成文件等)与 `.gitattributes`(LFS 规则 + `merge=unityyamlmerge` 属性)。**这两个文件本身已提交**,但 UnityYAMLMerge 的合并驱动路径是本机配置,不会随仓库同步——每台开发机克隆后需按 README「冲突处理」一节各自执行一次 `git config merge.unityyamlmerge.driver ...`。
- 首次在新机器上使用前仍需执行 `git lfs install`(注册全局 LFS 过滤器),这一步不属于仓库内容,不能靠 `.gitattributes` 自动完成。
- **`git commit` 一律由用户自己执行**(第 2 周确立,长期有效):Codex 可以 `git add`、准备好提交信息、告诉用户要跑什么命令,但不要代替用户执行 `git commit`。打 tag(不涉及重写历史)、`git status`/`git log` 这类只读或低风险操作不受此限制。

## 给协作者的提示

- README.md 面向人(项目目标、周计划、参考资料),AGENTS.md 面向 Codex(当前进度、既定约定、踩过的坑)。改变项目目标/计划改 README;改变当前进度/约定/注意事项改本文件。
- `docs/CODEX_WORKFLOW.md` 是本项目日常使用 Codex 的提示词模板和工作节奏参考；它不替代 AGENTS.md 的项目规则。
- 项目仍处于起步阶段:任何"架构约定"一旦被用户在对话中修改或否决,应立即更新本文件对应表格/条目,避免下次对话重复同样的分歧。
