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
            serverRole = 
                ServerRoleHelper.IsPortAvailable(Configuration.PrimaryServerAddress) 
                || ServerRoleHelper.IsPortAvailable(Configuration.BackupServerAddress) 
                ? ServerRole.Primary
                : ServerRole.Backup;
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

