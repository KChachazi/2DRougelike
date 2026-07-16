# 第 5 周:房间生成与关卡流程

> 这周是**第一次把前四周的零件组合起来用**,新概念反而不多:
> - `RoomConfig` 是 ScriptableObject —— **第 3 周武器 SO 的同一套路**(数据驱动,改配置不改代码);
> - 房间清空 / 切换的通知走 **EventBus**(第 3 周);
> - 房间发**局部事件**、由 LevelManager 桥接成**全局事件** —— **第 3 周确立的事件分层**;
> - 敌人生成走**简单工厂**(唯一的新模式,而且它很简单)。
>
> 真正的重点是**关卡流程的职责划分**:谁知道"房间顺序"?谁知道"门通向哪"?谁负责"清空判定"?把这几个问题想清楚,这周就成功了一大半。
>
> 照旧分 4 步,每步可编译、可 Play 验收。参考实现在 `Reference/Scripts/...`。

## 本周目标(对齐 README 六周计划)

- **`RoomConfig`(SO)数据驱动**房间的敌人 / 道具布局。
- **简单工厂**生成敌人和道具。
- **房间切换逻辑** + **Cinemachine 摄像机平滑过渡**。
- **3~5 个房间的关卡,含 Boss 房**。
- **小地图 UI**。

完成后:进入第一个房间 → 按配置刷出敌人 → 门是红的(锁着)→ 清光敌人 → 门变绿 → 走进门 → 摄像机**平滑推移**到下一个房间 → 玩家被传送到新房间入口 → 最后一间是 Boss 房 → 全部清空后 Console 打印"通关";左上角小地图实时显示你在第几间、清了哪几间、哪间是 Boss。

---

## 0. 新建目录 + 一个关键的职责问题

### 0.1 新建目录

- `Assets/Scripts/Level/` —— 命名空间 `Game.Level`。房间/关卡系统住这里。
  > README 的项目结构里原本没有这一层(只有 Core/Entities/Weapons/Commands/StateMachines/UI)。关卡是一个独立的模块,不该硬塞进 `Core`——**`Core` 是"谁都可能用到的地基"(EventBus、对象池),不是"什么都往里扔的杂物间"**。这周结束后我会把这一条补进 CLAUDE.md 和 README 的目录约定。

### 0.2 先想清楚:谁该知道什么?

这周会出现四个角色。**在写代码之前,先决定它们各自"知道多少"**——这个决定比代码本身重要得多:

| 角色 | 它知道什么 | 它**不**知道什么 |
|---|---|---|
| `RoomConfig`(SO) | 这个房间要生成哪些敌人/道具、在什么位置 | 房间在世界的哪儿、自己是第几间 |
| `Room`(场景物体) | 自己房间里还剩几个敌人活着、门在哪、摄像机是哪台 | **自己是第几个房间**、下一个房间是谁、LevelManager 是谁 |
| `Door` | 自己锁没锁 | **自己通向哪个房间**、LevelManager 是谁 |
| `LevelManager` | **房间的顺序**、当前在第几间 | 房间内部怎么生成敌人、怎么判断清空 |

两个"不知道"特别值得琢磨:

**① `Room` 不知道自己是第几个。** 所以它清空时**只能发一个局部事件**(`RoomCleared`),说"我被清空了",而没法说"第 2 号房间被清空了"。由 `LevelManager`(它才知道 index)接住这个局部事件,**加上身份**,再广播成全局的 `RoomClearedEvent(2)`。
> 这**完全就是第 3 周 `Health` → `PlayerController` 的那套分层**:`Health` 只喊"我血变了",不认识"玩家""UI";"我是玩家的血,要给 UI 看"这层身份由 `PlayerController` 桥接。**同一个模式,第二次出现——这不是巧合,这是这套架构在起作用。**

**② `Door` 不知道自己通向哪。** 玩家走进来时,它只广播一句"有人进门了"(`DoorEnteredEvent`),去哪儿是 `LevelManager` 的事。
> 好处:门可以随便复制粘贴到任何房间,不用在 Inspector 里配"我通向谁";以后想做"两扇门选一边走"的分支关卡,只改 `LevelManager`,门一行不用动。

---

## 步骤 1:RoomConfig + 工厂 + 单个房间

**这一步做什么**:让一个房间能按 SO 配置刷出敌人,并且在敌人被清光时知道"我空了"。这一步还没有房间切换,就一个房间。

### 1.1 修改 `Assets/Scripts/Core/GameEvents.cs`

加一个 `RoomType` 枚举和四个关卡事件(完整代码见 `Reference/Scripts/Core/GameEvents.cs`):

```csharp
public enum RoomType
{
    Normal,
    Boss,
}

public readonly struct LevelStartedEvent
{
    public readonly RoomType[] RoomTypes;   // 小地图靠它知道画几个格子、哪个是 Boss
    public LevelStartedEvent(RoomType[] roomTypes) { RoomTypes = roomTypes; }
}

public readonly struct RoomEnteredEvent
{
    public readonly int Index;
    public RoomEnteredEvent(int index) { Index = index; }
}

public readonly struct RoomClearedEvent
{
    public readonly int Index;
    public RoomClearedEvent(int index) { Index = index; }
}

public readonly struct DoorEnteredEvent { }      // 空事件:只是"发生了这件事"
public readonly struct LevelCompletedEvent { }
```

> **`RoomType` 为什么放在 `Core` 而不是 `Level`?** 因为 `GameEvents`(在 `Core`)要用它。如果 `RoomType` 定义在 `Level` 里,`Core` 就得反过来 `using Game.Level` —— **底层依赖上层,依赖方向就乱了**。地基不能依赖盖在它上面的楼。这个判断以后经常要做:**当两个模块都要用一个类型时,把它放到更底层的那个模块里。**

