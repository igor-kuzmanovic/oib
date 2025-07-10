using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Policy;
using System.Collections.Generic;
using System.ServiceModel.Security;
using Server.Authorization;

namespace Server.Infrastructure
{
    public abstract class ServerHostBase : IServerHost, IDisposable
    {
        protected readonly string serverAddress;
        protected ServiceHost serviceHost;
        protected X509Certificate2 serverCertificate;

        public bool IsRunning => serviceHost?.State == CommunicationState.Opened;
        public string Address => serverAddress;

        protected ServerHostBase(string address)
        {
            serverAddress = address ?? throw new ArgumentNullException(nameof(address));
        }

        public abstract void Start();

        public void Stop()
        {
            if (serviceHost != null)
            {
                try
                {
                    if (serviceHost.State == CommunicationState.Opened)
                    {
                        serviceHost.Close();
                    }
                    else
                    {
                        serviceHost.Abort();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping server: {ex.Message}");
                    serviceHost.Abort();
                }
                finally
                {
                    serviceHost = null;
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
