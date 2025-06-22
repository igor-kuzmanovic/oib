using Client.Services;

namespace Client.Commands
{
    public class RenameItemCommand : CommandBase
    {
        public override string Name => "Rename item";
        public override string Description => "Renames a file or folder";

        public RenameItemCommand(IFileServiceClient fileServiceClient) : base(fileServiceClient) { }

        public override void Execute()
        {
            string renameSrc = GetUserInput("Enter source path: ");
            string renameDst = GetUserInput("Enter new name: ");
            bool renamed = FileServiceClient.Rename(renameSrc, renameDst);
            DisplayResult("Rename", renamed);
        }
    }
}
