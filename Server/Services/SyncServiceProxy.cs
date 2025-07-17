using Contracts.Helpers;
using Contracts.Models;
using Server.Authorization;
using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
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
                        ClientCredentialType = TcpClientCredentialType.Certificate
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

        public StorageEvent[] GetEventsSinceId(int lastEventId)
        {
            try
            {
                return proxy.GetEventsSinceId(lastEventId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncServiceProxy] GetEventsSinceId failed: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[SyncServiceProxy] Inner: {ex.InnerException.Message}");
                return Array.Empty<StorageEvent>();
            }
        }

        public void Dispose()
        {
            CloseCommunicationObject(proxy);
            CloseCommunicationObject(factory);
            proxy = null;
            factory = null;
        }

        private void CloseCommunicationObject(object obj)
        {
            if (obj is ICommunicationObject comm)
            {
                try
                {
                    if (comm.State == CommunicationState.Faulted)
                        comm.Abort();
                    else
                        comm.Close();
                }
                catch
                {
                    comm.Abort();
                }
            }
        }
    }
}
