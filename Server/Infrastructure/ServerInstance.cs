using Contracts.Helpers;
using Contracts.Interfaces;
using Server.Audit;
using Server.Authorization;
using Server.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Policy;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Threading;

namespace Server.Infrastructure
{
    public class ServerInstance
    {
        private readonly string fileAddress;
        private readonly string syncAddress;
        private readonly string remoteSyncAddress;
        private readonly X509Certificate2 serverCertificate;
        private ServiceHost fileHost;
        private ServiceHost syncHost;
        private Timer syncCheckTimer;
        private ServerRole serverRole;

        public ServerInstance(ServerRole serverRole, string fileAddress, string syncAddress, string remoteSyncAddress)
        {
            this.serverRole = serverRole;
            this.fileAddress = fileAddress;
            this.syncAddress = syncAddress;
            this.remoteSyncAddress = remoteSyncAddress;
            if (this.syncAddress == this.remoteSyncAddress)
            {
                throw new ArgumentException("Sync address and remote sync address can't be the same");
            }

            serverCertificate = SecurityHelper.GetCurrentUserCertificate();
        }

        public void Start()
        {
            if (serverRole == ServerRole.Primary)
            {
                BecomePrimary();
            }
            else
            {
                BecomeBackup(GetRemoteServerCN(remoteSyncAddress));
            }
        }

        private string GetRemoteServerCN(string address)
        {
            if (address.IndexOf("Primary", StringComparison.OrdinalIgnoreCase) >= 0)
                return "FileServerPrimary";
            if (address.IndexOf("Backup", StringComparison.OrdinalIgnoreCase) >= 0)
                return "FileServerBackup";
            return "FileServerPrimary";
        }

        private void BecomePrimary()
        {
            serverRole = ServerRole.Primary;
            StartPrimaryHosts();
            AuditFacade.ServerStarted(fileAddress);
            Console.WriteLine("Promoted to primary. Service hosts started.");
        }

        private void BecomeBackup(string remoteServerCN)
        {
            serverRole = ServerRole.Backup;
            syncCheckTimer = new Timer(_ => CheckSyncConnection(remoteServerCN), null, 5000, 5000);
        }

        private void StartPrimaryHosts()
        {
            var fileBinding = new NetTcpBinding();
            fileBinding.Security.Mode = SecurityMode.Transport;
            fileBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            fileBinding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;

            fileHost = new ServiceHost(typeof(FileWCFService));
            fileHost.AddServiceEndpoint(typeof(IFileWCFService), fileBinding, fileAddress);

            fileHost.Authorization.ServiceAuthorizationManager = new AuthorizationManager();
            fileHost.Authorization.PrincipalPermissionMode = PrincipalPermissionMode.Custom;

            List<IAuthorizationPolicy> policies = new List<IAuthorizationPolicy>();
            policies.Add(new AuthorizationPolicy());
            fileHost.Authorization.ExternalAuthorizationPolicies = policies.AsReadOnly();

            fileHost.Open();

            var syncBinding = new NetTcpBinding();
            syncBinding.Security.Mode = SecurityMode.Transport;
            syncBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
            syncBinding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;

            syncHost = new ServiceHost(typeof(SyncWCFService));
            syncHost.AddServiceEndpoint(typeof(ISyncWCFService), syncBinding, syncAddress);
            var creds = new ServiceCredentials();
            creds.ServiceCertificate.Certificate = serverCertificate;
            creds.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.ChainTrust;
            creds.ClientCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
            syncHost.Description.Behaviors.Add(creds);

            try
            {
                syncHost.Open();
            }
            catch (Exception)
            {
                Console.WriteLine("Sync host not started, check user and certificate");
                syncHost = null;
            }
        }

        private void CheckSyncConnection(string remoteServerCN)
        {
            if (serverRole == ServerRole.Primary)
                return;
            try
            {
                using (var proxy = new SyncServiceProxy(remoteSyncAddress, serverCertificate, remoteServerCN))
                {
                    proxy.Ping();
                }
            }
            catch
            {
                Console.WriteLine("Lost connection to remote server. Promoting to primary.");
                syncCheckTimer?.Dispose();
                BecomePrimary();
            }
        }

        public bool IsFileHostRunning()
        {
            return this.fileHost != null && this.fileHost.State == CommunicationState.Opened;
        }

        public bool IsSyncHostRunning()
        {
            return this.syncHost != null && this.syncHost.State == CommunicationState.Opened;
        }

        public void Stop()
        {
            syncCheckTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            syncCheckTimer?.Dispose();
            try { fileHost?.Close(); } catch { fileHost?.Abort(); }
            try { syncHost?.Close(); } catch { syncHost?.Abort(); }
        }
    }
}
