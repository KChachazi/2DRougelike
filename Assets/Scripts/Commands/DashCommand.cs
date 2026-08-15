using Game.Entities;

namespace Game.Commands
{
    /// <summary>检查玩家行动状态和闪避冷却，并触发闪避状态。</summary>
    public class DashCommand : ICommand
    {
        private readonly PlayerController player;
        public DashCommand(PlayerController player) { this.player = player; }
        public bool CanExecute() => player.CanAct && player.CanDash;
        public void Execute() => player.TriggerDash();
    }
}