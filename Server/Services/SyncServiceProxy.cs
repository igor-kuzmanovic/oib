using System;
using System.ServiceModel;
using System.Security.Cryptography.X509Certificates;

namespace Server.Services
{
    public class SyncServiceProxy : IDisposable
    {
        private ChannelFactory<ISyncWCFService> factory;
        private ISyncWCFService proxy;

        public SyncServiceProxy(string address, X509Certificate2 clientCertificate, X509Certificate2 remoteServerCertificate)
        {
            var binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            var endpointAddress = new EndpointAddress(
                new Uri(address),
                new X509CertificateEndpointIdentity(remoteServerCertificate)
            );

            factory = new ChannelFactory<ISyncWCFService>(binding, endpointAddress);
            factory.Credentials.ClientCertificate.Certificate = clientCertificate;
            factory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.ChainTrust;
            factory.Credentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;

            proxy = factory.CreateChannel();
        }

        public bool Ping()
        {
            return proxy.Ping();
        }

        public void Dispose()
        {
            if (factory != null)
            {
                try { factory.Close(); } catch { factory.Abort(); }
            }
        }
    }
}
