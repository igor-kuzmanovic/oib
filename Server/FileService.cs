using System;
using System.IO;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Threading;
using Contracts;
using SecurityManager;

namespace Server
{
    public class FileService : IWCFService
    {
        private string GetCurrentUser()
        {
            CustomPrincipal principal = Thread.CurrentPrincipal as CustomPrincipal;
            return Formatter.ParseName(principal?.Identity?.Name ?? "Unknown");
        }

        private string GetServerAddress()
        {
            try
            {
                Uri uri = OperationContext.Current?.Channel?.LocalAddress?.Uri;
                return uri?.ToString() ?? (ServerManager.IsPrimaryServer() ? ServerManager.PrimaryServerAddress : ServerManager.BackupServerAddress);
            }
            catch
            {
                return ServerManager.IsPrimaryServer() ? ServerManager.PrimaryServerAddress : ServerManager.BackupServerAddress;
            }
        }

        private string GetAction()
        {
            return OperationContext.Current?.IncomingMessageHeaders?.Action ?? "UnknownAction";
        }
        private void LogSuccess(string user)
        {
            try
            {
                string serverAddress = GetServerAddress();
                Audit.AuthorizationSuccess(user, GetAction(), serverAddress);
            }
            catch (Exception e)
            {
                Console.WriteLine("Audit success log failed: " + e.Message);
            }
        }
        private void LogFailure(string user, string reason)
        {
            try
            {
                string serverAddress = GetServerAddress();
                Audit.AuthorizationFailed(user, GetAction(), reason, serverAddress);
            }
            catch (Exception e)
            {
                Console.WriteLine("Audit failure log failed: " + e.Message);
            }
        }
        private bool PerformAsEditor(Action action, string operationName)
        {
            string user = GetCurrentUser();
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Error in {operationName}: {ex.Message}");
                throw new FaultException($"Error during {operationName} operation: {ex.Message}");
            }
        }

