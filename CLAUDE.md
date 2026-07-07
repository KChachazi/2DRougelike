# CLAUDE.md

> 本文件面向在本仓库中协作的 Claude Code:每次新对话开始时应先读取本文件,用以恢复项目背景、当前进度与既定约定,避免在多次对话之间产生不一致的假设或重复造轮子。
> 若本文件描述与代码库实际状态冲突,**以代码库当前状态为准**;发现冲突或完成里程碑后,请顺手更新本文件对应章节,让下一次对话能无缝衔接。

## 项目一句话简介

俯视角 2D Roguelike 射击游戏(《元气骑士》风格),基于 Unity,目标是用 6 周时间跑通一套低耦合、可迁移的核心架构(V1 2D → V2 3D → V3 联机)。完整背景、六周计划、技术选型见 [README.md](README.md)。

## 当前进度(务必保持最新)

- 状态:**项目刚初始化,尚未开始第 1 周任务**。
- 已有内容:Unity 6000.3.19f1 + URP 2D 模板默认工程(`Assets/Scenes/SampleScene.unity`、默认 Volume Profile 等),Input System 包已安装,`Assets/InputSystem_Actions.inputactions` 为默认生成,尚未按项目需求配置。
- 尚未创建:`Assets/Scripts`、`Assets/Prefabs`、`Assets/Data` 等任何自定义目录和脚本。README 中的六周计划与架构模式表均为**目标设计**,代码库里还没有对应实现,不要假设任何 Core/Entities/Weapons 等脚本已存在。
- `devlog/week1.md` 已建但为空,尚无周记内容。
- 下一步:从 README「第 1 周」任务开始——Unity 2D 项目细化、玩家八方向移动、单发子弹对象池雏形、敌人追击碰撞伤害、最小 Game Loop 框架。

**更新规则**:每完成一项里程碑(一周任务,或用户认可的阶段性成果)后:
1. 更新本节的"已有内容 / 尚未创建 / 下一步";
2. 在 `devlog/week<N>.md` 写入该周的**实际完成记录**(不是计划,是发生了什么);
3. 若达到 README 中周任务的验收标准,同步更新 README 底部"开发日志与标签"表,并询问用户是否要打 `v1-week<N>` 标签(打标签会写入共享历史,先确认再执行)。

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
- 由于 `.gitignore`/`.gitattributes` 是刚补充的,`Assets/`、`Packages/`、`ProjectSettings/` 等此前已存在但未提交的文件仍会在下次 `git add` 时按新规则处理;首次提交前建议先 `git status` 确认没有 `Library/`、`Temp/` 等生成目录被意外加入暂存区。

## 给协作者的提示

- README.md 面向人(项目目标、周计划、参考资料),CLAUDE.md 面向 Claude(当前进度、既定约定、踩过的坑)。改变项目目标/计划改 README;改变当前进度/约定/注意事项改本文件。
- 项目仍处于起步阶段:任何"架构约定"一旦被用户在对话中修改或否决,应立即更新本文件对应表格/条目,避免下次对话重复同样的分歧。
