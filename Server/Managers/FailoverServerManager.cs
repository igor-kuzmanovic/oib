using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using Contracts.Interfaces;
using Server.Audit;
using Server.Infrastructure;
using Server.Services;

namespace Server.Managers
{
    public class FailoverServerManager : IServerManager, IDisposable
    {
        private IServerHost serverHost;

        public void StartServer()
        {
            try
            {
                serverHost = new PrimaryServerHost();
                serverHost.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start as primary server: {ex.Message}");
                throw;
            }
        }

        public void ShutdownServer()
        {
            serverHost?.Stop();
            Console.WriteLine("Server shutdown complete");
        }

        public string GetServerAddress()
        {
            return serverHost?.Address ?? Configuration.ServerAddress;
        }

        public void Dispose()
        {
            ShutdownServer();
        }
    }
}

