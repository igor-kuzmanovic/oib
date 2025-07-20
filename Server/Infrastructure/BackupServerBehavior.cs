using Contracts.Helpers;
using Contracts.Interfaces;
using Server.Audit;
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
        private readonly string remoteSyncAddress;
        private readonly X509Certificate2 clientCertificate;
        private readonly X509Certificate2 remoteServerCertificate;

        public BackupServerBehavior(
            string remoteSyncAddress,
            X509Certificate2 clientCertificate,
            X509Certificate2 remoteServerCertificate
        )
        {
            this.remoteSyncAddress = remoteSyncAddress;
            this.clientCertificate = clientCertificate;
            this.remoteServerCertificate = remoteServerCertificate;
        }

        public void Start()
        {
            Console.WriteLine("Backup server started. Monitoring primary availability. Will promote if primary is unreachable.");
            AuditFacade.ServerStarted(GetName());
        }

        public bool TrySyncWithPrimary()
        {
            try
            {
                using (var proxy = new SyncServiceProxy(remoteSyncAddress, clientCertificate, remoteServerCertificate))
                {
                    int lastEventId = StorageServiceProvider.Instance.GetLastEventId();
                    var events = proxy.GetEventsSinceId(lastEventId);

                    if (events.Length > 0)
                    {
                        foreach (var ev in events)
                        {
                            StorageServiceProvider.Instance.ApplyEvent(ev);
                            StorageServiceProvider.Instance.SetLastEventId(ev.Id);
                        }
                        Console.WriteLine($"[BackupServerBehavior] Synced {events.Length} events from primary.");
                        AuditFacade.ServerSynchronized(GetName(), SecurityHelper.GetName(remoteServerCertificate), events.Length);
                    }
                    else
                    {
                        Console.WriteLine("[BackupServerBehavior] No new events to sync.");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BackupServerBehavior] Sync with primary failed: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[BackupServerBehavior] Inner: {ex.InnerException.Message}");
                AuditFacade.ServerError($"Sync with {SecurityHelper.GetName(remoteServerCertificate)} failed: {ex.Message}");
                return false;
            }
        }

        public string GetName() { return SecurityHelper.GetName(clientCertificate); }

        public void Stop()
        {
            AuditFacade.ServerStopped(GetName());
        }
    }
}