### 1.2 新建 `Assets/Scripts/Level/RoomConfig.cs`

```csharp
using Game.Core;
using UnityEngine;

namespace Game.Level
{
    [CreateAssetMenu(fileName = "NewRoom", menuName = "Game/Room Config")]
    public class RoomConfig : ScriptableObject
    {
        public string roomName = "Room";
        public RoomType type = RoomType.Normal;

        [Tooltip("这个房间要生成的敌人(位置相对房间中心)")]
        public EnemySpawn[] enemySpawns;

        [Tooltip("这个房间要生成的道具(弹药补给等)")]
        public PickupSpawn[] pickupSpawns;
    }

    [System.Serializable]
    public struct EnemySpawn
    {
        public GameObject prefab;
        [Tooltip("相对房间中心的位置")]
        public Vector2 localPosition;
    }

    [System.Serializable]
    public struct PickupSpawn
    {
        public GameObject prefab;
        [Tooltip("相对房间中心的位置")]
        public Vector2 localPosition;
    }
}
```

讲解:
- 和 `WeaponData` 一样,**房间是数据不是代码**。加一个新房间 = 建一个资产 + 填几个敌人,不改任何脚本。
- **为什么不把敌人直接手摆进场景?** 那样布局就焊死在 `.unity` 文件里了:改起来痛苦、复用不了、多人协作还容易冲突。数据驱动之后,同一个 `RoomConfig` 可以被任何位置的房间复用。
- 位置用**相对房间原点的 `localPosition`**,不是世界坐标——这样配置才和"房间摆在哪"解耦。
- `[System.Serializable]` 是让 `struct` 能显示在 Inspector 里的必要条件(否则 `EnemySpawn[]` 在 Inspector 里根本不出现)。

### 1.3 新建 `Assets/Scripts/Level/EnemyFactory.cs`

```csharp
using UnityEngine;

namespace Game.Level
{
    public static class EnemyFactory
    {
        public static GameObject Create(GameObject prefab, Vector3 worldPosition, Transform parent)
        {
            if (prefab == null) return null;
            return Object.Instantiate(prefab, worldPosition, Quaternion.identity, parent);
        }
    }
}
```

**它薄得可笑,你可能会想"这有什么用?"** ——价值不在它现在做了什么,而在于**以后要改生成逻辑时,只有这一个地方要改**:

- 想让敌人走对象池?改这里。
- 想按关卡难度调整敌人血量?改这里。
- 想在生成时统计数量、注册到某个管理器?改这里。

如果生成散落在各处(`Room` 里 `Instantiate` 一次、Boss 房里又 `Instantiate` 一次),上面每条都要满世界改。**工厂模式买的就是这个"以后"。**

> **敌人为什么不走对象池?** 架构约定里写着"频繁 Instantiate/Destroy 的对象一律走对象池",但对象池的收益是"避免**高频**创建销毁带来的 GC 抖动"。敌人是**进房间时一次性生成一批**,频率极低,池化收益接近零,却要多管一套生命周期(复位状态、归还时机)。**架构约定是工具,不是教条**——子弹每秒几十发,那才是池化的战场。

### 1.4 新建 `Assets/Scripts/Level/Room.cs`

完整代码见 `Reference/Scripts/Level/Room.cs`。核心部分:

```csharp
public class Room : MonoBehaviour
{
    [SerializeField] private RoomConfig config;
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Door door;
    [SerializeField] private GameObject roomCamera;      // 步骤 3 才用
    [SerializeField] private Transform contentParent;

    public event Action<Room> RoomCleared;               // 局部事件!

    public RoomConfig Config => config;
    public Transform EntryPoint => entryPoint;
    public bool IsCleared { get; private set; }

    private readonly List<Health> trackedEnemies = new List<Health>();
    private int aliveCount;
    private bool spawned;

    public void Enter()
    {
        if (roomCamera != null) roomCamera.SetActive(true);

        if (!spawned)
        {
            SpawnContents();
            spawned = true;
        }

        if (aliveCount <= 0) MarkCleared();   // 空房间直接算清空,否则门永远不开
    }

    public void Exit()
    {
        if (roomCamera != null) roomCamera.SetActive(false);
    }

    private void SpawnContents()
    {
        Transform parent = contentParent != null ? contentParent : transform;

        foreach (EnemySpawn spawn in config.enemySpawns)
        {
            Vector3 worldPos = transform.position + (Vector3)spawn.localPosition;
            GameObject enemy = EnemyFactory.Create(spawn.prefab, worldPos, parent);
            if (enemy == null) continue;

            if (enemy.TryGetComponent(out Health health))
            {
                health.Died += OnEnemyDied;      // 订阅:它死了要通知我
                trackedEnemies.Add(health);      // 记着,退订时要用
                aliveCount++;
            }
        }

        foreach (PickupSpawn spawn in config.pickupSpawns)
        {
            Vector3 worldPos = transform.position + (Vector3)spawn.localPosition;
            EnemyFactory.Create(spawn.prefab, worldPos, parent);
        }
    }

    private void OnEnemyDied()
    {
        aliveCount--;
        if (aliveCount <= 0 && !IsCleared) MarkCleared();
    }

    private void MarkCleared()
    {
        IsCleared = true;
        if (door != null) door.Unlock();
        RoomCleared?.Invoke(this);   // 只说"我空了",不说"第几号房间空了"——它不知道
    }

    private void OnDestroy()
    {
        // 订阅必须成对退订(week3/week4 各栽过一次,这次别再犯)
        foreach (Health health in trackedEnemies)
        {
            if (health != null) health.Died -= OnEnemyDied;
        }
        trackedEnemies.Clear();
    }
}
```

几个关键点:

