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

            try
            {
                using (var fileServer = new Server.Infrastructure.FileServer())
                {
                    fileServer.Start();
                    Console.WriteLine("Press Enter to stop the server...");
                    Console.ReadLine();
                    fileServer.Stop();
                }
            }
            catch (ApplicationException ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"DETAILS: {ex.InnerException.Message}");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
        }
    }
}
