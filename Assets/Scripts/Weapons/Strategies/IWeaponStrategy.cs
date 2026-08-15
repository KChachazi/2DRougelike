using Game.Core;

namespace Game.Weapons
{
    /// <summary>
    /// 无状态的武器开火策略；弹药与冷却由 WeaponController 管理，
    /// 策略只负责执行具体攻击方式。
    /// </summary>
    public interface IWeaponStrategy
    {
        void Fire(WeaponController controller, DamageInfo damageInfo);
    }
}