using Game.Entities;
using Game.Weapons;

namespace Game.Commands
{
    public class GrenadeCommand : ICommand
    {
        private readonly PlayerController player;
        private readonly GrenadeThrower thrower;
        public GrenadeCommand(PlayerController player, GrenadeThrower thrower) { this.player = player; this.thrower = thrower; }
        public bool CanExecute() => thrower != null && player.CanAct && thrower.CanThrow;
        public void Execute() => thrower.Throw();
    }
}