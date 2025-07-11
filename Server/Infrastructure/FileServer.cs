using System;
using System.ServiceModel;
using System.Threading;
using System.Collections.Generic;
using Contracts.Interfaces;
using Server.Services;
using Contracts.Helpers;
using System.ServiceModel.Description;
using System.Security.Cryptography.X509Certificates;

namespace Server.Infrastructure
{
    public enum ServerRole { Primary, Backup }
    public class FileServer : IDisposable
    {
        private readonly object promotionLock = new object();
        private ServerRole role;
        private ServiceHost fileHost;
        private ServiceHost syncHost;
        private Timer backupTimer;
        private readonly string fileAddress;
        private readonly string syncAddress;
        private readonly string remoteSyncAddress;
        private readonly X509Certificate2 remoteServerCertificate;
        private bool isRunning;
        private bool isBackupActive;

        public FileServer()
        {
            var serverCertificate = SecurityHelper.GetCurrentUserCertificate();
            bool primaryUp = false, backupUp = false;

            string currentCN = SecurityHelper.GetName(serverCertificate);
            string remoteCN = currentCN == Configuration.PrimaryServerCN ? Configuration.BackupServerCN : Configuration.PrimaryServerCN;
            remoteServerCertificate = SecurityHelper.GetCertificate(StoreName.TrustedPeople, StoreLocation.LocalMachine, remoteCN);
            Console.WriteLine($"Loaded local certificate CN: {currentCN}");
            Console.WriteLine($"Expected remote certificate CN: {remoteCN}");
            if (serverCertificate == null)
            {
                Console.WriteLine("Error: Local certificate not found or invalid.");
                throw new ApplicationException("Local certificate not found or invalid.");
            }
            if (remoteServerCertificate == null)
            {
                Console.WriteLine($"Error: Remote certificate '{remoteCN}' not found or invalid.");
                throw new ApplicationException($"Remote certificate '{remoteCN}' not found or invalid.");
            }

            try
            {
                using (var proxy = new SyncServiceProxy(Configuration.PrimaryServerSyncAddress, serverCertificate, remoteServerCertificate))
                {
                    primaryUp = proxy.Ping();
                    Console.WriteLine($"Ping to Primary ({Configuration.PrimaryServerSyncAddress}): {(primaryUp ? "Success" : "Failed")}");
                }
            }
            catch (Exception ex)
            {
                primaryUp = false;
                Console.WriteLine($"Ping to Primary ({Configuration.PrimaryServerSyncAddress}): Failed. Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }

            try
            {
                using (var proxy = new SyncServiceProxy(Configuration.BackupServerSyncAddress, serverCertificate, remoteServerCertificate))
                {
                    backupUp = proxy.Ping();
                    Console.WriteLine($"Ping to Backup ({Configuration.BackupServerSyncAddress}): {(backupUp ? "Success" : "Failed")}");
                }
            }
            catch (Exception ex)
            {
                backupUp = false;
                Console.WriteLine($"Ping to Backup ({Configuration.BackupServerSyncAddress}): Failed. Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }

            if (primaryUp && backupUp)
                throw new ApplicationException("Both sync services are running. Cannot start another server.");
            else if (primaryUp || backupUp)
            {
                role = ServerRole.Backup;
                if (primaryUp)
                {
                    fileAddress = Configuration.BackupServerAddress;
                    syncAddress = Configuration.BackupServerSyncAddress;
                    remoteSyncAddress = Configuration.PrimaryServerSyncAddress;
                }
                else
                {
                    fileAddress = Configuration.PrimaryServerAddress;
                    syncAddress = Configuration.PrimaryServerSyncAddress;
                    remoteSyncAddress = Configuration.BackupServerSyncAddress;
                }
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
            isRunning = true;
            StartPrimary();
            if (role == ServerRole.Backup)
                StartBackup();
            Console.WriteLine($"Server started as {role}");
        }

        private void StartPrimary()
        {

            var fileBinding = new NetTcpBinding();
            fileBinding.Security.Mode = SecurityMode.Transport;
            fileBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            fileBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
            fileBinding.OpenTimeout = TimeSpan.FromSeconds(1);
            fileBinding.CloseTimeout = TimeSpan.FromSeconds(1);
            fileBinding.ReceiveTimeout = TimeSpan.FromSeconds(2);
            fileBinding.SendTimeout = TimeSpan.FromSeconds(2);

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
            syncBinding.OpenTimeout = TimeSpan.FromSeconds(1);
            syncBinding.CloseTimeout = TimeSpan.FromSeconds(1);
            syncBinding.ReceiveTimeout = TimeSpan.FromSeconds(2);
            syncBinding.SendTimeout = TimeSpan.FromSeconds(2);

            syncHost = new ServiceHost(typeof(SyncWCFService));
            syncHost.AddServiceEndpoint(typeof(ISyncWCFService), syncBinding, syncAddress);

            var creds = new System.ServiceModel.Description.ServiceCredentials();
            var serverCertificate = SecurityHelper.GetCurrentUserCertificate();
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
            isBackupActive = true;
            backupTimer = new Timer(_ => CheckPrimary(), null, 1000, 1000);
        }

        private void CheckPrimary()
        {
            lock (promotionLock)
            {
                if (!isBackupActive || role != ServerRole.Backup)
                    return;

                var clientCertificate = SecurityHelper.GetCurrentUserCertificate();
                bool remoteIsUp = false;

                try
                {
                    using (var proxy = new SyncServiceProxy(remoteSyncAddress, clientCertificate, remoteServerCertificate))
                    {
                        remoteIsUp = proxy.Ping();
                    }
                }
                catch
                {
                    remoteIsUp = false;
                }

                if (!remoteIsUp && role == ServerRole.Backup)
                {
                    Console.WriteLine("Remote server down. Promoting to primary...");
                    isBackupActive = false;
                    backupTimer?.Dispose();
                    backupTimer = null;
                    role = ServerRole.Primary;
                    Console.WriteLine($"Server switched to PRIMARY role at {DateTime.Now}");
                }
            }
        }

        public void Stop()
        {
            if (!isRunning) return;
            try
            {
                fileHost?.Close();
            }
            catch (Exception ex) { Console.WriteLine($"Error closing fileHost: {ex.Message}"); }
            try
            {
                syncHost?.Close();
            }
            catch (Exception ex) { Console.WriteLine($"Error closing syncHost: {ex.Message}"); }
            isBackupActive = false;
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
