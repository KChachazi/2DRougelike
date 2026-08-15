using Game.Weapons;

namespace Game.Commands
{
    /// <summary>切换到指定武器槽位；索引在执行前校验。</summary>
    public class SwitchWeaponCommand : ICommand
    {
        private readonly WeaponController weapon;
        private int index;
        public SwitchWeaponCommand(WeaponController weapon) { this.weapon = weapon; }
        public SwitchWeaponCommand SwitchIndex(int index)
        {
            this.index = index;
            return this;
        }
        public bool CanExecute() => weapon != null && index >= 0 && index < weapon.WeaponCount;
        public void Execute() => weapon.SwitchTo(index);
    }
}