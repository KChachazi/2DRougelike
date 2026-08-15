namespace Game.Commands
{
    /// <summary>可先检查执行条件、再执行具体玩家能力的命令契约。</summary>
    public interface ICommand
    {
        bool CanExecute();
        void Execute();
    }
}