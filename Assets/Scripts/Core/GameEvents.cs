using UnityEngine;

namespace Game.Core
{
    public readonly struct PlayerHealthChangedEvent
    {
        public readonly int Current;
        public readonly int Max;
        public PlayerHealthChangedEvent(int current, int max) { Current = current; Max = max; }
    }

    /* ------------- 武器 ------------- */
    public readonly struct AmmoChangedEvent
    {
        public readonly int Current;
        public readonly int Max; // Max 为 -1 时表示无限子弹，比如近战
        public AmmoChangedEvent(int current, int max) { Current = current; Max = max; }
    }
    public readonly struct WeaponChangedEvent
    {
        public readonly string WeaponName;
        public WeaponChangedEvent(string weaponName) { WeaponName = weaponName; }
    }

    /* ------------- 技能 ------------- */
    public enum SkillId
    {
        Dash,
        Grenade,
    }
    public readonly struct SkillCooldownStartedEvent
    {
        public readonly SkillId Skill;
        public readonly float Cooldown;
        public SkillCooldownStartedEvent(SkillId skill, float cooldown) { Skill = skill; Cooldown = cooldown; }
    }

    /* ------------- 关卡 ------------- */
    public enum RoomType
    {
        Normal,
        Boss,
    }
    public readonly struct LevelStartedEvent
    {
        public readonly RoomType[] RoomTypes;
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
    public readonly struct DoorEnteredEvent { }
    public readonly struct LevelCompletedEvent { }

    /* ------------- 表现 ------------- */
    public readonly struct EnemyDamagedEvent
    {
        public readonly Vector2 Position;
        public readonly int Damage;
        public EnemyDamagedEvent(Vector2 position, int damage) { Position = position; Damage = damage; }
    }
    public readonly struct EnemyDiedEvent
    {
        public readonly Vector2 Position;
        public EnemyDiedEvent(Vector2 position) { Position = position; }
    }
    public readonly struct ScreenShakeEvent
    {
        public readonly float Intensity;
        public readonly float Duration;
        public ScreenShakeEvent(float intensity, float duration) { Intensity = intensity; Duration = duration; }
    }
}