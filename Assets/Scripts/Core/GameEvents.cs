using UnityEngine;

namespace Game.Core
{
    // ======================== 玩家 ========================
    /// <summary>玩家当前血量发生变化，供血条等跨模块表现刷新。</summary>
    public readonly struct PlayerHealthChangedEvent
    {
        public readonly int Current;
        public readonly int Max;
        public PlayerHealthChangedEvent(int current, int max) { Current = current; Max = max; }
    }
    // ======================== 武器 ========================
    /// <summary>当前武器的剩余弹药发生变化。</summary>
    public readonly struct AmmoChangedEvent
    {
        public readonly int Current;
        /// <summary>最大弹药量；-1 表示无限弹药。</summary>
        public readonly int Max;
        public AmmoChangedEvent(int current, int max) { Current = current; Max = max; }
    }
    /// <summary>玩家切换了当前武器，携带用于 UI 显示的武器名称。</summary>
    public readonly struct WeaponChangedEvent
    {
        public readonly string WeaponName;
        public WeaponChangedEvent(string weaponName) { WeaponName = weaponName; }
    }
    // ======================== 技能 ========================
    /// <summary>跨模块识别技能的稳定标识。</summary>
    public enum SkillId
    {
        Dash,
        Grenade,
    }
    /// <summary>指定技能开始进入冷却，UI 可据此独立倒计时。</summary>
    public readonly struct SkillCooldownStartedEvent
    {
        public readonly SkillId Skill;
        public readonly float Cooldown;
        public SkillCooldownStartedEvent(SkillId skill, float cooldown) { Skill = skill; Cooldown = cooldown; }
    }
    // ======================== 关卡 ========================
    /// <summary>房间的玩法类型，用于配置、关卡流程和小地图显示。</summary>
    public enum RoomType
    {
        Normal,
        Boss,
    }
    /// <summary>关卡开始，并提供按流程顺序排列的房间类型快照。</summary>
    public readonly struct LevelStartedEvent
    {
        public readonly RoomType[] RoomTypes;
        public LevelStartedEvent(RoomType[] roomTypes) { RoomTypes = roomTypes; }
    }
    /// <summary>玩家进入指定序号的房间。</summary>
    public readonly struct RoomEnteredEvent
    {
        /// <summary>房间在当前关卡顺序中的零基索引。</summary>
        public readonly int Index;
        public RoomEnteredEvent(int index) { Index = index; }
    }
    /// <summary>指定序号的房间已清空全部敌人。</summary>
    public readonly struct RoomClearedEvent
    {
        /// <summary>房间在当前关卡顺序中的零基索引。</summary>
        public readonly int Index;
        public RoomClearedEvent(int index) { Index = index; }
    }
    /// <summary>玩家进入已解锁的门；具体前往哪里由关卡流程决定。</summary>
    public readonly struct DoorEnteredEvent { }
    /// <summary>当前关卡的全部房间流程已经完成。</summary>
    public readonly struct LevelCompletedEvent { }
    // ======================== 表现 ========================
    /// <summary>敌人受到一次伤害，供命中停顿和命中特效等表现系统使用。</summary>
    public readonly struct EnemyDamagedEvent
    {
        public readonly Vector2 Position;
        public readonly int Damage;
        public EnemyDamagedEvent(Vector2 position, int damage) { Position = position; Damage = damage; }
    }
    /// <summary>敌人死亡，携带死亡位置供击杀反馈使用。</summary>
    public readonly struct EnemyDiedEvent
    {
        public readonly Vector2 Position;
        public EnemyDiedEvent(Vector2 position) { Position = position; }
    }
    /// <summary>请求当前启用的相机按指定强度和时长执行震屏。</summary>
    public readonly struct ScreenShakeEvent
    {
        public readonly float Intensity;
        public readonly float Duration;
        public ScreenShakeEvent(float intensity, float duration) { Intensity = intensity; Duration = duration; }
    }
    // ======================== V1.5：状态异常反馈 ========================
    /// <summary>
    /// 实体被施加状态异常时广播。
    /// 例如：敌人头上冒出火焰图标、玩家屏幕边缘结冰等。
    /// 目前 StatusEffectManager 并未主动 Publish 此事件。
    /// </summary>
    public readonly struct StatusAppliedEvent
    {
        /// <summary>受影响的实体</summary>
        public readonly GameObject Target;
        /// <summary>被施加的状态类型</summary>
        public readonly StatusType Type;
        /// <summary>效果持续秒数</summary>
        public readonly float Duration;
        public StatusAppliedEvent(GameObject target, StatusType type, float duration)
        {
            Target = target;
            Type = type;
            Duration = duration;
        }
    }
    /// <summary>
    /// 状态异常过期或被移除时广播。
    /// 目前尚未由 StatusEffectManager 发布，属于后续表现系统的预留协议。
    /// </summary>
    public readonly struct StatusExpiredEvent
    {
        public readonly GameObject Target;
        public readonly StatusType Type;
        public StatusExpiredEvent(GameObject target, StatusType type)
        {
            Target = target;
            Type = type;
        }
    }
}