using Game.Weapons;

namespace Game.Commands
{
    public class AttackCommand : ICommand
    {
        private readonly WeaponController weapon;
        public AttackCommand(WeaponController weapon) { this.weapon = weapon; }

        public bool CanExecute() => weapon != null && weapon.CanFire();
        public void Execute() => weapon.Fire();
    }
}