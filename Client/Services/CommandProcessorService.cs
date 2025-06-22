using Client.Commands;
using System;
using System.ServiceModel;

namespace Client.Services
{
    public class CommandProcessorService
    {
        private readonly CommandRegistry commandRegistry;
        private readonly IFileServiceClient fileServiceClient;

        public CommandProcessorService(IFileServiceClient fileServiceClient)
        {
            this.fileServiceClient = fileServiceClient ?? throw new ArgumentNullException(nameof(fileServiceClient));
            this.commandRegistry = new CommandRegistry();
            RegisterCommands();
        }

        private void RegisterCommands()
        {

            commandRegistry.RegisterCommand("1", new ListFolderCommand(fileServiceClient));
            commandRegistry.RegisterCommand("2", new ReadFileCommand(fileServiceClient));
            commandRegistry.RegisterCommand("3", new CreateFolderCommand(fileServiceClient));
            commandRegistry.RegisterCommand("4", new CreateFileCommand(fileServiceClient));
            commandRegistry.RegisterCommand("5", new DeleteItemCommand(fileServiceClient));
            commandRegistry.RegisterCommand("6", new RenameItemCommand(fileServiceClient));
            commandRegistry.RegisterCommand("7", new MoveItemCommand(fileServiceClient));
            commandRegistry.RegisterCommand("8", new ServerStatusCommand(fileServiceClient));
            commandRegistry.RegisterCommand("0", new ExitCommand(fileServiceClient));
        }

        public void ProcessCommands()
        {
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("\nAvailable commands:");
                foreach (var command in commandRegistry.GetAllCommands())
                {
                    Console.WriteLine($"{command.Key}. {command.Value.Name}");
                }

                Console.Write("\nEnter command: ");
                string input = Console.ReadLine();

                try
                {
                    if (commandRegistry.TryGetCommand(input, out ICommand command))
                    {
                        if (input == "0")
                        {
                            exit = true;
                        }
                        else
                        {
                            command.Execute();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid command.");
                    }
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
    }
}
