using System;
using System.Security.Principal;
using System.Threading;
using Server.Infrastructure;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("FileServer Server");
            Console.WriteLine("======================================");
            Console.WriteLine($"Running as: {WindowsIdentity.GetCurrent().Name}");

            FileServer fileServer = null;

            try
            {
                fileServer = new FileServer();
                fileServer.Start();

                Console.WriteLine("Press Enter to stop the server...");
                while (!Console.KeyAvailable)
                {
                    fileServer.SyncAndPromoteToPrimaryIfDown();
                    Thread.Sleep(5000); // Poll every 5 seconds
                }
                Console.ReadLine();
            }
            catch (ApplicationException ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"DETAILS: {ex.InnerException.Message}");
            }
            finally
            {
                fileServer?.Dispose();
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
        }
    }
}
