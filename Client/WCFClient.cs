using Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace Client
{
    public class WCFClient : ChannelFactory<IWCFService>, IWCFService, IDisposable
    {
        private IWCFService factory;
        private NetTcpBinding binding;
        private EndpointAddress primaryAddress;
        private EndpointAddress backupAddress;
        private bool usingPrimaryServer = true;
        private int retryCount = 0;
        private const int MAX_RETRY_COUNT = 3;
        private const int RETRY_DELAY_MS = 1000;

        public WCFClient(NetTcpBinding binding, EndpointAddress primaryAddress, EndpointAddress backupAddress)
            : base(binding, primaryAddress)
        {
            this.binding = binding;
            this.primaryAddress = primaryAddress;
            this.backupAddress = backupAddress;
            this.factory = CreateChannel();
        }

        public void Dispose()
        {
            if (factory != null)
            {
                factory = null;
            }

            Close();
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
                return operation();
            }
            catch (FaultException<SecurityException> ex)
            {
                Console.WriteLine($"Security error in '{operationName}': {ex.Message}");
                return default;
            }
            catch (FaultException ex)
            {
                Console.WriteLine($"Fault error in '{operationName}': {ex.Message}");
                return default;
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

            if (retryCount > MAX_RETRY_COUNT)
            {
                Console.WriteLine($"Maximum retry count reached for '{operationName}'. Last error: {ex.Message}");
                throw new FaultException($"Service unavailable after {MAX_RETRY_COUNT} retries: {ex.Message}");
            }

            Console.WriteLine($"Connection error in '{operationName}': {ex.Message}. Attempt {retryCount} of {MAX_RETRY_COUNT}");

            // If using primary server, switch to backup
            if (usingPrimaryServer)
            {
                Console.WriteLine("Switching to backup server...");
                SwitchToBackupServer();
            }
            // If already using backup server, try switching back to primary
            else if (retryCount > 1)
            {
                Console.WriteLine("Trying to switch back to primary server...");
                SwitchToPrimaryServer();
            }

            // Wait before retry
            Thread.Sleep(RETRY_DELAY_MS);

            // Retry the operation
            return TryExecute(operation, operationName);
        }
        private void SwitchToPrimaryServer()
        {
            try
            {
                // Close and dispose of existing connections
                if (factory != null)
                {
                    try
                    {
                        ((ICommunicationObject)factory).Close();
                    }
                    catch
                    {
                        ((ICommunicationObject)factory).Abort();
                    }
                }

                try
                {
                    Close();
                }
                catch
                {
                    Abort();
                }

                // Create a new channel factory and channel
                ChannelFactory<IWCFService> newFactory = new ChannelFactory<IWCFService>(binding, primaryAddress);
                factory = newFactory.CreateChannel();
                usingPrimaryServer = true;
                Console.WriteLine("Successfully switched to primary server.");
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
                // Close and dispose of existing connections
                if (factory != null)
                {
                    try
                    {
                        ((ICommunicationObject)factory).Close();
                    }
                    catch
                    {
                        ((ICommunicationObject)factory).Abort();
                    }
                }

                try
                {
                    Close();
                }
                catch
                {
                    Abort();
                }

                // Create a new channel factory and channel
                ChannelFactory<IWCFService> newFactory = new ChannelFactory<IWCFService>(binding, backupAddress);
                factory = newFactory.CreateChannel();
                usingPrimaryServer = false;
                Console.WriteLine("Successfully switched to backup server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to switch to backup server: {ex.Message}");
            }
        }

        public string[] ShowFolderContent(string path)
        {
            return ExecuteWithFailover(() =>
            {
                var result = factory.ShowFolderContent(path);
                Console.WriteLine("'ShowFolderContent' allowed");
                return result;
            }, "ShowFolderContent");
        }

        public FileData ReadFile(string path)
        {
            return ExecuteWithFailover(() =>
            {
                var result = factory.ReadFile(path);
                Console.WriteLine("'ReadFile' allowed");
                return result;
            }, "ReadFile");
        }

        public bool CreateFolder(string path)
        {
            return ExecuteWithFailover(() =>
            {
                var result = factory.CreateFolder(path);
                Console.WriteLine("'CreateFolder' allowed");
                return result;
            }, "CreateFolder");
        }

        public bool CreateFile(string path, FileData fileData)
        {
            return ExecuteWithFailover(() =>
            {
                var result = factory.CreateFile(path, fileData);
                Console.WriteLine("'CreateFile' allowed");
                return result;
            }, "CreateFile");
        }

        public bool Delete(string path)
        {
            return ExecuteWithFailover(() =>
            {
                var result = factory.Delete(path);
                Console.WriteLine("'Delete' allowed");
                return result;
            }, "Delete");
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            return ExecuteWithFailover(() =>
            {
                var result = factory.Rename(sourcePath, destinationPath);
                Console.WriteLine("'Rename' allowed");
                return result;
            }, "Rename");
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            return ExecuteWithFailover(() =>
            {
                var result = factory.MoveTo(sourcePath, destinationPath);
                Console.WriteLine("'MoveTo' allowed");
                return result;
            }, "MoveTo");
        }

        public bool IsConnectedToPrimary => usingPrimaryServer;

        public string GetCurrentServerAddress()
        {
            return usingPrimaryServer ? primaryAddress.Uri.ToString() : backupAddress.Uri.ToString();
        }
    }
}
