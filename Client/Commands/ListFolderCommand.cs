using Client.Services;
using System;

namespace Client.Commands
{
    public class ListFolderCommand : CommandBase
    {
        public override string Name => "List folder content";
        public override string Description => "Lists all files and folders in a specified directory";

        public ListFolderCommand(IFileServiceClient fileServiceClient) : base(fileServiceClient) { }

        public override void Execute()
        {
            string folderPath = GetUserInput("Enter folder path: ");
            string[] items = FileServiceClient.ShowFolderContent(folderPath);

            if (items != null && items.Length > 0)
            {
                Console.WriteLine($"\nContent of '{folderPath}':");
                foreach (var item in items)
                {
                    Console.WriteLine($"  {item}");
                }
            }
            else
            {
                Console.WriteLine($"\nFolder '{folderPath}' is empty or doesn't exist.");
            }
        }
    }
}
