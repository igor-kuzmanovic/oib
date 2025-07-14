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
            var items = FileServiceClient.ShowFolderContent(folderPath);

            if (items != null && items.Length > 0)
            {
                Console.WriteLine($"\nContent of '{folderPath}':");
                foreach (var item in items)
                {
                    string type = item.IsFile ? "File" : "Folder";
                    string name = item.Path != null ? System.IO.Path.GetFileName(item.Path) : "(unknown name)";
                    string createdBy = item.CreatedBy ?? "unknown";
                    string createdAt = item.CreatedAt != default ? item.CreatedAt.ToString("u") : "unknown";
                    Console.WriteLine($"  [{type}] {name} | Created by: {createdBy}, Created at: {createdAt}");
                }
            }
            else
            {
                Console.WriteLine($"\nFolder '{folderPath}' is empty or doesn't exist.");
            }
        }
    }
}
