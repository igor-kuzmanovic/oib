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

            FileData fileData = FileServiceClient.ReadFile(filePath);
            if (fileData != null && fileData.Content != null && fileData.InitializationVector != null)
            {

                byte[] decryptedContent = EncryptionHelper.DecryptContent(fileData);
                Console.WriteLine($"\nFile '{filePath}' content:");
                string textContent = Encoding.UTF8.GetString(decryptedContent);
                Console.WriteLine(textContent);
            }
            else
            {
                Console.WriteLine($"\nFailed to read file '{filePath}'.");
            }
        }
    }
}
