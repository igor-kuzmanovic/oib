using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using Contracts;
using System.IdentityModel.Policy;
using SecurityManager;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Server
{
    public enum ServerRole
    {
        Primary,
        Backup
    }
    public class ServerManager
    {
        public static readonly string PrimaryServerAddress = "net.tcp://localhost:9999/WCFService";
        public static readonly string BackupServerAddress = "net.tcp://localhost:8888/WCFService";
        public static readonly string DataDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "FileServerData");

        private static bool isPrimaryServer = false;
        private readonly object lockObject = new object();
        private ServiceHost serviceHost;

        public static bool IsPrimaryServer()
        {
            return isPrimaryServer;
        }
        public void StartServer()
        {
            Console.WriteLine("Starting File Server..."); DisplayStorageLocations();

            Directory.CreateDirectory(DataDirectory);

            if (IsPrimaryServerRunning())
            {
                Console.WriteLine("Primary server is already running. Starting as backup server...");
                StartAsBackup();
            }
            else
            {
                Console.WriteLine("Starting as primary server...");
                StartAsPrimary();
            }
        }
        private void DisplayStorageLocations()
        {
            Console.WriteLine("File Storage Location:");
            Console.WriteLine($"  Files Directory: {DataDirectory}");
            Console.WriteLine();
        }
        public void StartAsBackup()
        {
            StartBackupServer();

            Console.WriteLine("Backup WCFService is opened. Press <enter> to finish...");
            Console.ReadLine();

            CleanupServer();
        }

        public void StartAsPrimary()
        {
            StartPrimaryServer();

            Console.WriteLine("Primary WCFService is opened. Press <enter> to finish...");
            Console.ReadLine();

            CleanupServer();
        }

        // Server operations
        private void StartPrimaryServer()
        {
            try
            {
                CloseExistingHost();

                NetTcpBinding binding = new NetTcpBinding();

                // Create the service host
                serviceHost = new ServiceHost(typeof(WCFService));

                // Add the service endpoint with the primary address
                serviceHost.AddServiceEndpoint(typeof(IWCFService), binding, PrimaryServerAddress);

                // Set up custom authorization
                ConfigureCustomAuthorization(serviceHost);                // Open the service host
                serviceHost.Open();
                isPrimaryServer = true;

                Console.WriteLine($"Primary server started on {PrimaryServerAddress}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting primary server: {ex.Message}");
                Audit.ServerError($"Error starting primary server: {ex.Message}", PrimaryServerAddress);
            }
        }

        private void StartBackupServer()
        {
            try
            {
                CloseExistingHost();

                NetTcpBinding binding = new NetTcpBinding();

                // Configure the backup server with certificate security
                binding.Security.Mode = SecurityMode.TransportWithMessageCredential;
                binding.Security.Message.ClientCredentialType = MessageCredentialType.Windows;
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;

                // Create the service host
                serviceHost = new ServiceHost(typeof(WCFService));

                // Add the service endpoint with the backup address
                serviceHost.AddServiceEndpoint(typeof(IWCFService), binding, BackupServerAddress);

                // Configure the certificate
                serviceHost.Credentials.ServiceCertificate.SetCertificate(
                    StoreLocation.LocalMachine,
                    StoreName.My,
                    X509FindType.FindBySubjectName,
                    "FileServerBackup");

                // Set up custom authorization
                ConfigureCustomAuthorization(serviceHost);

                // Open the service host
                serviceHost.Open(); isPrimaryServer = false;

                Console.WriteLine($"Backup server started on {BackupServerAddress}");
                Audit.BackupServerStarted(BackupServerAddress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting backup server: {ex.Message}");
                Audit.BackupServerError($"Error starting backup server: {ex.Message}", BackupServerAddress);
            }
        }

        // Helper methods
        public static bool IsPrimaryServerRunning()
        {
            try
            {
                // Try to connect to the primary server
                NetTcpBinding binding = new NetTcpBinding();
                binding.OpenTimeout = TimeSpan.FromSeconds(2); // Short timeout for check

                ChannelFactory<IWCFService> factory = null;
                IWCFService channel = null;

                try
                {
                    factory = new ChannelFactory<IWCFService>(binding, new EndpointAddress(PrimaryServerAddress));
                    channel = factory.CreateChannel();

                    // Try a simple operation
                    using (new OperationContextScope((IClientChannel)channel))
                    {
                        // Just try to connect
                        ((ICommunicationObject)channel).Open();

                        // If we reach here, primary is responsive
                        return true;
                    }
                }
                catch (Exception)
                {
                    // Primary is not responsive or not running
                    return false;
                }
                finally
                {
                    // Clean up
                    if (channel != null)
                    {
                        try
                        {
                            ((ICommunicationObject)channel).Close();
                        }
                        catch
                        {
                            ((ICommunicationObject)channel).Abort();
                        }
                    }

                    if (factory != null)
                    {
                        try
                        {
                            factory.Close();
                        }
                        catch
                        {
                            factory.Abort();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking primary server: {ex.Message}");
                return false;
            }
        }

        private void CloseExistingHost()
        {
            if (serviceHost != null)
            {
                if (serviceHost.State == CommunicationState.Opened)
                {
                    serviceHost.Close();
                }
                else
                {
                    serviceHost.Abort();
                }
                serviceHost = null;
            }
        }
        private void ConfigureCustomAuthorization(ServiceHost host)
        {
            host.Authorization.ServiceAuthorizationManager = new CustomAuthorizationManager();
            host.Authorization.PrincipalPermissionMode = PrincipalPermissionMode.Custom;
            List<IAuthorizationPolicy> policies = new List<IAuthorizationPolicy>();
            policies.Add(new CustomAuthorizationPolicy());
            host.Authorization.ExternalAuthorizationPolicies = policies.AsReadOnly();

            foreach (var endpoint in host.Description.Endpoints)
            {
                var binding = endpoint.Binding as NetTcpBinding;
                if (binding != null)
                {
                    binding.Security.Mode = SecurityMode.Transport;
                    binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                    binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
                }
            }

            var serviceBehavior = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            if (serviceBehavior == null)
            {
                serviceBehavior = new ServiceBehaviorAttribute();
                host.Description.Behaviors.Add(serviceBehavior);
            }

            serviceBehavior.ImpersonateCallerForAllOperations = false;
            serviceBehavior.IncludeExceptionDetailInFaults = true;
        }
        private void CleanupServer()
        {
            if (serviceHost != null && serviceHost.State == CommunicationState.Opened)
            {
                try
                {
                    serviceHost.Close();
                    Console.WriteLine("Server closed gracefully.");
                }
                catch
                {
                    serviceHost.Abort();
                    Console.WriteLine("Server aborted due to error during close.");
                }
            }
        }
    }
}
