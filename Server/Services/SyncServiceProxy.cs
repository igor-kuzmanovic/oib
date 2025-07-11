using System;
using System.ServiceModel;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using Contracts.Helpers;

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
                    }
                },
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
            try
            {
                return proxy.Ping();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncServiceProxy] Exception in Ping: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[SyncServiceProxy] Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public void Dispose()
        {
            if (proxy is ICommunicationObject comm)
            {
                try
                {
                    if (comm.State == CommunicationState.Faulted)
                    {
                        Console.WriteLine("[SyncServiceProxy] Proxy faulted. Aborting...");
                        comm.Abort();
                    }
                    else
                    {
                        comm.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SyncServiceProxy] Exception while disposing proxy: {ex.GetType().Name}: {ex.Message}");
                    comm.Abort();
                }
            }

            if (factory != null)
            {
                try
                {
                    if (factory.State == CommunicationState.Faulted)
                    {
                        Console.WriteLine("[SyncServiceProxy] Factory faulted. Aborting...");
                        factory.Abort();
                    }
                    else
                    {
                        factory.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SyncServiceProxy] Exception while disposing factory: {ex.GetType().Name}: {ex.Message}");
                    factory.Abort();
                }
            }

            proxy = null;
            factory = null;
        }
    }
}
