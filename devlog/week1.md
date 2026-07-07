# 第 1 周:项目搭建与基础移动射击

> 本文档面向第一次做 Unity 个人项目的你,尽量把每一步"在编辑器里具体点哪里"都写清楚。遇到卡住的地方,直接把报错信息或现象告诉我就行,不用先自己查半天。

## 本周目标(对齐 README 六周计划)

- Unity 2D 项目细化(确认 Input System 已正确接入)
- 玩家八方向移动 + 面向鼠标旋转
- 单发子弹射击(对象池雏形)
- 敌人追击玩家 + 碰撞造成伤害
- 一个"最小"的 Game Loop 框架(现在先是一个占位的 GameManager,后面几周会往里面加东西)

完成后你应该能在 Play 模式里:用 WASD 移动一个方块,鼠标移到哪方块就转向哪,按住鼠标左键连续发射子弹,子弹打中敌人会掉血,敌人会追过来撞你并且每秒扣一次血。

---

## 0. 开始前必须检查一件事:Active Input Handling

代码里我们用的是新版 Input System 的 `Keyboard.current` / `Mouse.current`,如果项目还在用旧版 Input Manager,这些会是 `null`,移动和开火都不会有反应。

**检查方法**:菜单栏 `Edit > Project Settings > Player > Other Settings > Active Input Handling`,确认是 **`Input System Package (New)`** 或 **`Both`**(不是单独的 `Input Manager (Old)`)。因为项目模板里已经有 `InputSystem_Actions.inputactions`,大概率已经是对的,但改完这一项 Unity 会提示重启编辑器,照做就行。

---

## 1. 为什么这么设计(先讲思路,再讲怎么操作)

对照 `CLAUDE.md` 里定下的架构约定:

- **Game Loop(Update/FixedUpdate 分离)**:所有涉及物理位移的代码(移动、追击)都写在 `FixedUpdate` 里,因为它以固定时间步长运行,和物理引擎(Rigidbody2D)同步,不会因为帧率波动而出现移动速度不稳定。朝向鼠标旋转、读取按键这类"感知输入"的代码放在 `Update` 里,因为它需要尽可能贴近每一帧的真实输入,不需要和物理步长绑定。
- **对象池**:子弹是这个游戏里创建/销毁最频繁的对象,如果每次开火都 `Instantiate`、打中/超时就 `Destroy`,会产生大量垃圾回收(GC),长时间游玩会卡顿。所以第一周就先把对象池的雏形(`ObjectPool.cs`)搭出来,子弹从池子里"借出来",用完"还回去",而不是真的销毁。这个池子设计成通用的(认 `IPoolable` 接口),以后特效、敌人也能直接复用,不用重写。
- **命名空间约定**:这是本项目第一批代码,顺带定一个规矩——命名空间和 `Assets/Scripts` 下的子目录一一对应:`Game.Core`、`Game.Entities`、`Game.Weapons`(以后还会有 `Game.Commands`、`Game.StateMachines`、`Game.UI`)。这样看 `using` 就知道这个类归哪个目录管。
- **最小 Game Loop**:`GameManager` 现在几乎是空的,只做了一件事——用单例模式(`Instance`)保存对玩家的引用。**这是故意的**,不是没写完。第 3 周接入 EventBus 之后,模块间通信会改走事件,而不是互相找引用,所以现在不值得往 GameManager 里塞逻辑,以免下周又要推翻重写。

---

## 2. 本周需要在 `Assets/Scripts/` 下创建的文件

```
Assets/Scripts/Core/ObjectPool.cs        # 通用对象池
Assets/Scripts/Core/GameManager.cs       # 最小单例,占位用
Assets/Scripts/Core/CameraFollow.cs      # 简易摄像机跟随(第5周会换成 Cinemachine)
Assets/Scripts/Entities/Health.cs        # 通用血量组件,玩家和敌人共用
Assets/Scripts/Entities/PlayerController.cs  # 玩家移动 + 朝向鼠标
Assets/Scripts/Entities/EnemyController.cs   # 敌人追击 + 碰撞伤害
Assets/Scripts/Weapons/Bullet.cs         # 子弹行为
Assets/Scripts/Weapons/PlayerShooter.cs  # 玩家开火逻辑
```

