using System;
using System.Security.Principal;
using Client.Services;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("FileServer Client");
            Console.WriteLine("======================================");
            Console.WriteLine($"Running as: {WindowsIdentity.GetCurrent().Name}");

            using (var fileServiceClient = new FileServiceClient())
            {
                var commandProcessorService = new CommandProcessorService(fileServiceClient);

                commandProcessorService.ProcessCommands();
            }

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }
    }
}
