using Contracts.Helpers;
using Contracts.Interfaces;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;

namespace Server.Infrastructure
{
    public class FileServer : IDisposable
    {
        private ServerRole role;
        private IServerBehavior behavior;
        private readonly string fileAddress;
        private readonly string syncAddress;
        private readonly string remoteSyncAddress;
        private readonly X509Certificate2 serverCertificate;
        private readonly X509Certificate2 clientCertificate;
        private readonly X509Certificate2 remoteServerCertificate;

        public FileServer()
        {
            serverCertificate = SecurityHelper.GetCertificate(Configuration.ServerSyncCertificateCN);
            if (serverCertificate == null)
                throw new ApplicationException($"Local server certificate '{Configuration.ServerSyncCertificateCN}' not found or invalid.");

            clientCertificate = SecurityHelper.GetCertificate(Configuration.ClientSyncCertificateCN);
            if (clientCertificate == null)
                throw new ApplicationException($"Client certificate '{Configuration.ClientSyncCertificateCN}' not found or invalid.");

            remoteServerCertificate = SecurityHelper.GetCertificate(Configuration.RemoteServerSyncCertificateCN);
            if (remoteServerCertificate == null)
                throw new ApplicationException($"Remote certificate '{Configuration.RemoteServerSyncCertificateCN}' not found or invalid.");

            bool primaryUp = !IsPortAvailable(new Uri(Configuration.PrimaryServerAddress).Port);
            bool backupUp = !IsPortAvailable(new Uri(Configuration.BackupServerAddress).Port);

            if (primaryUp && backupUp)
                throw new ApplicationException("Both sync services are running. Cannot start another server.");

            if (primaryUp)
            {
                Console.WriteLine("Server on primary address is running. Configuring as backup with backup address.");
                role = ServerRole.Backup;
                fileAddress = Configuration.BackupServerAddress;
                syncAddress = Configuration.ServerSyncAddress;
                remoteSyncAddress = Configuration.ServerSyncAddress;
                StorageServiceProvider.Initialize(Configuration.BackupDataDirectory);
            }
            else if (backupUp)
            {
                Console.WriteLine("Server on backup address is running. Configuring as backup with primary address.");
                role = ServerRole.Backup;
                fileAddress = Configuration.PrimaryServerAddress;
                syncAddress = Configuration.ServerSyncAddress;
                remoteSyncAddress = Configuration.ServerSyncAddress;
                StorageServiceProvider.Initialize(Configuration.PrimaryDataDirectory);
            }
            else
            {
                Console.WriteLine("No servers running. Configuring as primary with primary address.");
                role = ServerRole.Primary;
                fileAddress = Configuration.PrimaryServerAddress;
                syncAddress = Configuration.ServerSyncAddress;
                remoteSyncAddress = Configuration.ServerSyncAddress;
                StorageServiceProvider.Initialize(Configuration.PrimaryDataDirectory);
            }
        }

        public void Start()
        {
            if (role == ServerRole.Primary)
            {
                behavior = new PrimaryServerBehavior(fileAddress, syncAddress, serverCertificate);
            }
            else
            {
                behavior = new BackupServerBehavior(remoteSyncAddress, clientCertificate, remoteServerCertificate);
            }
            behavior.Start();
            Console.WriteLine($"Server started as {role}");
        }

        public void SyncAndPromoteToPrimaryIfDown()
        {
            if (role == ServerRole.Backup && behavior is BackupServerBehavior backup)
            {
                if (!backup.TrySyncWithPrimary())
                {
                    behavior.Stop();
                    PromoteToPrimary();
                }
            }
        }

        private void PromoteToPrimary()
        {
            role = ServerRole.Primary;
            behavior = new PrimaryServerBehavior(fileAddress, syncAddress, serverCertificate);
            behavior.Start();
            Console.WriteLine("Server promoted to PRIMARY role.");
        }

        private static bool IsPortAvailable(int port)
        {
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnections = ipProperties.GetActiveTcpListeners();

            foreach (var endpoint in tcpConnections)
            {
                if (endpoint.Port == port)
                {
                    Console.WriteLine($"Port {port} is already in use by another process.");
                    return false;
                }
            }
            return true;
        }

        public void Stop()
        {
            behavior?.Stop();
            Console.WriteLine("Server stopped.");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