> **这些文件需要你自己在 `Assets/Scripts/` 下创建**。对应的参考实现放在仓库根目录的 `Reference/Scripts/...`(与 `Assets/` 同级,已加入 `.gitignore`,不属于 Unity 工程、不会被编译,纯粹是给你抄写/对照用的样板)。下面逐个列出代码和讲解,建议自己在 IDE 里新建同名文件敲一遍——这样才会记得为什么这么写,遇到报错也更容易定位。

### 2.1 `Assets/Scripts/Core/ObjectPool.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public interface IPoolable
    {
        ObjectPool Pool { get; set; }
    }

    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int prewarmCount = 20;

        private readonly Queue<GameObject> pool = new Queue<GameObject>();

        private void Awake()
        {
            for (int i = 0; i < prewarmCount; i++)
            {
                GameObject instance = CreateInstance();
                instance.SetActive(false);
                pool.Enqueue(instance);
            }
        }

        private GameObject CreateInstance()
        {
            GameObject instance = Instantiate(prefab, transform);
            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolable.Pool = this;
            }
            return instance;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject instance = pool.Count > 0 ? pool.Dequeue() : CreateInstance();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public void Release(GameObject instance)
        {
            instance.SetActive(false);
            pool.Enqueue(instance);
        }
    }
}
```

**怎么用**:挂在一个空物体上(比如 `BulletPool`),`prefab` 拖子弹预制体,`prewarmCount` 是启动时预先生成多少个备用(避免游戏刚开始密集开火时临时 `Instantiate`)。谁想要一个实例就调 `Get(位置, 旋转)`,用完调 `Release(实例)` 还回去,而不是 `Destroy`。

如果池子里的都被借出去了(`pool.Count == 0`),`Get` 会自动再 `Instantiate` 一个新的——池子会按需扩容,不会出现"子弹打光了打不出去"的情况。

### 2.2 `Assets/Scripts/Core/GameManager.cs`

```csharp
using UnityEngine;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameObject player;

        public GameObject Player => player;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
    }
}
```

单例模式的标准写法:`Awake` 里检查如果已经有一个 `Instance` 存在(比如切场景时重复加载),就把自己销毁,避免出现两个 GameManager 打架。现在唯一的作用是存一下玩家引用,后面几周会陆续加内容。

### 2.3 `Assets/Scripts/Core/CameraFollow.cs`

```csharp
using UnityEngine;

namespace Game.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + (Vector3)offset;
            transform.position = new Vector3(desired.x, desired.y, transform.position.z);
        }
    }
}
```

只做一件事:每帧把摄像机的 X/Y 对齐到目标(玩家)的 X/Y,Z 保持摄像机自己原来的值(2D 项目里摄像机通常在 `z = -10`,不能被目标的 z 覆盖)。**特意没有跟着玩家一起转**——如果直接把摄像机挂到玩家身上当子物体,玩家转向鼠标时镜头会跟着疯狂转,体验很差。放在 `LateUpdate` 是 Unity 里"摄像机跟随"的标准做法,保证在所有物体的 `Update`/`FixedUpdate` 都跑完之后再定位摄像机,画面不会抖。

第 5 周会换成 Cinemachine 做平滑过渡,这个脚本是过渡期的占位方案。

### 2.4 `Assets/Scripts/Entities/Health.cs`

```csharp
using UnityEngine;

