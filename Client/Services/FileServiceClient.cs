using System;
using System.ServiceModel;
using System.Threading;
using Contracts.Interfaces;
using Contracts.Models;
using Contracts.Exceptions;
using Contracts.Authorization;
using Client.Infrastructure;

namespace Client.Services
{
    public class FileServiceClient : IFileServiceClient, IDisposable
    {
        private readonly NetTcpBinding binding;
        private readonly EndpointAddress primaryAddress;
        private readonly EndpointAddress backupAddress;

        private IWCFService serviceProxy;
        private bool usingPrimaryServer = true;

        private readonly int maxRetryCount = 3;
        private readonly int retryDelayMs = 1000;
        private int retryCount = 0;

        public FileServiceClient()
        {
            binding = new NetTcpBinding();
    
            binding.OpenTimeout = TimeSpan.FromSeconds(3);
            binding.SendTimeout = TimeSpan.FromSeconds(5);
            binding.ReceiveTimeout = TimeSpan.FromSeconds(10);

            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
    
            primaryAddress = new EndpointAddress(new Uri(Configuration.PrimaryServerAddress));
            backupAddress = new EndpointAddress(new Uri(Configuration.BackupServerAddress));

            CreateServiceProxy();
        }

        private void CreateServiceProxy()
        {
            try
            {
                ChannelFactory<IWCFService> factory = new ChannelFactory<IWCFService>(
                    binding, usingPrimaryServer ? primaryAddress : backupAddress);

                serviceProxy = factory.CreateChannel();

                Console.WriteLine($"Connected to {(usingPrimaryServer ? "primary" : "backup")} server at {GetCurrentServerAddress()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating service proxy: {ex.Message}");
                throw;
            }
        }

        private void RecreateChannel()
        {
            try
            {
                if (serviceProxy != null)
                {
                    try
                    {
                        ((ICommunicationObject)serviceProxy).Abort();
                    }
                    catch
                    {
                    }
                }

                CreateServiceProxy();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recreating channel: {ex.Message}");


                if (usingPrimaryServer)
                {
                    SwitchToBackupServer();
                }
                else
                {
                    SwitchToPrimaryServer();
                }
            }
        }

        private T ExecuteWithFailover<T>(Func<T> operation, string operationName)
        {
            try
            {
                retryCount = 0;
                return TryExecute(operation, operationName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error in '{operationName}': {ex.Message}");
                throw;
            }
        }

        private T TryExecute<T>(Func<T> operation, string operationName)
        {
            try
            {
                if (serviceProxy != null && ((ICommunicationObject)serviceProxy).State == CommunicationState.Faulted)
                {
                    Console.WriteLine("Communication channel is faulted. Recreating channel...");
                    RecreateChannel();
                }

                return operation();
            }
            catch (FaultException<SecurityException> ex)
            {
                Console.WriteLine($"Security error in '{operationName}': {ex.Message}");
                return default;
            }
            catch (FaultException<FileSystemException> ex)
            {
                Console.WriteLine($"File system error in '{operationName}': {ex.Message}");
                return default;
            }
            catch (FaultException ex)
            {
                Console.WriteLine($"Fault error in '{operationName}': {ex.Message}");
                return HandleFailover(operation, operationName, ex);
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"Communication error in '{operationName}': {ex.Message}");
                return HandleFailover(operation, operationName, ex);
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout error in '{operationName}': {ex.Message}");
                return HandleFailover(operation, operationName, ex);
            }
        }

        private T HandleFailover<T>(Func<T> operation, string operationName, Exception ex)
        {
            retryCount++;

            if (retryCount > maxRetryCount)
            {
                Console.WriteLine($"Maximum retry count reached for '{operationName}'. Last error: {ex.Message}");
                throw new FaultException($"Service unavailable after {maxRetryCount} retries: {ex.Message}");
            }

            Console.WriteLine($"Connection error in '{operationName}': {ex.Message}. Attempt {retryCount} of {maxRetryCount}");


            if (usingPrimaryServer)
            {
                Console.WriteLine("Switching to backup server...");
                SwitchToBackupServer();
            }

            else if (retryCount > 1)
            {
                Console.WriteLine("Trying to switch back to primary server...");
                SwitchToPrimaryServer();
            }


            Thread.Sleep(retryDelayMs);


            return TryExecute(operation, operationName);
        }

        private void SwitchToPrimaryServer()
        {
            try
            {
                if (serviceProxy != null)
                {
                    try
                    {
                        ((ICommunicationObject)serviceProxy).Close();
                    }
                    catch
                    {
                        ((ICommunicationObject)serviceProxy).Abort();
                    }

                    serviceProxy = null;
                }

                usingPrimaryServer = true;
                CreateServiceProxy();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to switch to primary server: {ex.Message}");
            }
        }

        private void SwitchToBackupServer()
        {
            try
            {
                if (serviceProxy != null)
                {
                    try
                    {
                        ((ICommunicationObject)serviceProxy).Close();
                    }
                    catch
                    {
                        ((ICommunicationObject)serviceProxy).Abort();
                    }

                    serviceProxy = null;
                }

                usingPrimaryServer = false;
                CreateServiceProxy();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to switch to backup server: {ex.Message}");
            }
        }

        public string[] ShowFolderContent(string path)
        {
            return ExecuteWithFailover(() => serviceProxy.ShowFolderContent(path), "ShowFolderContent");
        }

        public FileData ReadFile(string path)
        {
            return ExecuteWithFailover(() =>
            {
                var result = serviceProxy.ReadFile(path);
                Console.WriteLine("'ReadFile' allowed");
                return result;
            }, "ReadFile");
        }

        public bool CreateFolder(string path)
        {
            return ExecuteWithFailover(() =>
            {
                var result = serviceProxy.CreateFolder(path);
                Console.WriteLine("'CreateFolder' allowed");
                return result;
            }, "CreateFolder");
        }

        public bool CreateFile(string path, FileData fileData)
        {
            return ExecuteWithFailover(() =>
            {
                var result = serviceProxy.CreateFile(path, fileData);
                Console.WriteLine("'CreateFile' allowed");
                return result;
            }, "CreateFile");
        }

        public bool Delete(string path)
        {
            return ExecuteWithFailover(() =>
            {
                var result = serviceProxy.Delete(path);
                Console.WriteLine("'Delete' allowed");
                return result;
            }, "Delete");
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            return ExecuteWithFailover(() =>
            {
                var result = serviceProxy.Rename(sourcePath, destinationPath);
                Console.WriteLine("'Rename' allowed");
                return result;
            }, "Rename");
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            return ExecuteWithFailover(() =>
            {
                var result = serviceProxy.MoveTo(sourcePath, destinationPath);
                Console.WriteLine("'MoveTo' allowed");
                return result;
            }, "MoveTo");
        }

        public string GetCurrentServerAddress()
        {
            return usingPrimaryServer ? Configuration.PrimaryServerAddress : Configuration.BackupServerAddress;
        }

        public void Dispose()
        {
            if (serviceProxy != null)
            {
                try
                {
                    ((ICommunicationObject)serviceProxy).Close();
                }
                catch
                {
                    ((ICommunicationObject)serviceProxy).Abort();
                }

                serviceProxy = null;
            }
        }
    }
}
