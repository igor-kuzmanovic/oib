using Client.Infrastructure;
using Contracts.Exceptions;
using Contracts.Interfaces;
using Contracts.Models;
using System;
using System.Security.Principal;
using System.ServiceModel;

namespace Client.Services
{
    public class FileServiceClient : IFileServiceClient, IDisposable
    {
        private readonly NetTcpBinding binding;
        private readonly EndpointAddress primaryAddress;
        private readonly EndpointAddress backupAddress;

        private IFileWCFService serviceProxy;
        private bool usingPrimaryServer = true;

        public FileServiceClient()
        {
            binding = new NetTcpBinding();

            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;

            primaryAddress = new EndpointAddress(new Uri(Configuration.PrimaryServerAddress));
            backupAddress = new EndpointAddress(new Uri(Configuration.BackupServerAddress));

            CreateServiceProxy();
        }

        private void CreateServiceProxy()
        {
            var factory = new ChannelFactory<IFileWCFService>(binding, usingPrimaryServer ? primaryAddress : backupAddress);
            factory.Credentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Delegation; // Allows impersonation
            serviceProxy = factory.CreateChannel();

            Console.WriteLine($"Connected to {(usingPrimaryServer ? "primary" : "backup")} server at {GetCurrentServerAddress()}");
        }

        private void SwitchServer()
        {
            DisposeChannel();
            usingPrimaryServer = !usingPrimaryServer;
            CreateServiceProxy();
        }

        private void DisposeChannel()
        {
            if (serviceProxy == null) return;

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

        private T ExecuteWithFailover<T>(Func<T> operation, string operationName)
        {
            try
            {
                if (IsChannelFaulted()) CreateServiceProxy();
                return operation();
            }
            catch (FaultException<FileSecurityException> ex)
            {
                Console.WriteLine($"Security error in '{operationName}': {ex.Detail.Message}");
                return default;
            }
            catch (FaultException<FileSystemException> ex)
            {
                Console.WriteLine($"File system error in '{operationName}': {ex.Detail.Message}");
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error of type {ex.GetType()} in '{operationName}': {ex.Message}. Trying failover.");
                return RetryOnFailover(operation, operationName);
            }
        }

        private T RetryOnFailover<T>(Func<T> operation, string operationName)
        {
            try
            {
                SwitchServer();
                return operation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failover failed in '{operationName}': {ex.Message}");
                throw;
            }
        }

        private bool IsChannelFaulted()
        {
            return serviceProxy != null && ((ICommunicationObject)serviceProxy).State == CommunicationState.Faulted;
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
            DisposeChannel();
        }
    }
}
