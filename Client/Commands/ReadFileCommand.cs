using Client.Services;
using Contracts.Models;
using Contracts.Encryption;
using System;
using System.Text;

namespace Client.Commands
{
    public class ReadFileCommand : CommandBase
    {
        public override string Name => "Read file";
        public override string Description => "Reads and displays the content of a text file";

        public ReadFileCommand(IFileServiceClient fileServiceClient) : base(fileServiceClient) { }

        public override void Execute()
        {
            string filePath = GetUserInput("Enter file path to read (.txt file): ");

            if (!filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                filePath += ".txt";
                Console.WriteLine($"Added .txt extension. Reading file: {filePath}");
            }

            FileData encryptedFileData = FileServiceClient.ReadFile(filePath);
            if (encryptedFileData != null && encryptedFileData.Content != null && encryptedFileData.InitializationVector != null)
            {
                FileData decryptedFileData = EncryptionHelper.DecryptContent(encryptedFileData);
                Console.WriteLine($"\nFile '{filePath}' content:");
                string textContent = Encoding.UTF8.GetString(decryptedFileData.Content);
                Console.WriteLine(textContent);
                string createdBy = decryptedFileData.CreatedBy ?? "unknown";
                string createdAt = decryptedFileData.CreatedAt != default ? decryptedFileData.CreatedAt.ToString("u") : "unknown";
                Console.WriteLine($"Created by: {createdBy}, Created at: {createdAt}");
            }
            else
            {
                Console.WriteLine($"\nFailed to read file '{filePath}'.");
            }
        }
    }
}