1. **清空判定靠订阅 `Health.Died` 计数**,不是每帧去数场景里还有几个敌人(那是轮询,又慢又丑)。`Health.Died` 是**无参事件**,所以 `OnEnemyDied()` 不知道具体是谁死了——但我们**只需要计数**,够用。
2. **`RoomCleared` 是局部 C# 事件,不是 EventBus**。因为 `Room` 不知道自己是第几号,发不出有意义的全局事件。这个"身份"由 `LevelManager` 在步骤 2 补上。
3. **`OnDestroy` 里退订**。`trackedEnemies` 这个列表存在的唯一理由就是"退订时要拿到这些 Health"。`Subscribe`/`Unsubscribe` 配对已经错过两次了(week3 `HealthBarUI`、week4 `CooldownUI`),这次留个心。
4. **空房间要特判**:如果 `RoomConfig` 一个敌人都没配,`aliveCount` 就是 0,永远等不到 `OnEnemyDied` —— 门就永远不会开。所以 `Enter()` 末尾要判一次 `aliveCount <= 0`。**这类"边界情况"是关卡系统最容易卡死玩家的地方。**

### 1.5 Unity 编辑器操作

**A. 搭一个房间**

1. Hierarchy 右键 `Create Empty`,改名 `Room_1`,把它的 `Position` 设成 `(0, 0, 0)`。
2. 右键 `Room_1` → `Create Empty`,改名 `EntryPoint`,`Position` 设成 `(-6, 0, 0)`(房间左侧,玩家从这儿进)。
3. 右键 `Room_1` → `Create Empty`,改名 `Contents`(生成出来的敌人会挂在这下面,保持 Hierarchy 整洁)。
4. **围墙**(防止玩家跑出房间):右键 `Room_1` → `2D Object > Sprites > Square`,改名 `Wall_Top`,`Scale` 设 `(20, 1, 1)`、`Position` 设 `(0, 6, 0)`,加一个 `BoxCollider2D`。照这样再做 `Wall_Bottom`(`0,-6`)、`Wall_Left`(`-10, 0`,Scale `1,12,1`)、`Wall_Right`(`10, 0`,Scale `1,12,1`)。颜色调暗一点。
5. 选中 `Room_1` → `Add Component` → `Room`。`Entry Point` 拖 `EntryPoint`,`Content Parent` 拖 `Contents`,`Door` 和 `Room Camera` 先留空(步骤 2、3 再配)。

**B. 建房间配置**

1. `Assets/Data/` 右键 → `Create > Game > Room Config`,建一个 `Room1Config`。
2. `Room Name` 填 `Room 1`,`Type` 选 `Normal`。
3. `Enemy Spawns` 的 `Size` 设 `3`,每个元素:
   - `Prefab` 拖 `Assets/Prefabs/Enemy.prefab`;
   - `Local Position` 分别填 `(3, 2)`、`(5, -1)`、`(2, -3)`(相对房间中心)。
4. `Pickup Spawns` 的 `Size` 设 `1`,`Prefab` 拖 `AmmoPickup.prefab`,`Local Position` 填 `(0, 3)`。
5. 回到 `Room_1` 的 `Room` 组件,把 `Room1Config` 拖进 `Config` 槽。

**C. 清掉场景里手摆的敌人**

场景里之前手动摆的那个 `Enemy` 现在**删掉**(敌人从此由房间生成)。玩家 `Player` 留着,把它挪到 `(-6, 0)` 附近。

**D. 临时验证清空**

`Room.MarkCleared()` 里已经有 `RoomCleared?.Invoke(this)`,但现在还没人订阅。为了这一步能验收,**临时**在 `MarkCleared()` 里加一行:

```csharp
private void MarkCleared()
{
    IsCleared = true;
    if (door != null) door.Unlock();
    Debug.Log($"[Room] {name} 已清空!");   // 临时:步骤 2 接上 LevelManager 后可以删
    RoomCleared?.Invoke(this);
}
```

**E. 谁来调 `Room.Enter()`?**

步骤 2 的 `LevelManager` 才会调它。这一步为了能测,**临时**在 `Room` 里加一句:

```csharp
private void Start()
{
    Enter();   // 临时:步骤 2 有了 LevelManager 之后删掉这个 Start
}
```

### ✅ 步骤 1 验收

- [ ] Play 时房间里**自动刷出 3 个敌人**(位置和 `RoomConfig` 里配的一致),外加一个弹药补给。
- [ ] 生成的敌人挂在 `Room_1 > Contents` 下面(Hierarchy 里能看到)。
- [ ] 敌人 AI 正常(会追你、打你),你能打死它们。
- [ ] **把 3 个敌人全部杀光** → Console 打印 `[Room] Room_1 已清空!`。
- [ ] 改 `RoomConfig` 里的敌人数量/位置,重新 Play,刷出来的敌人跟着变(**数据驱动生效**)。
- [ ] Console 无报错。

---

## 步骤 2:门 + LevelManager(房间切换)

**这一步做什么**:做 3 个房间串成一条线,清空后开门,走进门去下一间。

### 2.1 新建 `Assets/Scripts/Level/Door.cs`

```csharp
using Game.Core;
using UnityEngine;

namespace Game.Level
{
    [RequireComponent(typeof(Collider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color lockedColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color unlockedColor = new Color(0.2f, 0.8f, 0.3f);

        private bool locked = true;

        private void Awake() => Lock();

        public void Lock()
        {
            locked = true;
            if (spriteRenderer != null) spriteRenderer.color = lockedColor;
        }

        public void Unlock()
        {
            locked = false;
            if (spriteRenderer != null) spriteRenderer.color = unlockedColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (locked) return;
            if (!other.CompareTag("Player")) return;

            EventBus.Publish(new DoorEnteredEvent());   // 只说"有人进门了"
        }
    }
}
```

