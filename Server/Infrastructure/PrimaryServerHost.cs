using Contracts.Interfaces;
using Server.Audit;
using Server.Services;
using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace Server.Infrastructure
{
    public class PrimaryServerHost : ServerHostBase
    {
        public PrimaryServerHost() : base(Configuration.ServerAddress) { }

        public override void Start()
        {
            try
            {
                Console.WriteLine($"Primary server certificate loaded: {serverCertificate.SubjectName.Name}");

                var binding = new NetTcpBinding();
                binding.Security.Mode = SecurityMode.Transport;
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
                binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

                serviceHost = new ServiceHost(typeof(WCFService));
                serviceHost.AddServiceEndpoint(typeof(IWCFService), binding, serverAddress);

                ConfigureAuthorization(serviceHost);

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

