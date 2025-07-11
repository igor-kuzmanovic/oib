using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using Contracts.Interfaces;
using Server.Services;
using Server.Infrastructure;

namespace Server
{
    public enum ServerRole { Primary, Backup }

    public class FileServer : IDisposable
    {
        private ServerRole role;
        private ServiceHost fileHost;
        private ServiceHost syncHost;
        private Timer backupTimer;
        private string fileAddress;
        private string syncAddress;
        private string remoteSyncAddress;
        private bool isRunning = false;

        public FileServer()
        {
            if (IsPortInUse(new Uri(Configuration.PrimaryServerSyncAddress).Port) ||
                IsPortInUse(new Uri(Configuration.BackupServerSyncAddress).Port))
            {
                role = ServerRole.Backup;
                fileAddress = Configuration.BackupServerAddress;
                syncAddress = Configuration.BackupServerSyncAddress;
                remoteSyncAddress = Configuration.PrimaryServerSyncAddress;
            }
            else
            {
                role = ServerRole.Primary;
                fileAddress = Configuration.PrimaryServerAddress;
                syncAddress = Configuration.PrimaryServerSyncAddress;
                remoteSyncAddress = Configuration.BackupServerSyncAddress;
            }
        }

        public void Start()
        {
            if (role == ServerRole.Primary)
            {
                StartPrimary();
            }
            else
            {
                StartBackup();
            }
            isRunning = true;
            Console.WriteLine($"Server started as {role}");
        }

        private void StartPrimary()
        {
            var fileBinding = new NetTcpBinding();
            fileBinding.Security.Mode = SecurityMode.Transport;
            fileBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            fileBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            fileHost = new ServiceHost(typeof(FileWCFService));
            fileHost.AddServiceEndpoint(typeof(IFileWCFService), fileBinding, fileAddress);
            fileHost.Authorization.ServiceAuthorizationManager = new Server.Authorization.AuthorizationManager();
            fileHost.Authorization.PrincipalPermissionMode = PrincipalPermissionMode.Custom;
            var policies = new List<System.IdentityModel.Policy.IAuthorizationPolicy> { new Server.Authorization.AuthorizationPolicy() };
            fileHost.Authorization.ExternalAuthorizationPolicies = policies.AsReadOnly();
            try
            {
                fileHost.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open FileWCFService host: {ex.Message}");
            }

            var syncBinding = new NetTcpBinding();
            syncBinding.Security.Mode = SecurityMode.Transport;
            syncBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
            syncBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            syncHost = new ServiceHost(typeof(SyncWCFService));
            syncHost.AddServiceEndpoint(typeof(ISyncWCFService), syncBinding, syncAddress);
            var creds = new System.ServiceModel.Description.ServiceCredentials();
            var serverCertificate = Server.Helpers.SecurityHelper.GetCurrentUserCertificate();
            creds.ServiceCertificate.Certificate = serverCertificate;
            creds.ClientCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.ChainTrust;
            creds.ClientCertificate.Authentication.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
            syncHost.Description.Behaviors.Add(creds);
            try
            {
                syncHost.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open SyncWCFService host: {ex.Message}");
            }
        }

        private void StartBackup()
        {
            backupTimer = new Timer(_ => CheckPrimary(), null, 5000, 5000);
        }

        private void CheckPrimary()
        {
            if (!IsPortInUse(new Uri(remoteSyncAddress).Port))
            {
                Console.WriteLine("Primary server down. Promoting to primary...");
                backupTimer?.Dispose();
                role = ServerRole.Primary;
                fileAddress = Configuration.PrimaryServerAddress;
                syncAddress = Configuration.PrimaryServerSyncAddress;
                remoteSyncAddress = Configuration.BackupServerSyncAddress;
                StartPrimary();
                Console.WriteLine($"Server switched to PRIMARY role at {DateTime.Now}");
            }
        }

        private static bool IsPortInUse(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, port);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public void Stop()
        {
            if (!isRunning) return;
            try { fileHost?.Close(); } catch (Exception ex) { Console.WriteLine($"Error closing fileHost: {ex.Message}"); }
            try { syncHost?.Close(); } catch (Exception ex) { Console.WriteLine($"Error closing syncHost: {ex.Message}"); }
            backupTimer?.Dispose();
            isRunning = false;
            Console.WriteLine("Server stopped.");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