**注意这个类有多"蠢"**——它不知道自己通向哪个房间,也不认识 `LevelManager`。这是**故意的**,好处见 0.2 节。

### 2.2 新建 `Assets/Scripts/Level/LevelManager.cs`

完整代码见 `Reference/Scripts/Level/LevelManager.cs`。核心:

```csharp
public class LevelManager : MonoBehaviour
{
    [SerializeField] private Room[] rooms;   // 按顺序拖进来

    private int currentIndex = -1;

    private void OnEnable()
    {
        EventBus.Subscribe<DoorEnteredEvent>(OnDoorEntered);
        foreach (Room room in rooms)
            if (room != null) room.RoomCleared += OnRoomCleared;
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DoorEnteredEvent>(OnDoorEntered);
        foreach (Room room in rooms)
            if (room != null) room.RoomCleared -= OnRoomCleared;
    }

    private void Start()
    {
        // 广播关卡结构(小地图用),放 Start 保证 UI 的 OnEnable 已经订阅完了
        RoomType[] types = new RoomType[rooms.Length];
        for (int i = 0; i < rooms.Length; i++)
            types[i] = rooms[i].Config != null ? rooms[i].Config.type : RoomType.Normal;
        EventBus.Publish(new LevelStartedEvent(types));

        EnterRoom(0);
    }

    private void OnDoorEntered(DoorEnteredEvent e) => EnterRoom(currentIndex + 1);

    private void EnterRoom(int index)
    {
        if (index < 0 || index >= rooms.Length)
        {
            EventBus.Publish(new LevelCompletedEvent());
            Debug.Log("[LevelManager] 通关!");
            return;
        }

        if (currentIndex >= 0) rooms[currentIndex].Exit();

        currentIndex = index;
        Room room = rooms[index];

        TeleportPlayer(room.EntryPoint);
        room.Enter();

        EventBus.Publish(new RoomEnteredEvent(index));
    }

    // Room 的局部事件 → 补上"你是第几个"这个身份 → 广播成全局事件
    private void OnRoomCleared(Room room)
    {
        int index = System.Array.IndexOf(rooms, room);
        if (index < 0) return;
        EventBus.Publish(new RoomClearedEvent(index));
    }

    private void TeleportPlayer(Transform entry)
    {
        GameObject player = GameManager.Instance != null
            ? GameManager.Instance.Player
            : GameObject.FindGameObjectWithTag("Player");
        if (player == null || entry == null) return;

        if (player.TryGetComponent(out Rigidbody2D rb))
        {
            rb.position = entry.position;          // 不要直接改 transform!
            rb.linearVelocity = Vector2.zero;
        }
    }
}
```

两个要点:

**① 传送玩家要改 `Rigidbody2D.position`,不是 `transform.position`。** 玩家是 Dynamic 刚体,直接改 `transform` 会和物理引擎打架(位置被物理系统覆盖、或者穿模)。改 `rb.position` 是"告诉物理引擎我瞬移了"。顺手清一下 `linearVelocity`,防止把速度带到新房间。

**② `OnRoomCleared` 就是那个"桥接"。** `Room` 发来的局部事件里只有"我"(`Room` 对象),`LevelManager` 用 `Array.IndexOf` 查出它是第几个,然后广播带 index 的全局事件。**这和第 3 周 `PlayerController` 把 `Health.HealthChanged` 桥接成 `PlayerHealthChangedEvent` 是一模一样的动作。**

### 2.3 删掉步骤 1 的临时代码

- `Room.Start()` 里那句 `Enter();` —— **删掉**(现在由 `LevelManager` 调)。
- `MarkCleared()` 里的 `Debug.Log` —— 可以删了(小地图会显示)。

### 2.4 Unity 编辑器操作

**A. 做门**

1. 右键 `Room_1` → `2D Object > Sprites > Square`,改名 `Door`。
2. `Position` 设 `(9, 0, 0)`(房间右墙上),`Scale` 设 `(1, 2, 1)`。
3. 它自带的 `BoxCollider2D` → **勾上 `Is Trigger`**。
4. `Add Component` → `Door`,把它自己的 `SpriteRenderer` 拖进 `Sprite Renderer` 槽。
5. 回到 `Room_1` 的 `Room` 组件,把 `Door` 拖进 `Door` 槽。
6. **注意**:`Wall_Right` 挡在门的位置上,把它改成两段(上下各一段,中间留出门的缺口),或者干脆把 `Wall_Right` 删掉——否则玩家走不到门那儿。

**B. 复制出 3 个房间**

1. 选中 `Room_1` → `Ctrl+D` 复制两份,改名 `Room_2`、`Room_3`。
2. `Room_2` 的 `Position` 设 `(30, 0, 0)`,`Room_3` 设 `(60, 0, 0)`(**房间之间隔开,互不干扰**)。
3. 各自建一个 `RoomConfig`(`Room2Config`、`Room3Config`),敌人数量/位置调得不一样,拖进各自的 `Room` 组件。
4. **`Room_3` 是最后一间**:它的 `Door` 可以删掉(或留着不管——`LevelManager` 走到头会打印"通关")。

**C. 建 LevelManager**

1. Hierarchy 右键 `Create Empty`,改名 `LevelManager`。
2. `Add Component` → `LevelManager`。
3. `Rooms` 数组 `Size` 设 `3`,**按顺序**拖入 `Room_1`、`Room_2`、`Room_3`。

### ✅ 步骤 2 验收

- [ ] Play 时玩家自动被放到 `Room_1` 的 `EntryPoint`,房间刷出敌人。
- [ ] 门是**红色**的,走上去**没有反应**(锁着)。
- [ ] 清光房间里的敌人 → 门变成**绿色**。
- [ ] 走进绿门 → **瞬间传送**到 `Room_2` 的入口,`Room_2` 刷出敌人。
- [ ] `Room_2` 清空 → 走门 → 到 `Room_3`。
- [ ] `Room_3` 清空后(如果留了门)走门 → Console 打印 `[LevelManager] 通关!`。
- [ ] 传送后玩家**不会带着速度乱飘**(`linearVelocity` 被清零了)。
- [ ] Console 无报错。

