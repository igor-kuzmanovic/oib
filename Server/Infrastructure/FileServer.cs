using Contracts.Helpers;
using Contracts.Interfaces;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Threading;

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
        private readonly X509Certificate2 serverCertificate;
        private readonly X509Certificate2 remoteServerCertificate;
        private readonly X509Certificate2 clientCertificate;
        private bool isPrimaryActive;
        private bool isBackupActive;

        public FileServer()
        {
            serverCertificate = clientCertificate = SecurityHelper.GetCurrentUserCertificate();
            if (serverCertificate == null)
                throw new ApplicationException("Local server/client certificate not found or invalid.");

            string currentCN = SecurityHelper.GetName(serverCertificate);
            Console.WriteLine($"Loaded local certificate CN: {currentCN}, Thumbprint: {serverCertificate.Thumbprint}");

            string remoteCN = currentCN == Configuration.PrimaryServerCN ? Configuration.BackupServerCN : Configuration.PrimaryServerCN;
            remoteServerCertificate = SecurityHelper.GetCertificate(StoreName.TrustedPeople, StoreLocation.LocalMachine, remoteCN);
            if (remoteServerCertificate == null)
                throw new ApplicationException($"Remote certificate '{remoteCN}' not found or invalid.");
            Console.WriteLine($"Expected remote certificate CN: {remoteCN}, Thumbprint: {remoteServerCertificate.Thumbprint}");

            bool primaryUp = !IsPortAvailable(new Uri(Configuration.PrimaryServerSyncAddress).Port);
            bool backupUp = !IsPortAvailable(new Uri(Configuration.BackupServerSyncAddress).Port);

            if (primaryUp && backupUp)
                throw new ApplicationException("Both sync services are running. Cannot start another server.");
            else if (primaryUp || backupUp)
            {
                role = ServerRole.Backup;
                if (primaryUp)
                {
                    Console.WriteLine("Primary server is up, configuring as backup.");
                    fileAddress = Configuration.BackupServerAddress;
                    syncAddress = Configuration.BackupServerSyncAddress;
                    remoteSyncAddress = Configuration.PrimaryServerSyncAddress;
                }
                else
                {
                    Console.WriteLine("Backup server is up, configuring as primary.");
                    fileAddress = Configuration.PrimaryServerAddress;
                    syncAddress = Configuration.PrimaryServerSyncAddress;
                    remoteSyncAddress = Configuration.BackupServerSyncAddress;
                }
            }
            else
            {
                Console.WriteLine("No active servers found, configuring as primary.");
                role = ServerRole.Primary;
                fileAddress = Configuration.PrimaryServerAddress;
                syncAddress = Configuration.PrimaryServerSyncAddress;
                remoteSyncAddress = Configuration.BackupServerSyncAddress;
            }
        }

        public void Start()
        {
            if (role == ServerRole.Primary)
                StartPrimary();
            else
                StartBackup();

            Console.WriteLine($"Server started as {role}");
        }

        private void StartPrimary()
        {
            isPrimaryActive = true;
            StartFileHost();
            StartSyncHost();
        }

        private void StartFileHost()
        {
            if (fileHost != null && fileHost.State == CommunicationState.Opened)
                return;

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

            var policies = new List<System.IdentityModel.Policy.IAuthorizationPolicy>
            {
                new Server.Authorization.AuthorizationPolicy()
            };
            fileHost.Authorization.ExternalAuthorizationPolicies = policies.AsReadOnly();

            try
            {
                fileHost.Open();
                Console.WriteLine("FileWCFService host started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open FileWCFService host: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.GetType()}: {ex.InnerException.Message}");
            }
        }

        private void StartSyncHost()
        {
            if (syncHost != null && syncHost.State == CommunicationState.Opened)
                return;

            var syncBinding = new NetTcpBinding
            {
                Security = {
                    Mode = SecurityMode.Transport,
                    Transport = {
                        ClientCredentialType = TcpClientCredentialType.Certificate,
                    }
                },
            };

            syncHost = new ServiceHost(typeof(SyncWCFService));
            syncHost.AddServiceEndpoint(typeof(ISyncWCFService), syncBinding, syncAddress);

            syncHost.Credentials.ServiceCertificate.Certificate = serverCertificate;
            syncHost.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            syncHost.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new CertificateValidator();

            try
            {
                syncHost.Open();
                Console.WriteLine("SyncWCFService host started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open SyncWCFService host: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.GetType()}: {ex.InnerException.Message}");
            }
        }

        private void StartBackup()
        {
            isBackupActive = true;
            backupTimer = new Timer(_ => CheckPrimary(), null, 5000, 5000);
        }

        private void CheckPrimary()
        {
            lock (promotionLock)
            {
                if (!isBackupActive || role != ServerRole.Backup)
                    return;

                bool remoteIsUp = false;

                try
                {
                    using (var proxy = new SyncServiceProxy(remoteSyncAddress, clientCertificate, remoteServerCertificate))
                    {
                        remoteIsUp = proxy.Ping();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FileServer] Exception in CheckPrimary: {ex.GetType().Name}: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[FileServer] Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    }
                    remoteIsUp = false;
                }

                if (!remoteIsUp && role == ServerRole.Backup)
                {
                    Console.WriteLine("Remote server down. Promoting to primary...");
                    isBackupActive = false;
                    role = ServerRole.Primary;
                    StartPrimary();
                    Console.WriteLine($"Server switched to PRIMARY role");
                    backupTimer?.Dispose();
                    backupTimer = null;
                    Console.WriteLine("Backup timer disposed");
                }
            }
        }

        private static bool IsPortAvailable(int port)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                listener?.Stop();
            }
        }

        public void Stop()
        {
            if (!isPrimaryActive)
                return;

            Console.WriteLine("[FileServer] Stopping server...");

            isBackupActive = false;

            if (backupTimer != null)
            {
                try
                {
                    backupTimer.Dispose();
                    backupTimer = null;
                    Console.WriteLine("[FileServer] Backup timer disposed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FileServer] Error disposing backupTimer: {ex.Message}");
                }
            }

            if (fileHost != null)
            {
                try
                {
                    if (fileHost.State != CommunicationState.Closed && fileHost.State != CommunicationState.Closing)
                    {
                        fileHost.Close();
                        Console.WriteLine("[FileServer] fileHost closed.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FileServer] Error closing fileHost: {ex.Message}");
                    fileHost.Abort();
                }
                finally
                {
                    fileHost = null;
                }
            }

            if (syncHost != null)
            {
                try
                {
                    if (syncHost.State != CommunicationState.Closed && syncHost.State != CommunicationState.Closing)
                    {
                        syncHost.Close();
                        Console.WriteLine("[FileServer] syncHost closed.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FileServer] Error closing syncHost: {ex.Message}");
                    syncHost.Abort();
                }
                finally
                {
                    syncHost = null;
                }
            }

            isPrimaryActive = false;

            Console.WriteLine("[FileServer] Server stopped.");
        }


        public void Dispose()
        {
            Stop();
        }
    }
}
