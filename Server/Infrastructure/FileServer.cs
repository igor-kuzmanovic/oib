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

namespace Server.Infrastructure
{
    public enum ServerRole { Primary, Backup }

    public class FileServer : IDisposable
    {
        private ServerRole role;
        private IServerBehavior behavior;
        private readonly string fileAddress;
        private readonly string syncAddress;
        private readonly string remoteSyncAddress;
        private readonly X509Certificate2 serverCertificate;
        private readonly X509Certificate2 remoteServerCertificate;
        private readonly X509Certificate2 clientCertificate;

        public FileServer()
        {
            serverCertificate = clientCertificate = SecurityHelper.GetCurrentUserCertificate();
            if (serverCertificate == null)
                throw new ApplicationException("Local server/client certificate not found or invalid.");

            string currentCN = SecurityHelper.GetName(serverCertificate);
            string remoteCN = currentCN == Configuration.PrimaryServerCN ? Configuration.BackupServerCN : Configuration.PrimaryServerCN;
            remoteServerCertificate = SecurityHelper.GetCertificate(StoreName.TrustedPeople, StoreLocation.LocalMachine, remoteCN);
            if (remoteServerCertificate == null)
                throw new ApplicationException($"Remote certificate '{remoteCN}' not found or invalid.");

            bool primaryUp = !IsPortAvailable(new Uri(Configuration.PrimaryServerSyncAddress).Port);
            bool backupUp = !IsPortAvailable(new Uri(Configuration.BackupServerSyncAddress).Port);

            if (primaryUp && backupUp)
                throw new ApplicationException("Both sync services are running. Cannot start another server.");

            if (primaryUp)
            {
                role = ServerRole.Backup;
                fileAddress = Configuration.BackupServerAddress;
                syncAddress = Configuration.BackupServerSyncAddress;
                remoteSyncAddress = Configuration.PrimaryServerSyncAddress;
            }
            else if (backupUp)
            {
                role = ServerRole.Backup;
                fileAddress = Configuration.PrimaryServerAddress;
                syncAddress = Configuration.PrimaryServerSyncAddress;
                remoteSyncAddress = Configuration.BackupServerSyncAddress;
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
                behavior = new PrimaryServerBehavior(fileAddress, syncAddress, serverCertificate);
            }
            else
            {
                behavior = new BackupServerBehavior(fileAddress, syncAddress, remoteSyncAddress, clientCertificate, remoteServerCertificate, PromoteToPrimary);
            }
            behavior.Start();
            Console.WriteLine($"Server started as {role}");
        }

        public void TryPromoteIfPrimaryDown()
        {
            if (role == ServerRole.Backup && behavior is BackupServerBehavior backup && !backup.IsRemotePrimaryAlive())
            {
                behavior.Stop();
                PromoteToPrimary();
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
            behavior?.Stop();
            Console.WriteLine("Server stopped.");
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public interface IServerBehavior
    {
        void Start();
        void Stop();
    }

    public class PrimaryServerBehavior : IServerBehavior
    {
        private ServiceHost fileHost;
        private ServiceHost syncHost;
        private readonly string fileAddress;
        private readonly string syncAddress;
        private readonly X509Certificate2 serverCertificate;

        public PrimaryServerBehavior(string fileAddress, string syncAddress, X509Certificate2 serverCertificate)
        {
            this.fileAddress = fileAddress;
            this.syncAddress = syncAddress;
            this.serverCertificate = serverCertificate;
        }

        public void Start()
        {
            StartFileHost();
            StartSyncHost();
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

            syncHost.Credentials.ServiceCertificate.Certificate = serverCertificate;
            syncHost.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            syncHost.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new Server.Authorization.CertificateValidator();

            try
            {
                syncHost.Open();
                Console.WriteLine("SyncWCFService host started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open SyncWCFService host: {ex.Message}");
            }
        }

        public void Stop()
        {
            try { fileHost?.Close(); } catch { fileHost?.Abort(); }
            try { syncHost?.Close(); } catch { syncHost?.Abort(); }
        }
    }

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
            StartFileHost();
            StartSyncHost();
            Console.WriteLine("Backup server started. Will promote if primary is unreachable.");
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
            try { fileHost?.Close(); } catch { fileHost?.Abort(); }
            try { syncHost?.Close(); } catch { syncHost?.Abort(); }
        }
    }
}