namespace Game.Entities
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead) return;

            Current = Mathf.Max(0, Current - amount);

            if (IsDead)
            {
                Die();
            }
        }

        private void Die()
        {
            gameObject.SetActive(false);
        }
    }
}
```

玩家和敌人共用同一个血量组件。`Die()` 现在只是简单地 `SetActive(false)`(先让它消失),后面周数接入 EventBus 广播死亡事件、加死亡特效/掉落物的时候会扩展这里,不用现在就想全。

### 2.5 `Assets/Scripts/Entities/PlayerController.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private Rigidbody2D rb;
        private Camera mainCamera;
        private Vector2 moveInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            ReadMoveInput();
            RotateTowardsMouse();
        }

        private void FixedUpdate()
        {
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }

        private void ReadMoveInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                moveInput = Vector2.zero;
                return;
            }

            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);

            moveInput = new Vector2(x, y).normalized;
        }

        private void RotateTowardsMouse()
        {
            if (Mouse.current == null || mainCamera == null) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));

            Vector2 direction = (Vector2)worldPos - rb.position;
            if (direction.sqrMagnitude < 0.0001f) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rb.rotation = angle;
        }
    }
}
```

几个关键点:

- **为什么直接轮询 `Keyboard.current`/`Mouse.current`,不用 Input Actions 资产?** 项目模板自带的 `InputSystem_Actions.inputactions` 是给 3D 模板用的通用配置(带 Jump、Sprint 这些用不上的动作),第一周直接改它反而要多学一套 Action Map / 生成 C# 类的流程。直接读设备状态一样是新版 Input System(满足"接入 Input System"这个目标),代码更直接,适合刚上手。等第 4 周做输入缓冲队列(命令模式)时,我们会把输入采集这一层重新封装,到时候要不要换成 Input Actions 资产可以再讨论。
- **为什么用 `rb.MovePosition` 而不是直接改 `transform.position`?** `MovePosition` 会经过物理引擎插值,和其他刚体(比如敌人撞过来)的物理交互才会正确;直接改 `transform.position` 会绕过物理系统,可能出现穿模。
- **朝向鼠标的角度计算**:`Mathf.Atan2(direction.y, direction.x)` 算出的角度,是以"物体局部 +X 轴(`transform.right`)朝向该方向"为基准的。也就是说,这套代码约定:**精灵素材默认朝向是朝右(局部 +X)**,而不是朝上。当前用 Unity 内置的方块/圆形占位图形没有明显的"脸朝哪"的问题,不用关心这件事;但**以后如果导入的角色美术默认是朝上的**,不要来改这段角度计算代码,正确的做法是在 Player 下面建一个子物体专门放 `SpriteRenderer`,把这个子物体的局部旋转设为 `-90°` 去抵消美术朝向,让"逻辑朝向"(`rb.rotation`,影响子弹方向)和"美术朝向"分开,互不干扰。

### 2.6 `Assets/Scripts/Entities/EnemyController.cs`

```csharp
using UnityEngine;

namespace Game.Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private int contactDamage = 10;
        [SerializeField] private float damageCooldown = 1f;

        private Rigidbody2D rb;
        private Transform player;
        private float damageTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        private void Update()
        {
            if (damageTimer > 0f)
            {
                damageTimer -= Time.deltaTime;
            }
        }

        private void FixedUpdate()
        {
            if (player == null) return;

            Vector2 direction = ((Vector2)player.position - rb.position).normalized;
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (damageTimer > 0f) return;
            if (!collision.collider.CompareTag("Player")) return;

            if (collision.collider.TryGetComponent(out Health health))
            {
                health.TakeDamage(contactDamage);
                damageTimer = damageCooldown;
            }
        }
    }
}
```

关键点:

- `Start()` 里用 `FindGameObjectWithTag("Player")` 找玩家。这是最简单的写法,够第一周用;以后玩家引用会统一从 `GameManager.Instance.Player` 拿,减少满场景 `Find`。
- 用的是 `OnCollisionStay2D` 而不是 `OnCollisionEnter2D`:敌人会持续贴着玩家,`Enter` 只在刚接触的一瞬间触发一次,后续贴身跟随不会再触发,伤害只会扣一次就没了。`Stay` 会在接触期间每个物理帧都调用,配合 `damageTimer` 做的"每秒扣一次血"冷却,才符合持续贴身战斗的手感。
- 这个回调要生效,**双方的 Collider2D 都不能勾 `Is Trigger`**(碰撞而不是触发),下面编辑器步骤里会specifically 强调。

### 2.7 `Assets/Scripts/Weapons/Bullet.cs`

```csharp
using Game.Core;
using Game.Entities;
using UnityEngine;

