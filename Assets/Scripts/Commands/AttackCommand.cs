using Game.Weapons;

namespace Game.Commands
{
    /// <summary>检查武器是否可开火，并调用武器控制器执行攻击。</summary>
    public class AttackCommand : ICommand
    {
        private readonly WeaponController weapon;
        public AttackCommand(WeaponController weapon) { this.weapon = weapon; }

        public bool CanExecute() => weapon != null && weapon.CanFire();
        public void Execute() => weapon.Fire();
    }
}