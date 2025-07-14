using Client.Services;
using Contracts.Models;
using Contracts.Encryption;
using System;
using System.Text;

namespace Client.Commands
{
    public class CreateFileCommand : CommandBase
    {
        public override string Name => "Create file";
        public override string Description => "Creates a new text file with specified content";

        public CreateFileCommand(IFileServiceClient fileServiceClient) : base(fileServiceClient) { }

        public override void Execute()
        {
            string newFilePath = GetUserInput("Enter file path to create (must be .txt): ");

            if (!newFilePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                newFilePath += ".txt";
                Console.WriteLine($"Added .txt extension. File will be created as: {newFilePath}");
            }

            string content = GetUserInput("Enter text content: ");
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);

            FileData tempFileData = new FileData
            {
                Content = contentBytes,
                CreatedBy = Environment.UserName,
                CreatedAt = DateTime.UtcNow,
                IsFile = true
            };
            FileData newFileData = EncryptionHelper.EncryptContent(tempFileData);

            bool fileCreated = FileServiceClient.CreateFile(newFilePath, newFileData);
            DisplayResult("File creation", fileCreated);
        }
    }
}