> 摄像机现在还是硬切/或者跟着玩家瞬移过去,很难看——步骤 3 解决。

---

## 步骤 3:Cinemachine 摄像机平滑过渡

**这一步做什么**:装 Cinemachine,每个房间放一台摄像机,切房间时**平滑推移**过去(元气骑士式的"一个房间一个固定视角")。

### 3.1 安装 Cinemachine

1. 菜单 `Window > Package Manager`。
2. 左上角下拉选 **`Unity Registry`**。
3. 搜 **`Cinemachine`** → 选中 → 右下角 **`Install`**。
4. 等它装完(可能要重新编译一会儿)。

> 装完记得在 README「运行要求」里补一笔(这周结束时我会更新)。

### 3.2 一个重要的设计决定:代码里不碰 Cinemachine

`Room.cs` 里摄像机字段的类型是 **`GameObject`**,不是 `CinemachineCamera`:

```csharp
[SerializeField] private GameObject roomCamera;   // 不是 CinemachineCamera!

public void Enter()  { if (roomCamera != null) roomCamera.SetActive(true); }
public void Exit()   { if (roomCamera != null) roomCamera.SetActive(false); }
```

**这是故意的。** Cinemachine 2.x 和 3.x 的类名、命名空间完全不同(`Cinemachine.CinemachineVirtualCamera` vs `Unity.Cinemachine.CinemachineCamera`),连 `Priority` 的类型都变了。**把这些写进代码,就等于被包版本绑架**——将来升级 Cinemachine,你的 `Room.cs` 就编译不过了。

而 Cinemachine 的核心机制是:**Brain 会自动在"当前激活的虚拟摄像机"之间做平滑过渡**。所以我们只要 `SetActive` 切换就行了——这是官方支持的用法,而我们的代码**一行 `using Cinemachine` 都不用写**。

> 这个思路值得记住:**当你依赖一个"可能会变"的外部东西时,尽量只依赖它最稳定的那一面。** `GameObject.SetActive` 十年不会变,`CinemachineCamera` 的 API 说变就变。

### 3.3 Unity 编辑器操作

**A. 给主摄像机装 Brain**

1. 选中 Hierarchy 里的 `Main Camera`。
2. **把第 1 周的 `CameraFollow` 组件禁用或移除**(Cinemachine 要接管摄像机了,两个一起动会打架)。
3. `Add Component` → 搜 **`CinemachineBrain`** → 添加。
4. `CinemachineBrain` 的 **`Default Blend`** 设成 `Ease In Out`,时间 `0.8` 秒(这就是房间之间过渡的手感,可以自己调)。

**B. 给每个房间放一台摄像机**

> ⚠️ **本项目装的是 Cinemachine 3.1.7**。3.x 和 2.x 的 Inspector 字段名不一样,下面按 **3.x** 写(括号里是 2.x 的旧名,以防你搜到老教程时对不上):
>
> | 3.x(我们用的) | 2.x(老教程里的) |
> |---|---|
> | `CinemachineCamera` 组件 | `CinemachineVirtualCamera` |
> | **`Tracking Target`**(一个槽) | `Follow` + `Look At`(两个槽) |
> | `Lens` **默认折叠**,要点 ▶ 展开 | `Lens` 默认展开 |

对 `Room_1` / `Room_2` / `Room_3` 各做一遍:

1. 右键房间物体(如 `Room_1`)→ `Cinemachine > Cinemachine Camera`,改名 `RoomCamera`。
2. 把它的 `Position` 设成 **`(0, 0, -10)`**(相对房间中心,Z 要是负的,不然拍不到东西)。
3. **`Tracking Target` 留空**(新建时默认就是 `None`,不用动)——我们要的是**固定视角**(房间内镜头不动,元气骑士就是这样),不是跟随玩家。
   > 2.x 里这是 `Follow` 和 `Look At` 两个槽,3.x 合并成了一个 `Tracking Target`。
4. **点 `Lens` 左边的 ▶ 展开**,把 `Orthographic Size` 调成 `7` 左右(能看到整个房间就行,自己调)。
   > 折叠状态下 `Lens` 那一行只显示当前值的摘要(比如 `19.2857`),看不到具体字段——**必须展开**。
5. `Priority` 显示 `(using default)`,**不用动**——我们靠 `SetActive` 切换摄像机,不靠优先级。
6. **把这台摄像机的 GameObject 取消勾选(设为 inactive)**——`Room.Awake()` 里也会关掉它,但在编辑器里就关好更清楚。
7. 回到该房间的 `Room` 组件,把 `RoomCamera` 拖进 **`Room Camera`** 槽。

> 💡 **顺便体会一下 3.2 那个决定的价值**:Cinemachine 从 2.x 到 3.x,类名、命名空间、字段名全变了。如果这些写进了 `Room.cs`,你现在面对的就不是"字段改名了",而是一屏幕编译错误。而我们的代码里只有 `GameObject` + `SetActive` —— **版本怎么变都编译得过,代价只是文档里的菜单路径要更新**。

### ✅ 步骤 3 验收

- [ ] Play 开始时,摄像机对准 `Room_1`(不再跟着玩家跑)。
- [ ] 清空房间、走进门 → 摄像机**平滑地推移**到 `Room_2`(不是瞬间硬切),过渡大约 0.8 秒。
- [ ] 过渡期间玩家已经被传送到新房间,镜头追上来时他正好在入口。
- [ ] 三个房间的过渡都正常。
- [ ] 把 `CinemachineBrain` 的 `Default Blend` 改成 `Cut`,过渡会变成硬切——**改回 `Ease In Out`**,体会一下差别。
- [ ] Console 无报错。

