using Client.Services;

namespace Client.Commands
{
    public class DeleteItemCommand : CommandBase
    {
        public override string Name => "Delete item";
        public override string Description => "Deletes a file or folder at the specified path";

        public DeleteItemCommand(IFileServiceClient fileServiceClient) : base(fileServiceClient) { }

        public override void Execute()
        {
            string deletePath = GetUserInput("Enter path to delete: ");
            bool deleted = FileServiceClient.Delete(deletePath);
            DisplayResult("Deletion", deleted);
        }
    }
}
