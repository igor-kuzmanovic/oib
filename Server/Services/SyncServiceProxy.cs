using System;
using System.ServiceModel;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;

namespace Server.Services
{
    public class SyncServiceProxy : IDisposable
    {
        private ChannelFactory<ISyncWCFService> factory;
        private ISyncWCFService proxy;

        public SyncServiceProxy(string address, X509Certificate2 clientCertificate, X509Certificate2 remoteServerCertificate)
        {
            var binding = new NetTcpBinding
            {
                Security =
                {
                    Mode = SecurityMode.Transport,
                    Transport =
                    {
                        ClientCredentialType = TcpClientCredentialType.Certificate,
                        ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign
                    }
                }
            };

            var endpointAddress = new EndpointAddress(
                new Uri(address),
                new X509CertificateEndpointIdentity(remoteServerCertificate)
            );

            factory = new ChannelFactory<ISyncWCFService>(binding, endpointAddress);
            factory.Credentials.ClientCertificate.Certificate = clientCertificate;
            factory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            factory.Credentials.ServiceCertificate.Authentication.CustomCertificateValidator = new CertificateValidator();

            proxy = factory.CreateChannel();
        }

        public bool Ping()
        {
            return proxy.Ping();
        }

        public void Dispose()
        {
            if (proxy is ICommunicationObject comm)
            {
                try
                {
                    if (comm.State != CommunicationState.Faulted)
                        comm.Close();
                    else
                        comm.Abort();
                }
                catch
                {
                    comm.Abort();
                }
            }

            if (factory != null)
            {
                try
                {
                    if (factory.State != CommunicationState.Faulted)
                        factory.Close();
                    else
                        factory.Abort();
                }
                catch
                {
                    factory.Abort();
                }
            }

            proxy = null;
            factory = null;
        }
    }
}
