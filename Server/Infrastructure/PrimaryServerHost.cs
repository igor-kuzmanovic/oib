using Contracts.Helpers;
using Contracts.Interfaces;
using Server.Audit;
using Server.Authorization;
using Server.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Policy;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;

namespace Server.Infrastructure
{
    public class PrimaryServerHost : ServerHostBase
    {
        public PrimaryServerHost() : base(Configuration.PrimaryServerAddress)
        {
            serverCertificate = SecurityHelper.GetCertificate();
        }

        public override void Start()
        {
            try
            {
                Console.WriteLine($"Primary server certificate loaded: {serverCertificate.SubjectName.Name}");

                var clientBinding = new NetTcpBinding();
                clientBinding.Security.Mode = SecurityMode.Transport;
                clientBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                clientBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

                var backupBinding = new NetTcpBinding();
                backupBinding.Security.Mode = SecurityMode.Transport;
                backupBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
                backupBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

                serviceHost = new ServiceHost(typeof(WCFService));
                serviceHost.AddServiceEndpoint(typeof(IWCFService), clientBinding, serverAddress);
                serviceHost.AddServiceEndpoint(typeof(IWCFService), backupBinding, serverAddress);

                serviceHost.Authorization.ServiceAuthorizationManager = new AuthorizationManager();
                serviceHost.Authorization.PrincipalPermissionMode = PrincipalPermissionMode.Custom;

                List<IAuthorizationPolicy> policies = new List<IAuthorizationPolicy>();
                policies.Add(new AuthorizationPolicy());
                serviceHost.Authorization.ExternalAuthorizationPolicies = policies.AsReadOnly();

                serviceHost.Open();

                Console.WriteLine($"Primary server started at {serverAddress}");
                AuditFacade.ServerStarted(serverAddress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting primary server: {ex.Message}");
                AuditFacade.ServerError($"Error starting primary server: {ex.Message}", serverAddress);
                throw;
            }
        }
    }
}

