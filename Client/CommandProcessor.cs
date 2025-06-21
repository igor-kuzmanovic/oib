using Contracts;
using System;
using System.ServiceModel;
using System.Text;

namespace Client
{
    public class CommandProcessor
    {
        private readonly WCFClient proxy;

        public CommandProcessor(WCFClient proxy)
        {
            this.proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }

        public void ProcessCommands()
        {
            bool exit = false;

            while (!exit)
            {
                DisplayMenu();

                Console.Write("\nEnter command: ");
                string input = Console.ReadLine();

                try
                {
                    exit = ProcessCommand(input);
                }
                catch (FaultException ex)
                {
                    Console.WriteLine($"Service error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void DisplayMenu()
        {
            Console.WriteLine("\nAvailable commands:");
            Console.WriteLine("1. List folder content");
            Console.WriteLine("2. Read file");
            Console.WriteLine("3. Create folder");
            Console.WriteLine("4. Create file");
            Console.WriteLine("5. Delete item");
            Console.WriteLine("6. Rename item");
            Console.WriteLine("7. Move item");
            Console.WriteLine("8. Check server status");
            Console.WriteLine("0. Exit");
        }

        private bool ProcessCommand(string input)
        {
            switch (input)
            {
                case "1":
                    ListFolderContent();
                    break;

                case "2":
                    ReadFile();
                    break;

                case "3":
                    CreateFolder();
                    break;

                case "4":
                    CreateFile();
                    break;

                case "5":
                    DeleteItem();
                    break;

                case "6":
                    RenameItem();
                    break;

                case "7":
                    MoveItem();
                    break;

                case "8":
                    CheckServerStatus();
                    break;

                case "0":
                    return true;

                default:
                    Console.WriteLine("Invalid command.");
                    break;
            }

            return false;
        }

        private void ListFolderContent()
        {
            Console.Write("Enter folder path: ");
            string folderPath = Console.ReadLine();
            string[] items = proxy.ShowFolderContent(folderPath);
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
        private void ReadFile()
        {
            Console.Write("Enter file path to read (.txt file): ");
            string filePath = Console.ReadLine();

            if (!filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                filePath += ".txt";
                Console.WriteLine($"Added .txt extension. Reading file: {filePath}");
            }

            FileData fileData = proxy.ReadFile(filePath);
            if (fileData != null && fileData.Content != null)
            {
                Console.WriteLine($"\nFile '{filePath}' content:");
                Console.WriteLine("-------------------------------------------");
                string textContent = Encoding.UTF8.GetString(fileData.Content);
                Console.WriteLine(textContent);
                Console.WriteLine("-------------------------------------------");
            }
            else
            {
                Console.WriteLine($"\nFailed to read file '{filePath}'.");
            }
        }

        private void CreateFolder()
        {
            Console.Write("Enter folder path to create: ");
            string newFolderPath = Console.ReadLine();
            bool folderCreated = proxy.CreateFolder(newFolderPath);
            Console.WriteLine($"\nFolder creation {(folderCreated ? "successful" : "failed")}.");
        }
        private void CreateFile()
        {
            Console.Write("Enter file path to create (must be .txt): ");
            string newFilePath = Console.ReadLine();

            if (!newFilePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                newFilePath += ".txt";
                Console.WriteLine($"Added .txt extension. File will be created as: {newFilePath}");
            }

            Console.Write("Enter text content: ");
            string content = Console.ReadLine(); byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            FileData newFileData = new FileData();
            newFileData.Content = contentBytes;

            bool fileCreated = proxy.CreateFile(newFilePath, newFileData);
            Console.WriteLine($"\nFile creation {(fileCreated ? "successful" : "failed")}.");
        }

        private void DeleteItem()
        {
            Console.Write("Enter path to delete: ");
            string deletePath = Console.ReadLine();
            bool deleted = proxy.Delete(deletePath);
            Console.WriteLine($"\nDeletion {(deleted ? "successful" : "failed")}.");
        }

        private void RenameItem()
        {
            Console.Write("Enter source path: ");
            string renameSrc = Console.ReadLine();
            Console.Write("Enter new name: ");
            string renameDst = Console.ReadLine();
            bool renamed = proxy.Rename(renameSrc, renameDst);
            Console.WriteLine($"\nRename {(renamed ? "successful" : "failed")}.");
        }

        private void MoveItem()
        {
            Console.Write("Enter source path: ");
            string moveSrc = Console.ReadLine();
            Console.Write("Enter destination path: ");
            string moveDst = Console.ReadLine();
            bool moved = proxy.MoveTo(moveSrc, moveDst);
            Console.WriteLine($"\nMove {(moved ? "successful" : "failed")}.");
        }

        private void CheckServerStatus()
        {
            string serverType = proxy.IsConnectedToPrimary ? "PRIMARY" : "BACKUP";
            string serverAddress = proxy.GetCurrentServerAddress();
            Console.WriteLine($"\nCurrently connected to: {serverType} server at {serverAddress}");
        }
    }
}