namespace Game.Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifeTime = 2f;
        [SerializeField] private int damage = 10;

        private Rigidbody2D rb;
        private float timer;

        public ObjectPool Pool { get; set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            timer = 0f;
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = transform.right * speed;

            timer += Time.fixedDeltaTime;
            if (timer >= lifeTime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Enemy")) return;

            if (other.TryGetComponent(out Health health))
            {
                health.TakeDamage(damage);
            }

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            rb.linearVelocity = Vector2.zero;

            if (Pool != null)
            {
                Pool.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
```

关键点:

- `transform.right * speed` 沿用了上面"局部 +X 是朝向"的约定,和玩家旋转、开火点的旋转是同一套逻辑,子弹飞出去的方向才会和瞄准方向一致。
- `OnEnable` 里重置计时器:因为对象池是"复用"物体,`Awake` 只会在第一次创建时跑一次,而每次从池子里被 `Get()` 出来重新启用时会触发 `OnEnable`,必须在这里而不是 `Awake` 里清零计时器,否则子弹活过一次 `lifeTime` 之后,以后每次复用都会立刻被判定超时收回。
- `ReturnToPool()` 里做了 `Pool != null` 的判断:正常情况下子弹都是从 `ObjectPool` 生成的,`Pool` 会被自动赋值;这个判断只是一个保险(比如你在场景里手动拖了一个 Bullet 测试),不影响正常流程。
- 子弹的 Collider2D **需要勾选 `Is Trigger`**,这样子弹可以穿过敌人的碰撞体触发 `OnTriggerEnter2D`,而不会被物理引擎当成实体挡住/弹开。

### 2.8 `Assets/Scripts/Weapons/PlayerShooter.cs`

```csharp
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Weapons
{
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private ObjectPool bulletPool;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireCooldown = 0.25f;

        private float cooldownTimer;

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            bool firePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (firePressed && cooldownTimer <= 0f)
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

这一版是"单发子弹射击"里说的雏形——先不考虑武器切换、弹药限制(那是第 3 周的 `IWeaponStrategy` 要做的事),现在只管两件事:按住左键、按冷却间隔连续开火。`bulletPool`/`firePoint` 都在 Inspector 里拖引用,下面编辑器步骤会讲怎么连线。

---

## 3. Unity 编辑器操作步骤(照着做)

### 步骤 1:创建 Tag

`Player` 是 Unity 内置 Tag,不用创建。需要手动加一个 `Enemy` Tag:

1. 菜单栏 `Edit > Project Settings > Tags and Layers`
2. 展开 `Tags` 列表,点击 `+`,输入 `Enemy`,回车

### 步骤 2:搭建 Player

1. 打开 `Assets/Scenes/SampleScene.unity`(直接双击场景文件)
2. 菜单栏 `GameObject > 2D Object > Sprites > Square`,生成一个方块精灵,重命名为 `Player`
3. 在 Hierarchy 里选中 `Player`,Inspector 右上角 `Tag` 下拉框选 `Player`
4. `Add Component` 加 `Rigidbody 2D`:
   - `Body Type` = `Dynamic`
   - `Gravity Scale` = `0`(俯视角游戏不需要重力)
   - `Collision Detection` = `Continuous`(防止移动速度快时穿过敌人)
5. `Add Component` 加 `Box Collider 2D`(默认大小基本贴合方块精灵即可,不用改)
6. `Add Component`,把 `PlayerController.cs`、`Health.cs`、`PlayerShooter.cs` 都加到 `Player` 上
7. 在 `Player` 下创建一个空子物体:右键 `Player > Create Empty`,重命名为 `FirePoint`,把它的 `Position` 设为 `(0.5, 0, 0)`(局部坐标,在方块右侧一点点,对应前面说的"局部 +X 是朝向"约定)
8. 回到 `Player` 的 `PlayerShooter` 组件,把 `Fire Point` 字段拖成刚才的 `FirePoint` 子物体(`Bullet Pool` 字段先留空,等步骤 4 建好池子后再回来连)

### 步骤 3:搭建 Bullet 预制体

1. `GameObject > 2D Object > Sprites > Circle`,重命名为 `Bullet`,可以把 `Transform > Scale` 调小一点(比如 `0.3, 0.3, 1`)
2. `Add Component` 加 `Rigidbody 2D`:`Body Type` = `Dynamic`,`Gravity Scale` = `0`,`Collision Detection` = `Continuous`
3. `Add Component` 加 `Circle Collider 2D`,勾选 **`Is Trigger`**
4. `Add Component` 加 `Bullet.cs`
5. 把 Hierarchy 里的 `Bullet` 拖到 `Assets/Prefabs/` 文件夹里,生成预制体
6. 生成预制体之后,Hierarchy 里会留一个场景实例——直接删掉这个实例(池子会动态生成,不需要场景里预先放一个)

### 步骤 4:搭建 BulletPool

1. `GameObject > Create Empty`,重命名为 `BulletPool`
2. `Add Component` 加 `ObjectPool.cs`
3. `Prefab` 字段拖入 `Assets/Prefabs/Bullet`,`Prewarm Count` 保持默认 `20` 即可
4. 回到 `Player` 的 `PlayerShooter` 组件,把 `Bullet Pool` 字段拖成 `BulletPool` 这个物体

### 步骤 5:搭建 Enemy 预制体

1. `GameObject > 2D Object > Sprites > Triangle`(和玩家用不同形状,方便肉眼区分),重命名为 `Enemy`
2. Inspector 里 `Tag` 选 `Enemy`
3. `Add Component` 加 `Rigidbody 2D`:`Body Type` = `Dynamic`,`Gravity Scale` = `0`
4. `Add Component` 加 `Polygon Collider 2D` 或 `Circle Collider 2D` 均可,**不要勾 `Is Trigger`**(要和玩家发生真实碰撞,`OnCollisionStay2D` 才会触发)
5. `Add Component` 把 `EnemyController.cs`、`Health.cs` 加上去
6. 建议拖到 `Assets/Prefabs/` 存成预制体(方便以后房间生成系统直接实例化),场景里保留 1~2 个实例用于测试

### 步骤 6:搭建 GameManager

1. `GameObject > Create Empty`,重命名为 `GameManager`
2. `Add Component` 加 `GameManager.cs`
3. `Player` 字段拖入场景里的 `Player` 物体

### 步骤 7:摄像机跟随

1. 选中 `Main Camera`
2. `Add Component` 加 `CameraFollow.cs`
3. `Target` 字段拖入 `Player`
4. `Offset` 保持 `(0, 0)` 即可(如果想让摄像机往玩家前方偏一点可以后面再调)

### 步骤 8:保存并测试

1. `Ctrl + S` 保存场景
2. 点击 Play
3. 检查清单:
   - `WASD`/方向键能让方块八方向移动
   - 移动鼠标,方块会转向鼠标所在方向
   - 按住鼠标左键,`FirePoint` 位置持续飞出子弹
   - 子弹打到三角形敌人,敌人多打几下会消失(默认血量 100,子弹伤害 10,需要命中 10 次)
   - 敌人会主动朝玩家移动,贴上玩家后每隔 1 秒玩家扣一次血(可以在 `Player` 的 `Health` 组件上右键 `Debug` 模式或者用下面的排查方法确认)

---

## 4. 常见问题排查

| 现象 | 可能原因 | 解决方法 |
|---|---|---|
| 按键没反应,鼠标转向也没反应 | Active Input Handling 没切到新版 | 回到本文档第 0 节检查设置 |
| 子弹一开火全部堆在原点不动 | `FirePoint` 没有设置 / `Bullet Pool` 没连线 | 检查 `PlayerShooter` 组件上的两个字段是否都已拖引用 |
| 子弹方向和瞄准方向对不上 | Bullet 预制体本身被手动加了旋转 | 确认 Bullet 预制体的 `Transform > Rotation` 是 `(0,0,0)`,方向完全由 `firePoint.rotation` 决定 |
| 子弹穿过敌人没反应 | 敌人 Tag 不是 `Enemy`,或者 Bullet 的 Collider 没勾 `Is Trigger` | 分别检查这两处 |
| 敌人贴着玩家但不掉血 | 玩家或敌人的 Collider 勾了 `Is Trigger`(需要都不勾) | `OnCollisionStay2D` 要求双方都是非 Trigger 碰撞体 |
| 敌人穿过玩家/子弹穿过敌人(高速下) | `Collision Detection` 没设成 `Continuous` | 检查对应 Rigidbody2D 组件的这个字段 |
| Console 报 `NullReferenceException` | 大概率是某个 Inspector 字段忘了拖引用 | 报错信息里会指出具体是哪个脚本第几行,把完整报错发我 |

---

## 5. 本周完成情况(实际发生了什么)

- [x] 环境检查(Active Input Handling)
- [x] Player 搭建完成,可以移动 + 转向
- [x] Bullet 预制体 + BulletPool 搭建完成,可以开火
- [x] Enemy 搭建完成,会追击 + 造成伤害
- [x] GameManager / CameraFollow 挂好
- [x] 整体跑通,没有报错

**过程中踩过的坑**(都是用户对照 `Reference/Scripts/` 手动敲代码时引入的笔误,不是参考实现本身的问题,记录下来方便以后排查同类问题):

- `PlayerController.cs`:`Mouse.current.position.ReadValue` 漏了调用括号 `()`;`rb.rotation` 和 `rb.position` 搞混,导致方向向量用错了类型。
- `EnemyController.cs`:`Awake()` 里写成 `rb.GetComponent<Rigidbody2D>()`(在还是 `null` 的 `rb` 上取组件),应为 `rb = GetComponent<Rigidbody2D>()`;`OnCollisionStay2D` 少打一个字母写成 `OCollisionStay2D`,导致 Unity 认不出这个是碰撞回调,不报错但静默失效。
- `Bullet.cs`:漏写 `IPoolable` 接口声明,导致对象池的 `TryGetComponent<IPoolable>` 找不到组件,`Pool` 恒为 `null`,子弹一直在被 `Destroy` 而不是回收复用(功能上不影响手感,但违反了对象池的架构约定);`CompareTag("enemy")` 大小写和实际 Tag `Enemy` 不一致,导致子弹一直判定"不是敌人",直接穿过不掉血。
- 顺手清理了几个转录时带出来的多余 `using`(`Unity.Collections`、`System.Dynamic`、`System.Net.NetworkInformation`、`UnityEditor.Callbacks`),其中 `UnityEditor.Callbacks` 如果不删,后续真正打包 Build 时会编译报错(`UnityEditor` 命名空间不存在于打包后的运行时)。
- `CameraFollow.cs` 里偏移量计算一度被改成 `target.position - offset`(应为 `+`),当前 `offset` 默认是 `(0,0)` 所以没造成可见影响,但已经改回来,避免以后设置非零偏移时方向反了。

**验收结果**:WASD 八方向移动、朝向鼠标旋转、按住左键连续开火、子弹命中敌人掉血/多次命中后消失、敌人追击玩家并每秒造成一次接触伤害,均已在 Play 模式下测试通过。第 1 周正式完成。

---

## 6. 下周预告:第 2 周 - 状态机与敌人基础 AI

会在这周的移动/开火基础上,把玩家和敌人的行为拆成状态机(Idle/Move/Dash/Attack/Hurt,Patrol/Chase/Attack/Dead),加入闪避(Dash)+ 无敌帧 + 受击闪白,以及一把近战武器原型。目录里会新增 `Assets/Scripts/StateMachines/`。
