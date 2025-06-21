using System;
using System.IO;
using System.Linq;
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
                    throw new FaultException<FileSystemException>(new FileSystemException("Only .txt files are supported."));
                }

                if (fileData == null || fileData.Content == null || fileData.InitializationVector == null)
                {
                    LogFailure(user, "CreateFile received null or incomplete FileData.");
                    throw new FaultException<FileSystemException>(new FileSystemException("FileData or its properties cannot be null."));
                }
                byte[] decryptedContent = EncryptionHelper.DecryptContent(fileData);

                string resolvedPath = ResolvePath(path);

                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                using (var context = identity.Impersonate())
                {
                    string directory = Path.GetDirectoryName(resolvedPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllBytes(resolvedPath, decryptedContent);
                }
                string serverAddress = GetServerAddress();
                Audit.FileCreated(user, path, serverAddress);
                LogSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in CreateFile: {ex.Message}");
                throw new FaultException<FileSystemException>(new FileSystemException($"Error while creating file: {ex.Message}"));
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
                string resolvedPath = ResolvePath(path);

                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                using (var context = identity.Impersonate())
                {
                    Directory.CreateDirectory(resolvedPath);
                }
                string serverAddress = GetServerAddress();
                Audit.FolderCreated(user, path, serverAddress);
                LogSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in CreateFolder: {ex.Message}");
                throw new FaultException<FileSystemException>(new FileSystemException($"Error while creating folder: {ex.Message}"));
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
                string resolvedPath = ResolvePath(path);

                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                using (var context = identity.Impersonate())
                {
                    bool isFile = File.Exists(resolvedPath);
                    bool isDirectory = Directory.Exists(resolvedPath);
                    if (isFile)
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
                        throw new FaultException<FileSystemException>(new FileSystemException($"Path not found: {path}"));
                    }
                }
                LogSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in Delete: {ex.Message}");
                throw new FaultException<FileSystemException>(new FileSystemException($"Error while deleting: {ex.Message}"));
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
                string resolvedSourcePath = ResolvePath(sourcePath);
                string resolvedDestinationPath = ResolvePath(destinationPath);

                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                using (var context = identity.Impersonate())
                {
                    bool isFile = File.Exists(resolvedSourcePath);
                    bool isDirectory = Directory.Exists(resolvedSourcePath);
                    if (isFile)
                    {
                        // Ensure destination directory exists
                        string destDir = Path.GetDirectoryName(resolvedDestinationPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        File.Move(resolvedSourcePath, resolvedDestinationPath);
                        string serverAddress = GetServerAddress();
                        Audit.FileMoved(user, sourcePath, destinationPath, serverAddress);
                    }
                    else if (isDirectory)
                    {
                        // Ensure parent directory of destination exists
                        string destDir = Path.GetDirectoryName(resolvedDestinationPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        Directory.Move(resolvedSourcePath, resolvedDestinationPath);
                        string serverAddress = GetServerAddress();
                        Audit.FolderMoved(user, sourcePath, destinationPath, serverAddress);
                    }
                    else
                    {
                        throw new FaultException<FileSystemException>(new FileSystemException($"Source file or folder not found: {sourcePath}"));
                    }
                }
                LogSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in MoveTo: {ex.Message}");
                throw new FaultException<FileSystemException>(new FileSystemException($"Error while moving: {ex.Message}"));
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
                    throw new FaultException<FileSystemException>(new FileSystemException("Only .txt files are supported."));
                }

                // Resolve the path to ensure it's in the data directory
                string resolvedPath = ResolvePath(path);

                if (!File.Exists(resolvedPath))
                {
                    LogFailure(user, $"File not found: {path}");
                    throw new FaultException<FileSystemException>(new FileSystemException($"File not found: {path}"));
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
                throw new FaultException<FileSystemException>(new FileSystemException($"Error while reading file: {ex.Message}"));
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
                    throw new FaultException<FileSystemException>(new FileSystemException($"Folder not found: {path}"));
                }
                // Only list directories and .txt files
                var entries = Directory.GetFileSystemEntries(resolvedPath)
                    .Where(entry => Directory.Exists(entry) || (File.Exists(entry) && entry.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                LogSuccess(user);
                string serverAddress = GetServerAddress();
                Audit.FolderAccessed(user, path, serverAddress);
                return entries;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in ShowFolderContent: {ex.Message}");
                throw new FaultException<FileSystemException>(new FileSystemException($"Error while showing folder content: {ex.Message}"));
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
                throw new FaultException<FileSystemException>(new FileSystemException("Access to paths outside the data directory is not allowed."));
            }

            return safePath;
        }
    }
}