using Client.Services;

namespace Client.Commands
{
    public class MoveItemCommand : CommandBase
    {
        public override string Name => "Move item";
        public override string Description => "Moves a file or folder to a new location";

        public MoveItemCommand(IFileServiceClient fileServiceClient) : base(fileServiceClient) { }

        public override void Execute()
        {
            string moveSrc = GetUserInput("Enter source path: ");
            string moveDst = GetUserInput("Enter destination path: ");
            bool moved = FileServiceClient.MoveTo(moveSrc, moveDst);
            DisplayResult("Move", moved);
        }
    }
}
