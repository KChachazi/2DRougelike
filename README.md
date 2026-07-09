
# Project V1 - 2D Roguelike Shooter (《元气骑士》风格)

基于 Unity 的俯视角 2D Roguelike 射击游戏，聚焦**游戏编程模式**与**可迁移架构**，构建一套 V1（2D）→ V2（3D）→ V3（联机）的渐进式工程。

---

## 🎮 项目简介

- **类型**：俯视角 Roguelike 射击
- **引擎**：Unity 6.3 LTS (6000.3.19f1)，2D + URP 模板
- **目标**：在 6 周内实现完整的房间战斗、敌人 AI、数据驱动武器系统，并沉淀一套与引擎低耦合的核心逻辑（可复用于后续 3D 和联机版本）。
- **参考游戏**：《Soul Knight》（元气骑士）
- **当前状态**：项目已初始化（URP 2D 模板 + Input System），尚未开始第 1 周开发，详见 [CLAUDE.md](CLAUDE.md) 中的“当前进度”

---

## 🧱 技术架构（核心模式）

| 模块               | 实现方式                         | 设计意图                                      |
| ------------------ | -------------------------------- | --------------------------------------------- |
| Game Loop          | Update / FixedUpdate 明确分离    | 逻辑与渲染隔离，便于帧率无关逻辑复用          |
| 状态机 (FSM)       | 玩家/敌人状态由枚举+Switch 或状态类驱动 | 行为可扩展，调试清晰                        |
| 事件总线 (EventBus) | 轻量级观察者模式，解耦 UI、血量、拾取等 | 模块间零硬引用，利于单元测试和模块替换    |
| 命令模式           | 将玩家输入封装为命令，支持缓冲队列 | 实现输入缓冲、技能队列，可轻松录制回放 |
| 对象池             | 子弹、特效、敌人的通用对象池     | 减少 GC，稳定帧率                             |
| 武器策略           | IWeaponStrategy 接口 + ScriptableObject 数据 | 武器即数据 + 策略，新增武器无需改主体逻辑 |
| 房间生成           | RoomConfig (ScriptableObject) + 简单工厂 | 关卡数据驱动，房间布局和敌群可迅速调整   |
| 资源管理           | Addressables (可选) 或 Resources 目录 | 渐进式引入，V1 可暂用 Resources，V2 切 Addressables |

---

## 🗺️ 六周开发路线

### 第 1 周：项目搭建与基础移动射击
- Unity 2D 项目初始化，Input System 接入
- 玩家八方向移动 + 面向鼠标旋转
- 单发子弹射击（对象池雏形）
- 敌人追击 + 碰撞伤害
- 最小 Game Loop 框架

### 第 2 周：状态机与敌人基础 AI
- 玩家状态机（Idle, Move, Dash, Attack, Hurt）
- 敌人状态机（Patrol, Chase, Attack, Dead）
- 闪避（Dash）+ 无敌帧 + 受击闪白
- 近战武器原型

### 第 3 周：事件系统与数据驱动武器
- EventBus 接入，UI 血条/子弹数事件更新
- 武器 ScriptableObject（伤害、冷却、子弹类型）
- 策略模式武器切换（手枪/步枪/近战）
- 弹药限制 + 补给拾取

### 第 4 周：命令模式与输入缓冲
- 指令封装（MoveCommand, AttackCommand, DashCommand）
- 输入缓冲队列实现连招
- 技能冷却可视化
- 范围技能（手雷）演示非指向性命令

### 第 5 周：房间生成与关卡流程
- RoomConfig 数据驱动房间敌人/道具布局
- 简单工厂生成敌人和道具
- 房间切换逻辑 + Cinemachine 摄像机平滑过渡
- 3-5 房间关卡，含 Boss 房间
- 小地图 UI

### 第 6 周：打磨、优化与展示
- Shader 特效（受伤溶解、拾取金光）
- 手感优化（相机跟随、命中停顿、屏幕震动）
- 性能 Profiling，对象池泄漏检查
- 架构图、视频录制、技术文档整理

---

## 🛠️ 版本控制与协作

