using Server.Infrastructure;
using System;
using System.Security.Principal;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("FileServer Server");
            Console.WriteLine("======================================");
            Console.WriteLine($"Running as: {WindowsIdentity.GetCurrent().Name}");

            using (var serverManager = new ServerManager())
            {
                serverManager.StartServer();
                Console.WriteLine("Press Enter to stop the server...");
                Console.ReadLine();
                serverManager.ShutdownServer();
            }
        }
    }
}
