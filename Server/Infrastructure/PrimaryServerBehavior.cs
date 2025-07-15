using Contracts.Interfaces;
using Server.Authorization;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;

namespace Server.Infrastructure
{
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
                    }
                }
            };

            fileHost = new ServiceHost(typeof(FileWCFService));
            fileHost.AddServiceEndpoint(typeof(IFileWCFService), fileBinding, fileAddress);

            fileHost.Description.Behaviors.Remove<ServiceDebugBehavior>();
            fileHost.Description.Behaviors.Add(new ServiceDebugBehavior
            {
                IncludeExceptionDetailInFaults = true,
            });

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

            syncHost.Description.Behaviors.Remove<ServiceDebugBehavior>();
            syncHost.Description.Behaviors.Add(new ServiceDebugBehavior
            {
                IncludeExceptionDetailInFaults = true,
            });

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
            }
        }

        public void Stop()
        {
            try { fileHost?.Close(); } catch { fileHost?.Abort(); }
            try { syncHost?.Close(); } catch { syncHost?.Abort(); }
        }
    }
}