---

## 步骤 4:小地图 + Boss 房

**这一步做什么**:左上角一排格子显示关卡进度;最后一间做成 Boss 房。

### 4.1 新建 `Assets/Scripts/UI/MinimapUI.cs`

完整代码见 `Reference/Scripts/UI/MinimapUI.cs`。核心:

```csharp
public class MinimapUI : MonoBehaviour
{
    [SerializeField] private Transform iconContainer;   // 挂 Horizontal Layout Group
    [SerializeField] private Image iconPrefab;

    [Header("颜色")]
    [SerializeField] private Color currentColor   = new Color(1f, 0.9f, 0.2f);      // 当前:黄
    [SerializeField] private Color clearedColor   = new Color(0.3f, 0.7f, 0.4f);    // 已清:绿
    [SerializeField] private Color bossColor      = new Color(0.85f, 0.25f, 0.25f); // Boss:红
    [SerializeField] private Color unvisitedColor = new Color(0.35f, 0.35f, 0.4f);  // 没去过:灰

    private Image[] icons;
    private RoomType[] roomTypes;
    private bool[] clearedFlags;
    private int currentIndex = -1;

    private void OnEnable()
    {
        EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
        EventBus.Subscribe<RoomEnteredEvent>(OnRoomEntered);
        EventBus.Subscribe<RoomClearedEvent>(OnRoomCleared);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
        EventBus.Unsubscribe<RoomEnteredEvent>(OnRoomEntered);
        EventBus.Unsubscribe<RoomClearedEvent>(OnRoomCleared);
    }

    private void OnLevelStarted(LevelStartedEvent e)
    {
        // 按房间数生成格子
        roomTypes = e.RoomTypes;
        clearedFlags = new bool[roomTypes.Length];
        icons = new Image[roomTypes.Length];

        for (int i = 0; i < roomTypes.Length; i++)
            icons[i] = Instantiate(iconPrefab, iconContainer);

        Refresh();
    }

    private void OnRoomEntered(RoomEnteredEvent e) { currentIndex = e.Index; Refresh(); }
    private void OnRoomCleared(RoomClearedEvent e) { clearedFlags[e.Index] = true; Refresh(); }

    private void Refresh()
    {
        if (icons == null) return;
        for (int i = 0; i < icons.Length; i++)
            icons[i].color = GetColor(i);
    }

    // 优先级:当前 > 已清空 > Boss(未清) > 没去过
    private Color GetColor(int index)
    {
        if (index == currentIndex) return currentColor;
        if (clearedFlags[index]) return clearedColor;
        if (roomTypes[index] == RoomType.Boss) return bossColor;
        return unvisitedColor;
    }
}
```

> 注意它**不认识 `LevelManager`,也不认识 `Room`**,只订阅三个事件。关卡结构怎么变,小地图代码一行不用改——**这就是三周前搭 EventBus 换来的回报**。
>
> 还有个老规矩又用上了:`LevelManager` 在 **`Start`** 里广播 `LevelStartedEvent`,而 `MinimapUI` 在 **`OnEnable`** 里订阅。Unity 保证所有 `OnEnable` 先于所有 `Start`,所以小地图**一定**收得到开场那一次广播。

### 4.2 Unity 编辑器操作

**A. 小地图**

1. 右键 `Canvas` → `Create Empty`,改名 `Minimap`。锚点用 `Alt+Shift` 定到**左上角**,`Pos X = 130`、`Pos Y = -90`(在血条下面),`Width = 200`、`Height = 40`。
2. `Add Component` → **`Horizontal Layout Group`**:`Spacing` 设 `6`,`Child Alignment` 设 `Middle Left`,`Child Force Expand` 的两个勾**都取消**。
3. `Add Component` → **`Content Size Fitter`**(可选):`Horizontal Fit` 设 `Preferred Size`。
4. **做格子预制体**:右键 `Canvas` → `UI > Image`,改名 `RoomIcon`,`Width/Height` 都设 `28`,`Source Image` 选你的 `Square`。把它**拖进 `Assets/Prefabs/`** 做成预制体,然后**删掉场景里的那个**。
5. 选中 `Minimap` → `Add Component` → `MinimapUI`:
   - `Icon Container` 拖 `Minimap` **自己**;
   - `Icon Prefab` 拖 `Assets/Prefabs/RoomIcon.prefab`。

**B. Boss 房**

1. **做 Boss 预制体**:把 `Assets/Prefabs/Enemy.prefab` 复制一份(`Ctrl+D`),改名 `Boss`。
   - `Transform > Scale` 设 `(2, 2, 1)`(大一圈);
   - `Health > Max Health` 设 `500`;
   - `EnemyController` 的 `Contact Damage` 设 `20`、`Chase Speed` 设 `2`(慢但疼);
   - `SpriteRenderer > Color` 调成深紫色。
2. **建 Boss 房配置**:`Assets/Data/` 右键 → `Create > Game > Room Config`,建 `BossRoomConfig`。
   - `Room Name` 填 `Boss Room`,**`Type` 选 `Boss`**;
   - `Enemy Spawns` 只放 **1 个**:`Prefab` 拖 `Boss.prefab`,`Local Position` 填 `(4, 0)`;
   - `Pickup Spawns` 可以放 2 个弹药补给(不然打不动)。
3. 把 `Room_3` 的 `Config` 换成 `BossRoomConfig`。

### ✅ 步骤 4 验收

