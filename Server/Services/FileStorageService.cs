using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using Contracts.Models;
using Contracts.Exceptions;
using Contracts.Encryption;
using Contracts.Authorization;
using Contracts.Helpers;
using Server.Authorization;
using Server.Infrastructure;
using Server.Audit;

namespace Server.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string dataDirectory;

        public FileStorageService()
        {
            dataDirectory = Configuration.DataDirectory;
        }

        private string GetCurrentUser()
        {
            Principal principal = Thread.CurrentPrincipal as Principal;
            if (principal?.Identity is WindowsIdentity winIdentity)
            {
                return SecurityHelper.GetName(winIdentity);
            }

            return SecurityHelper.ParseName(principal?.Identity?.Name ?? "Unknown");
        }

        private string GetCallingUser()
        {
            try
            {
                return SecurityHelper.GetName(OperationContext.Current);
            }
            catch
            {
                return GetCurrentUser();
            }
        }

        private string GetServerAddress()
        {
            return OperationContext.Current?.IncomingMessageHeaders?.Action ?? "UnknownServer";
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
                AuditFacade.AuthorizationSuccess(user, GetAction(), serverAddress);
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
                AuditFacade.AuthorizationFailed(user, GetAction(), reason, serverAddress);
            }
            catch (Exception e)
            {
                Console.WriteLine("Audit failure log failed: " + e.Message);
            }
        }

        public bool CreateFile(string path, FileData fileData)
        {
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                LogFailure(user, "CreateFile requires Change permission.");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for CreateFile."));
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

                var identity = GetCallingUserIdentity();
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
                AuditFacade.FileCreated(user, path, serverAddress);
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
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                LogFailure(user, "CreateFolder requires Change permission.");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for CreateFolder."));
            }

            try
            {
                string resolvedPath = ResolvePath(path);

                var identity = GetCallingUserIdentity();
                using (var context = identity.Impersonate())
                {
                    Directory.CreateDirectory(resolvedPath);
                }

                string serverAddress = GetServerAddress();
                AuditFacade.FolderCreated(user, path, serverAddress);
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
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.Delete))
            {
                LogFailure(user, "Delete requires Delete permission.");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for Delete."));
            }

            try
            {
                string resolvedPath = ResolvePath(path);

                var identity = GetCallingUserIdentity();
                using (var context = identity.Impersonate())
                {
                    bool isFile = File.Exists(resolvedPath);
                    bool isDirectory = Directory.Exists(resolvedPath);

                    if (isFile)
                    {
                        File.Delete(resolvedPath);
                        string serverAddress = GetServerAddress();
                        AuditFacade.FileDeleted(user, path, serverAddress);
                    }
                    else if (isDirectory)
                    {
                        Directory.Delete(resolvedPath, true);
                        string serverAddress = GetServerAddress();
                        AuditFacade.FolderDeleted(user, path, serverAddress);
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
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                LogFailure(user, "MoveTo requires Change permission.");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for MoveTo."));
            }

            try
            {
                string resolvedSourcePath = ResolvePath(sourcePath);
                string resolvedDestinationPath = ResolvePath(destinationPath);

                var identity = GetCallingUserIdentity();
                using (var context = identity.Impersonate())
                {
                    bool isFile = File.Exists(resolvedSourcePath);
                    bool isDirectory = Directory.Exists(resolvedSourcePath);

                    if (isFile)
                    {
                        string destDir = Path.GetDirectoryName(resolvedDestinationPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        File.Move(resolvedSourcePath, resolvedDestinationPath);
                        string serverAddress = GetServerAddress();
                        AuditFacade.FileMoved(user, sourcePath, destinationPath, serverAddress);
                    }
                    else if (isDirectory)
                    {
                        string destDir = Path.GetDirectoryName(resolvedDestinationPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        Directory.Move(resolvedSourcePath, resolvedDestinationPath);
                        string serverAddress = GetServerAddress();
                        AuditFacade.FolderMoved(user, sourcePath, destinationPath, serverAddress);
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
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.See))
            {
                LogFailure(user, "ReadFile requires See permission.");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for ReadFile."));
            }

            try
            {
                if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    LogFailure(user, "Only .txt files are supported.");
                    throw new FaultException<FileSystemException>(new FileSystemException("Only .txt files are supported."));
                }

                string resolvedPath = ResolvePath(path);

                if (!File.Exists(resolvedPath))
                {
                    LogFailure(user, $"File not found: {path}");
                    throw new FaultException<FileSystemException>(new FileSystemException($"File not found: {path}"));
                }
                byte[] fileContent = File.ReadAllBytes(resolvedPath);
                FileData encryptedData = EncryptionHelper.EncryptContent(fileContent);

                LogSuccess(user);
                string serverAddress = GetServerAddress();
                AuditFacade.FileAccessed(user, path, serverAddress);

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
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.See))
            {
                LogFailure(user, "ShowFolderContent requires See permission.");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for ShowFolderContent."));
            }

            try
            {
                string resolvedPath = ResolvePath(path);

                if (!Directory.Exists(resolvedPath))
                {
                    LogFailure(user, $"Folder not found: {path}");
                    throw new FaultException<FileSystemException>(new FileSystemException($"Folder not found: {path}"));
                }

                var entries = Directory.GetFileSystemEntries(resolvedPath)
                    .Where(entry => Directory.Exists(entry) || (File.Exists(entry) && entry.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                    .Select(entry =>
                    {
                        if (entry.StartsWith(dataDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            return entry.Substring(dataDirectory.Length).TrimStart('\\', '/');
                        }
                        return entry;
                    })
                    .ToArray();

                LogSuccess(user);
                string serverAddress = GetServerAddress();
                AuditFacade.FolderAccessed(user, path, serverAddress);
                return entries;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in ShowFolderContent: {ex.Message}");
                throw new FaultException<FileSystemException>(new FileSystemException($"Error while showing folder content: {ex.Message}"));
            }
        }

        private string ResolvePath(string relativePath)
        {
            string safePath = Path.GetFullPath(Path.Combine(dataDirectory, relativePath.TrimStart('\\', '/')));

            if (!safePath.StartsWith(dataDirectory))
            {
                throw new FaultException<FileSystemException>(new FileSystemException("Access to paths outside the data directory is not allowed."));
            }

            return safePath;
        }

        private WindowsIdentity GetCallingUserIdentity()
        {
            try
            {
                if (OperationContext.Current?.ServiceSecurityContext?.WindowsIdentity != null)
                {
                    return OperationContext.Current.ServiceSecurityContext.WindowsIdentity;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get calling user identity: {ex.Message}");
            }
            
            return WindowsIdentity.GetCurrent();
        }
    }
}

