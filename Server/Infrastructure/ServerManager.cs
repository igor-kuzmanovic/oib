using Server.Infrastructure;
using System;

namespace Server.Infrastructure
{
    public enum ServerRole { Primary, Backup }

    public class ServerManager : IServerManager, IDisposable
    {
        private ServerInstance serverInstance;

        public ServerManager() { }

        private ServerRole serverRole;
        private string fileAddress;
        private string syncAddress;
        private string remoteSyncAddress;

        public void StartServer()
        {
            serverRole = ServerRoleDetector.IsPrimaryPortAvailable(Configuration.PrimaryServerAddress);
            if (serverRole == ServerRole.Primary)
            {
                fileAddress = Configuration.PrimaryServerAddress;
                syncAddress = Configuration.PrimaryServerSyncAddress;
                remoteSyncAddress = Configuration.BackupServerSyncAddress;
            }
            else
            {
                fileAddress = Configuration.BackupServerAddress;
                syncAddress = Configuration.BackupServerSyncAddress;
                remoteSyncAddress = Configuration.PrimaryServerSyncAddress;
            }
            serverInstance = new ServerInstance(serverRole, fileAddress, syncAddress, remoteSyncAddress);
            serverInstance.Start();

            Console.WriteLine($"Initial server role: {serverRole.ToString().ToLower()}");
            if (serverInstance.IsFileHostRunning())
            {
                Console.WriteLine($"File server running on: {fileAddress}");
            } else
            {
                Console.WriteLine($"File server not running");
            }
            if (serverInstance.IsSyncHostRunning())
            {
                Console.WriteLine($"Sync server running on: {syncAddress}");
            }
            else
            {
                Console.WriteLine($"Sync server not running");
            }
        }

        public void ShutdownServer()
        {
            serverInstance?.Stop();
        }

        public void Dispose()
        {
            ShutdownServer();
        }
    }
}