本仓库采用 **Git + Git LFS + Unity Smart Merge** 方案，确保多端同步与团队协作安全。

### 快速开始（个人多设备）
```bash
# 安装 Git 和 Git LFS（首次）
git lfs install

# 克隆仓库
git clone <仓库地址>
cd <项目目录>

# 用 Unity Hub 打开项目，等待 Library 重建
```

### 分支策略（协作推荐）
- `main`：稳定版本，只接受 Pull Request
- `dev`：开发主线
- `feature/<功能名>`：功能分支，开发完毕后合并回 dev

### 冲突处理
仓库根目录的 `.gitattributes` 已将 `.unity`/`.prefab`/`.asset`/`.mat` 等 YAML 资源标记为使用 `merge=unityyamlmerge` 合并驱动。
该驱动的可执行文件路径因本机 Unity 安装位置而异，**每台机器克隆仓库后需各自注册一次**（此配置是本地的，不会被提交）：

```bash
# Windows 示例（按实际 Unity 安装路径调整）
git config merge.unityyamlmerge.driver "\"C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data/Tools/UnityYAMLMerge.exe\" merge -p %O %B %A %A"

# macOS 示例
git config merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/6000.3.19f1/Unity.app/Contents/Tools/UnityYAMLMerge' merge -p %O %B %A %A"
```

注册后，冲突时 Git 会自动调用 UnityYAMLMerge；若自动合并失败，请在 Unity 编辑器中手动解决，然后提交。
未注册该驱动也不影响正常工作，只是场景/预制体冲突会退回 Git 默认的文本合并（更容易冲突失败)。

---

## 📂 项目结构说明

```
Assets/
├── Scripts/
│   ├── Core/          # 核心系统（GameManager, EventBus, ObjectPool...）
│   ├── Entities/      # 玩家、敌人、NPC 等实体脚本
│   ├── Weapons/       # 武器接口、策略、ScriptableObject 定义
│   ├── Commands/      # 命令模式相关类
│   ├── StateMachines/ # 状态机实现
│   └── UI/            # UI 控制脚本
├── Prefabs/          # 预制体
├── Scenes/           # 场景文件
├── Data/             # ScriptableObject 配置文件（武器、房间等）
├── Art/              # 美术资源（精灵、动画、材质）
├── Audio/            # 音效与音乐
└── ThirdParty/       # 第三方插件或工具
```

---

## 🚀 运行要求

- Unity 6000.3.19f1（建议使用相同版本打开，避免 ProjectSettings/Library 差异）
- Input System package（已包含在项目依赖中，见 `Packages/manifest.json`）
- 已配置 Git LFS 的客户端（若拉取时发现资源丢失，请执行 `git lfs pull`）

---

## 📚 参考资料

- 《游戏编程模式》（Game Programming Patterns） - Robert Nystrom
- [Unity 官方 Input System 文档](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html)
- [Catlike Coding 渲染教程](https://catlikecoding.com/unity/tutorials/rendering/)
- [UnityYAMLMerge 官方指南](https://docs.unity3d.com/Manual/SmartMerge.html)
- 免费美术素材：[Kenney.nl](https://kenney.nl/) (Rogue-like / RPG Packs)

---

## ✍️ 开发日志与标签

每完成一周的任务，建议打一个 Git 标签 `v1-week<周数>`，并在下方简要记录关键成果：
- ✅ `v1-week1`: 基础移动射击、敌人追击、对象池（已完成，详见 [devlog/week1.md](devlog/week1.md)）
- ✅ `v1-week2`: 状态机、闪避、近战武器（已完成，详见 [devlog/week2.md](devlog/week2.md)）
- `v1-week3`: EventBus、ScriptableObject 武器、策略切换
- `v1-week4`: 命令模式、输入缓冲、手雷技能
- `v1-week5`: 房间系统、Boss 战、小地图
- `v1-week6`: 打磨、特效、性能优化

---

## 📄 许可

本项目仅用于个人学习与作品集展示，代码和资源遵循对应原始来源的许可。
Kenney 素材可自由使用（具体参见其官网许可条款）。