- [ ] 左上角出现 **3 个格子**(和房间数一致)。
- [ ] 开局第 1 格是**黄的**(当前),第 2 格**灰的**(没去过),第 3 格**红的**(Boss 房)。
- [ ] 清空 `Room_1` → 第 1 格变**绿**(已清空)。
- [ ] 走门进 `Room_2` → 第 2 格变**黄**(当前),第 1 格保持绿。
- [ ] 进入 Boss 房 → 第 3 格变黄;里面是**一只又大又紫、血很厚**的 Boss。
- [ ] 打死 Boss → 第 3 格变绿,Console 打印通关(如果 Boss 房留了门,走进去才打印)。
- [ ] Console 无报错。

---

## 常见问题排查

| 现象 | 可能原因 | 排查 |
|---|---|---|
| 房间不刷敌人 | `Room` 的 `Config` 没拖,或 `Enemy Spawns` 的 `Prefab` 是空的 | 检查 Inspector |
| 敌人刷在奇怪的位置 | `localPosition` 被当成了世界坐标 | 生成时要 `transform.position + (Vector3)spawn.localPosition` |
| 敌人全死了门也不开 | `aliveCount` 没减到 0(有敌人没订阅上 `Died`),或房间压根没敌人 | 确认敌人预制体上有 `Health`;空房间要靠 `Enter()` 末尾那句 `if (aliveCount <= 0)` 兜底 |
| 门变绿了但走上去没反应 | 门的 `Collider2D` 没勾 `Is Trigger`;或玩家 Tag 不是 `Player` | 勾 `Is Trigger`;检查 Tag |
| 走门后玩家没被传送 | `EntryPoint` 没拖;或 `GameManager.Instance.Player` 是空的 | 检查 Inspector;`LevelManager` 里有 `FindGameObjectWithTag` 兜底 |
| 传送后玩家一直往一个方向飘 | 传送时没清 `linearVelocity` | `rb.linearVelocity = Vector2.zero;`(和 week2 那个"被撞后漂移"是同一类问题) |
| Inspector 里找不到 `Follow` / `Look At` | 装的是 Cinemachine **3.x**,这两个槽合并成了 **`Tracking Target`** | 留空即可(固定视角就是要它空着) |
| Inspector 里找不到 `Orthographic Size` | 3.x 的 `Lens` **默认折叠**,只显示一个摘要数值 | 点 `Lens` 左边的 ▶ 展开 |
| 摄像机不动 / 还跟着玩家 | `Main Camera` 上的 `CameraFollow` 没禁用;或没加 `CinemachineBrain` | 禁用 `CameraFollow`,加 `CinemachineBrain` |
| 摄像机拍不到东西(全黑/灰) | 房间摄像机的 `Position.z` 不是负数 | 设成 `(0, 0, -10)` |
| 房间切换是硬切,没有平滑过渡 | `CinemachineBrain` 的 `Default Blend` 是 `Cut` | 改成 `Ease In Out`,时间 0.8 秒 |
| 小地图没有格子 | `LevelStartedEvent` 广播时 `MinimapUI` 还没订阅 | 广播必须在 `Start`、订阅必须在 `OnEnable`(老规矩) |
| 小地图格子叠在一起 | `Minimap` 上没挂 `Horizontal Layout Group` | 加上,`Spacing` 设 6 |
| 打不过 Boss | 弹药不够 | 在 `BossRoomConfig` 的 `Pickup Spawns` 里多放几个补给;或调低 Boss 的 `Max Health` |

---

## 本周验收总 checklist

- [x] 房间内容由 `RoomConfig`(SO)驱动,改配置就能改布局,**不用改代码、不用手摆敌人**。
- [x] 敌人/道具生成统一走 `EnemyFactory`(简单工厂),生成逻辑只有一个入口。
- [x] `Room` 靠订阅 `Health.Died` 计数来判断清空,**不轮询**;清空后开门。
- [x] `Room` 发局部事件、`LevelManager` 桥接成全局事件——**和第 3 周 `Health` → `PlayerController` 是同一个分层**。
- [x] `Door` 不认识 `LevelManager`,`MinimapUI` 不认识 `Room`——**模块间零硬引用**。
- [x] 3 个房间串成关卡,清空 → 开门 → 传送 → Cinemachine 平滑过渡。
- [x] 最后一间是 Boss 房,小地图正确显示当前/已清空/Boss。
- [x] 第 1~4 周的功能(移动/闪避/射击/切枪/近战/手雷/输入缓冲/血条/弹药/冷却 UI)全部未被破坏。

**四步全部 Play 验收通过,第 5 周完成。**

---

## 实际完成记录(这一节是"发生了什么",不是计划)

### 落地的文件

**新增(6 个脚本)**

- `Level/`(新目录,命名空间 `Game.Level`):`RoomConfig`(SO + `EnemySpawn`/`PickupSpawn` 两个可序列化 struct)、`EnemyFactory`(静态简单工厂)、`Room`、`Door`、`LevelManager`。
- `UI/MinimapUI.cs`。

**修改(1 个)**

- `Core/GameEvents.cs`——新增 `RoomType` 枚举(`Normal`/`Boss`)+ 5 个关卡事件:`LevelStartedEvent(RoomType[])`、`RoomEnteredEvent(int)`、`RoomClearedEvent(int)`、`DoorEnteredEvent`(空)、`LevelCompletedEvent`(空)。

**资产 / 场景**

- `Assets/Prefabs/Room.prefab`——**房间做成了预制体**(比原计划的"Ctrl+D 复制三份"更好:改围墙/门样式只需改一处,三个实例各自 override `config` 和位置)。
- `Assets/Prefabs/Boss.prefab`——Scale 2、紫色、300 血、`contactDamage` 20、`chaseSpeed` 2(大、慢、疼)。
- `Assets/Prefabs/RoomIcon.prefab`——小地图格子。
- `Assets/Data/`:`Room_1_Config`、`Room_2_Config`、`BossRoomConfig`(`type: Boss` + 1 只 Boss + 3 个补给)。
- 场景:3 个 Room 预制体实例(x = 0 / 30 / 60)、`LevelManager`、每个房间一台 `CinemachineCamera`(inactive)、`Main Camera` 换成 `CinemachineBrain`(`CameraFollow` 已禁用)、Canvas 加 `Minimap`(Horizontal Layout Group)。
- **Cinemachine 3.1.7** 通过 Package Manager 安装。

