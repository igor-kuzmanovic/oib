using Contracts.Interfaces;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;

namespace Server.Infrastructure
{
    public class BackupServerBehavior : IServerBehavior
    {
        private readonly string fileAddress;
        private readonly string syncAddress;
        private readonly string remoteSyncAddress;
        private readonly X509Certificate2 clientCertificate;
        private readonly X509Certificate2 remoteServerCertificate;
        private readonly Action promoteCallback;

        public BackupServerBehavior(
            string fileAddress,
            string syncAddress,
            string remoteSyncAddress,
            X509Certificate2 clientCertificate,
            X509Certificate2 remoteServerCertificate,
            Action promoteCallback
        )
        {
            this.fileAddress = fileAddress;
            this.syncAddress = syncAddress;
            this.remoteSyncAddress = remoteSyncAddress;
            this.clientCertificate = clientCertificate;
            this.remoteServerCertificate = remoteServerCertificate;
            this.promoteCallback = promoteCallback;
        }

        public void Start()
        {
            Console.WriteLine("Backup server started. Monitoring primary availability. Will promote if primary is unreachable.");
        }

        public bool IsRemotePrimaryAlive()
        {
            try
            {
                using (var proxy = new SyncServiceProxy(remoteSyncAddress, clientCertificate, remoteServerCertificate))
                {
                    return proxy.Ping();
                }
            }
            catch
            {
                return false;
            }
        }

        public void Stop()
        {

        }
    }
}
