using Game.Entities;

namespace Game.Commands
{
    public class DashCommand : ICommand
    {
        private readonly PlayerController player;
        public DashCommand(PlayerController player) { this.player = player; }
        public bool CanExecute() => player.CanAct && player.CanDash;
        public void Execute() => player.TriggerDash();
    }
}