        public bool CreateFile(string path, FileData fileData)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("Change"))
            {
                LogFailure(user, "CreateFile requires Change permission.");
                throw new FaultException<SecurityException>(new SecurityException($"User {user} is not authorized for CreateFile."));
            }
            try
            {
                if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    LogFailure(user, "Only .txt files are supported.");
                    throw new FaultException("Only .txt files are supported.");
                }

                // Resolve the path to ensure it's in the data directory
                string resolvedPath = ResolvePath(path); byte[] decryptedContent = EncryptionHelper.DecryptContent(fileData);

                bool result = PerformAsEditor(() =>
                {
                    string directory = Path.GetDirectoryName(resolvedPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllBytes(resolvedPath, decryptedContent);
                    string serverAddress = GetServerAddress();
                    Audit.FileCreated(user, path, serverAddress);
                    LogSuccess(user);
                }, "CreateFile");

                return result;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in CreateFile: {ex.Message}");
                throw new FaultException("Error while creating file.");
            }
        }
        public bool CreateFolder(string path)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("Change"))
            {
                LogFailure(user, "CreateFolder requires Change permission.");
                throw new FaultException<SecurityException>(new SecurityException($"User {user} is not authorized for CreateFolder."));
            }

            try
            {
                // Resolve the path to ensure it's in the data directory
                string resolvedPath = ResolvePath(path);

                bool result = PerformAsEditor(() =>
                {
                    Directory.CreateDirectory(resolvedPath);
                    string serverAddress = GetServerAddress();
                    Audit.FolderCreated(user, path, serverAddress);
                    LogSuccess(user);
                }, "CreateFolder");

                return result;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in CreateFolder: {ex.Message}");
                throw new FaultException("Error while creating folder.");
            }
        }
        public bool Delete(string path)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("Delete"))
            {
                LogFailure(user, "Delete requires Delete permission.");
                throw new FaultException<SecurityException>(new SecurityException($"User {user} is not authorized for Delete."));
            }

            try
            {
                // Resolve the path to ensure it's in the data directory
                string resolvedPath = ResolvePath(path);

                bool result = PerformAsEditor(() =>
                {
                    bool isFile = File.Exists(resolvedPath);
                    bool isDirectory = Directory.Exists(resolvedPath); if (isFile)
                    {
                        File.Delete(resolvedPath);
                        string serverAddress = GetServerAddress();
                        Audit.FileDeleted(user, path, serverAddress);
                    }
                    else if (isDirectory)
                    {
                        Directory.Delete(resolvedPath, true);
                        string serverAddress = GetServerAddress();
                        Audit.FolderDeleted(user, path, serverAddress);
                    }
                    else
                    {
                        throw new FaultException("File or folder not found.");
                    }
                    LogSuccess(user);
                }, "Delete");

                return result;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in Delete: {ex.Message}");
                throw new FaultException("Error while deleting.");
            }
        }
        public bool MoveTo(string sourcePath, string destinationPath)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("Change"))
            {
                LogFailure(user, "MoveTo requires Change permission.");
                throw new FaultException<SecurityException>(new SecurityException($"User {user} is not authorized for MoveTo."));
            }

            try
            {
                // Resolve both paths to ensure they're in the data directory
                string resolvedSourcePath = ResolvePath(sourcePath);
                string resolvedDestinationPath = ResolvePath(destinationPath);

                bool result = PerformAsEditor(() =>
                {
                    bool isFile = File.Exists(resolvedSourcePath);
                    bool isDirectory = Directory.Exists(resolvedSourcePath); if (isFile)
                    {
                        File.Move(resolvedSourcePath, resolvedDestinationPath);
                        string serverAddress = GetServerAddress();
                        Audit.FileMoved(user, sourcePath, destinationPath, serverAddress);
                    }
                    else if (isDirectory)
                    {
                        Directory.Move(resolvedSourcePath, resolvedDestinationPath);
                        string serverAddress = GetServerAddress();
                        Audit.FolderMoved(user, sourcePath, destinationPath, serverAddress);
                    }
                    else
                    {
                        throw new FaultException("Source file or folder not found.");
                    }
                    LogSuccess(user);
                }, "MoveTo");

                return result;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in MoveTo: {ex.Message}");
                throw new FaultException("Error while moving.");
            }
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            return MoveTo(sourcePath, destinationPath);
        }

        public FileData ReadFile(string path)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("See"))
            {
                LogFailure(user, "ReadFile requires See permission.");
                throw new FaultException<SecurityException>(new SecurityException($"User {user} is not authorized for ReadFile."));
            }
            try
            {
                if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    LogFailure(user, "Only .txt files are supported.");
                    throw new FaultException("Only .txt files are supported.");
                }

                // Resolve the path to ensure it's in the data directory
                string resolvedPath = ResolvePath(path);

                if (!File.Exists(resolvedPath))
                {
                    LogFailure(user, $"File not found: {path}");
                    throw new FileNotFoundException($"File not found: {path}");
                }

                byte[] fileContent = File.ReadAllBytes(resolvedPath); FileData encryptedData = EncryptionHelper.EncryptContent(fileContent);

                LogSuccess(user);
                string serverAddress = GetServerAddress();
                Audit.FileAccessed(user, path, serverAddress);

                return encryptedData;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in ReadFile: {ex.Message}");
                throw new FaultException("Error while reading file.");
            }
        }

        public string[] ShowFolderContent(string path)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("See"))
            {
                LogFailure(user, "ShowFolderContent requires See permission.");
                throw new FaultException<SecurityException>(new SecurityException($"User {user} is not authorized for ShowFolderContent."));
            }

            try
            {
                // Resolve the path to ensure it's in the data directory
                string resolvedPath = ResolvePath(path);

                if (!Directory.Exists(resolvedPath))
                {
                    LogFailure(user, $"Folder not found: {path}");
                    throw new FaultException($"Folder not found: {path}");
                }
                string[] entries = Directory.GetFileSystemEntries(resolvedPath);
                LogSuccess(user);
                string serverAddress = GetServerAddress();
                Audit.FolderAccessed(user, path, serverAddress);
                return entries;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in ShowFolderContent: {ex.Message}");
                throw new FaultException("Error while listing folder content.");
            }
        }

        // Path resolution
        private string ResolvePath(string relativePath)
        {
            // Make sure path is safe (no .. navigation outside base dir)
            string safePath = Path.GetFullPath(Path.Combine(ServerManager.DataDirectory,
                relativePath.TrimStart('\\', '/')));

            // Ensure the path is within the data directory
            if (!safePath.StartsWith(ServerManager.DataDirectory))
            {
                throw new FaultException("Access to paths outside the data directory is not allowed.");
            }

            return safePath;
        }
    }
}