using Client.Services;

namespace Client.Commands
{
    public class CreateFolderCommand : CommandBase
    {
        public override string Name => "Create folder";
        public override string Description => "Creates a new folder at the specified path";

        public CreateFolderCommand(IFileServiceClient fileServiceClient) : base(fileServiceClient) { }

        public override void Execute()
        {
            string newFolderPath = GetUserInput("Enter folder path to create: ");
            bool folderCreated = FileServiceClient.CreateFolder(newFolderPath);
            DisplayResult("Folder creation", folderCreated);
        }
    }
}
