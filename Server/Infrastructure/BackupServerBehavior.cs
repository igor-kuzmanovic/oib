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
        private ServiceHost fileHost;
        private ServiceHost syncHost;

        public BackupServerBehavior(
            string fileAddress,
            string syncAddress,
            string remoteSyncAddress,
            X509Certificate2 clientCertificate,
            X509Certificate2 remoteServerCertificate,
            Action promoteCallback)
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

        private void StartFileHost()
        {
            var fileBinding = new NetTcpBinding
            {
                Security = {
                    Mode = SecurityMode.Transport,
                    Transport = {
                        ClientCredentialType = TcpClientCredentialType.Windows,
                        ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign
                    }
                }
            };

            fileHost = new ServiceHost(typeof(FileWCFService));
            fileHost.AddServiceEndpoint(typeof(IFileWCFService), fileBinding, fileAddress);
            fileHost.Authorization.ServiceAuthorizationManager = new Server.Authorization.AuthorizationManager();
            fileHost.Authorization.PrincipalPermissionMode = PrincipalPermissionMode.Custom;
            fileHost.Authorization.ImpersonateCallerForAllOperations = false;
            var policies = new List<System.IdentityModel.Policy.IAuthorizationPolicy>
            {
                new Server.Authorization.AuthorizationPolicy()
            };
            fileHost.Authorization.ExternalAuthorizationPolicies = policies.AsReadOnly();

            try
            {
                fileHost.Open();
                Console.WriteLine("FileWCFService host started (backup).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open FileWCFService host (backup): {ex.Message}");
            }
        }

        private void StartSyncHost()
        {
            var syncBinding = new NetTcpBinding
            {
                Security = {
                    Mode = SecurityMode.Transport,
                    Transport = {
                        ClientCredentialType = TcpClientCredentialType.Certificate,
                    }
                }
            };

            syncHost = new ServiceHost(typeof(SyncWCFService));
            syncHost.AddServiceEndpoint(typeof(ISyncWCFService), syncBinding, syncAddress);

            syncHost.Credentials.ServiceCertificate.Certificate = clientCertificate;
            syncHost.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            syncHost.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new Server.Authorization.CertificateValidator();

            try
            {
                syncHost.Open();
                Console.WriteLine("SyncWCFService host started (backup).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open SyncWCFService host (backup): {ex.Message}");
            }
        }

        public void Stop()
        {
            // No services to stop in backup mode
        }
    }
}
