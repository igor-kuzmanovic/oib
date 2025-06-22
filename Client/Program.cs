using Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetTcpBinding binding = ServerConfig.GetBinding();

            string primaryAddress = ServerConfig.PrimaryServerAddress;
            string backupAddress = ServerConfig.BackupServerAddress;

            Console.WriteLine("FileServer Client");
            Console.WriteLine("======================================");
            Console.WriteLine($"Running as: {System.Security.Principal.WindowsIdentity.GetCurrent().Name}");

            try
            {
                using (WCFClient proxy = new WCFClient(binding,
                                                      new EndpointAddress(new Uri(primaryAddress)),
                                                      new EndpointAddress(new Uri(backupAddress))))
                {
                    CommandProcessor commandProcessor = new CommandProcessor(proxy);
                    commandProcessor.ProcessCommands();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error: {ex.Message}");
            }

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }
    }
}
