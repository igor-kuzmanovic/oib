using System.Text;

namespace Client.Commands
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        void Execute();
    }
}
