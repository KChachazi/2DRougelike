namespace Game.Commands
{
    public interface ICommand
    {
        bool CanExecute();
        void Execute();
    }
}