### 踩过的坑

1. **杀完敌人 Console 不提示"已清空"**——虚惊一场,是**看漏了**(日志其实打出来了)。但这暴露了一个诊断习惯:遇到"没反应"先确认 **Console 的 `Log` 过滤按钮是否被关掉**,再怀疑代码。

2. **`private void OnDestory()` 拼写错误**——少了一个 `r`。`OnDestroy` 是 Unity 的**魔法方法**,靠**方法名字符串**匹配调用,不是靠接口/override。拼错一个字母,Unity 就永远不调用它,**而编译器一个字都不会报**(在它看来这只是个没人调用的私有方法)。后果是 `trackedEnemies` 的订阅永远不退订。
   > **写 Unity 消息方法(`Awake`/`Start`/`OnEnable`/`OnDisable`/`OnDestroy`/`OnTriggerEnter2D`…)时留意拼写**:大多数 IDE 会给它们特殊高亮,**如果某个方法名没有变色,八成就是拼错了**。

3. **"以为改了、其实没存"**——`Content Parent` 拖了但没 `Ctrl+S`,场景文件里仍是 `{fileID: 0}`。Unity 的 Inspector 改动(包括 **ScriptableObject 资产**)必须保存才会落盘。配引用配到一半去 Play,报空引用时很容易误以为是代码错了。

4. **`LevelManager.currentIndex` 初始值写成了 `1`(应为 `-1`)**——`-1` 的语义是"还没进过任何房间",于是 `if (currentIndex >= 0) rooms[currentIndex].Exit();` 读作"如果之前待过房间,先退出它"。写成 `1` 后,开局 `EnterRoom(0)` 会先执行 `rooms[1].Exit()` ——**"离开"一个你还没去过的房间**。
   > **它为什么没炸**:正好有 3 个房间(`rooms[1]` 不越界),且 `Exit()` 只是关摄像机(而 Room_2 的摄像机本来就是关的)。**房间数减到 1 的那一刻就会 `IndexOutOfRangeException`**。和第 4 周 `SwitchWeaponCommand` 的 `index <= WeaponCount` 是同一类:**边界/哨兵值写错,被更宽松的数据规模掩盖着**。

5. **Cinemachine 3.x 的 Inspector 字段和教程对不上**——`Follow`/`Look At` 合并成了 **`Tracking Target`**,`Lens` **默认折叠**(要点 ▶ 展开才看得到 `Orthographic Size`)。
   > **但这只是文档问题,不是代码问题**——因为 `Room` 的摄像机字段类型是 `GameObject`、只用 `SetActive`,**一行 Cinemachine API 都没引用**。如果当初写了 `CinemachineVirtualCamera`,现在面对的就不是"字段改名了",而是一屏幕编译错误。**依赖会变的外部库时,只依赖它最稳定的那一面。**

6. **Boss 不是紫色的**——`EnemyPatrolState.Enter()` 里硬编码了 `enemy.SpriteRenderer.color = Color.white;`,Boss 一进巡逻状态就被刷成白色。
   > **第一次的修法是直接删掉那行——这治好了 Boss,却弄坏了普通敌人**:`Chase.Enter()` 变橙、`Chase.Exit()` 是空的、`Patrol.Enter()` 又不再设颜色 → 敌人**追击一次后永远卡在橙色**,"巡逻白/追击橙"的反馈坏了一半。
   > **根因是硬编码颜色内含了一个假设:"所有敌人本色都是白的"**。正确修法是**让每个敌人记住自己的本色**:`EnemyController.Awake` 里 `OriginalColor = SpriteRenderer.color`,`EnemyPatrolState.Enter` 恢复到 `enemy.OriginalColor`。这样普通敌人(白)和 Boss(紫)各自保持,以后加绿史莱姆、蓝法师也不用再动状态类。
   > 这个模式第 2 周就用过——`PlayerHurtState` 的 `originalColor = player.SpriteRenderer.color` + `Exit()` 还原。**别假设别人长什么样,先记下来再改,改完还原。**

> 第 4 条又是一个**"程序照常跑、验收照常过"的沉默 bug**(第 8 个了)。第 6 条则是另一类教训:**修 bug 时只盯着症状,容易引入回归**——删掉一行让 Boss 变紫了,但没问"这行原本是干嘛的"。

## 课后练习(选做)

1. **房间清空时给奖励**:清空后在房间中央生成一个补给(或者随机一把武器)。想想:**这个逻辑该写在 `Room` 里,还是订阅 `RoomClearedEvent` 的另一个组件里?** (提示:如果写进 `Room`,它就同时管"生成""判定""奖励"三件事了。)
2. **Boss 血条**:Boss 出场时在屏幕上方显示一条大血条。你已经有 `Health.HealthChanged` 和 `EventBus` 了——需要新加事件吗?还是能复用现成的?
3. **让房间可以重进**:现在 `Room` 用 `spawned` 标记保证只生成一次。如果想做"可以回上一个房间",需要处理哪些状态?(提示:敌人已经死了,`trackedEnemies` 里全是空引用……)

## 下周预告:第 6 周 - 打磨与收尾

音效、粒子特效、屏幕震动、性能分析(Profiler)、构建打包。第 6 周会把这五周的东西整体过一遍,补上"让它像个游戏"的最后一层。
