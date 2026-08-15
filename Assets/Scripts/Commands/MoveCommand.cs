using Game.Entities;
using UnityEngine;

namespace Game.Commands
{
    /// <summary>把输入层计算出的移动方向写入玩家上下文。</summary>
    public class MoveCommand : ICommand
    {
        private readonly PlayerController player;
        private Vector2 direction;

        public MoveCommand(PlayerController player) { this.player = player; }
        public void SetDirection(Vector2 value) => direction = value;
        public bool CanExecute() => true;
        public void Execute() => player.SetMoveInput(direction);
    }
}