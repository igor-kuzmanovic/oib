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

        private FaultException<FileSecurityException> CreateSecurityFault(string message)
        {
            return new FaultException<FileSecurityException>(
                new FileSecurityException(message),
                new FaultReason(message)
            );
        }

        private FaultException<FileSystemException> CreateSystemFault(string message)
        {
            return new FaultException<FileSystemException>(
                new FileSystemException(message),
                new FaultReason(message)
            );
        }

        private void LogAuthorizationSuccess(string user)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.AuthorizationSuccess(user, GetAction(), GetServerAddress());
            });
        }

        private void LogAuthorizationFailure(string user, string reason)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.AuthorizationFailed(user, GetAction(), reason, GetServerAddress());
            });
        }

        private void LogFileCreated(string user, string path)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.FileCreated(user, path, GetServerAddress());
            });
        }

        private void LogFolderCreated(string user, string path)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.FolderCreated(user, path, GetServerAddress());
            });
        }

        private void LogFileDeleted(string user, string path)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.FileDeleted(user, path, GetServerAddress());
            });
        }

        private void LogFolderDeleted(string user, string path)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.FolderDeleted(user, path, GetServerAddress());
            });
        }

        private void LogFileMoved(string user, string sourcePath, string destinationPath)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.FileMoved(user, sourcePath, destinationPath, GetServerAddress());
            });
        }

        private void LogFolderMoved(string user, string sourcePath, string destinationPath)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.FolderMoved(user, sourcePath, destinationPath, GetServerAddress());
            });
        }

        private void LogFileAccessed(string user, string path)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.FileAccessed(user, path, GetServerAddress());
            });
        }

        private void LogFolderAccessed(string user, string path)
        {
            WindowsIdentity.RunImpersonated(WindowsIdentity.GetCurrent().AccessToken, () =>
            {
                AuditFacade.FolderAccessed(user, path, GetServerAddress());
            });
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

        private string ResolvePath(string relativePath)
        {
            string safePath = Path.GetFullPath(Path.Combine(dataDirectory, relativePath.TrimStart(new char[] { '\\', '/' })));
            if (!safePath.StartsWith(dataDirectory))
            {
                throw CreateSystemFault("Access to paths outside the data directory is not allowed.");
            }
            return safePath;
        }

        public bool CreateFile(string path, FileData fileData)
        {
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;
            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                LogAuthorizationFailure(user, "CreateFile requires Change permission.");
                throw CreateSecurityFault($"User {user} is not authorized for CreateFile.");
            }
            try
            {
                if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    LogAuthorizationFailure(user, "Only .txt files are supported.");
                    throw CreateSystemFault("Only .txt files are supported.");
                }
                if (fileData == null || fileData.Content == null || fileData.InitializationVector == null)
                {
                    LogAuthorizationFailure(user, "CreateFile received null or incomplete FileData.");
                    throw CreateSystemFault("FileData or its properties cannot be null.");
                }
                if (File.Exists(path))
                {
                    LogAuthorizationFailure(user, $"File already exists: {path}");
                    throw CreateSystemFault($"File already exists: {path}");
                }
                byte[] decryptedContent = EncryptionHelper.DecryptContent(fileData);
                string resolvedPath = ResolvePath(path);
                try
                {
                    string directory = Path.GetDirectoryName(resolvedPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllBytes(resolvedPath, decryptedContent);
                }
                catch (Exception ex)
                {
                    LogAuthorizationFailure(user, $"File operation failed: {ex.Message}");
                    throw CreateSecurityFault($"File operation failed: {ex.Message}");
                }
                LogFileCreated(user, path);
                LogAuthorizationSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogAuthorizationFailure(user, $"Exception in CreateFile: {ex.Message}");
                throw CreateSystemFault($"Error while creating file: {ex.Message}");
            }
        }

        public bool CreateFolder(string path)
        {
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                LogAuthorizationFailure(user, "CreateFolder requires Change permission.");
                throw CreateSecurityFault($"User {user} is not authorized for CreateFolder.");
            }
            if (Directory.Exists(path))
            {
                LogAuthorizationFailure(user, $"Folder already exists: {path}");
                throw CreateSystemFault($"Folder already exists: {path}");
            }

            try
            {
                string resolvedPath = ResolvePath(path);

                try
                {
                    Directory.CreateDirectory(resolvedPath);
                }
                catch (Exception ex)
                {
                    LogAuthorizationFailure(user, $"Folder creation failed: {ex.Message}");
                    throw CreateSecurityFault($"Folder creation failed: {ex.Message}");
                }

                LogFolderCreated(user, path);
                LogAuthorizationSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogAuthorizationFailure(user, $"Exception in CreateFolder: {ex.Message}");
                throw CreateSystemFault($"Error while creating folder: {ex.Message}");
            }
        }

        public bool Delete(string path)
        {
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.Delete))
            {
                LogAuthorizationFailure(user, "Delete requires Delete permission.");
                throw CreateSecurityFault($"User {user} is not authorized for Delete.");
            }

            try
            {
                string resolvedPath = ResolvePath(path);

                try
                {
                    bool isFile = File.Exists(resolvedPath);
                    bool isDirectory = Directory.Exists(resolvedPath);

                    if (isFile)
                    {
                        File.Delete(resolvedPath);
                        LogFileDeleted(user, path);
                    }
                    else if (isDirectory)
                    {
                        Directory.Delete(resolvedPath, true);
                        LogFolderDeleted(user, path);
                    }
                    else
                    {
                        LogAuthorizationFailure(user, $"Path not found: {path}");
                        throw CreateSystemFault($"Path not found: {path}");
                    }
                }
                catch (Exception ex)
                {
                    LogAuthorizationFailure(user, $"Delete operation failed: {ex.Message}");
                    throw CreateSecurityFault($"Delete operation failed: {ex.Message}");
                }

                LogAuthorizationSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogAuthorizationFailure(user, $"Exception in Delete: {ex.Message}");
                throw CreateSystemFault($"Error while deleting: {ex.Message}");
            }
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                LogAuthorizationFailure(user, "MoveTo requires Change permission.");
                throw CreateSecurityFault($"User {user} is not authorized for MoveTo.");
            }

            try
            {
                string resolvedSourcePath = ResolvePath(sourcePath);
                string resolvedDestinationPath = ResolvePath(destinationPath);

                try
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
                        LogFileMoved(user, sourcePath, destinationPath);
                    }
                    else if (isDirectory)
                    {
                        string destDir = Path.GetDirectoryName(resolvedDestinationPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        Directory.Move(resolvedSourcePath, resolvedDestinationPath);
                        LogFolderMoved(user, sourcePath, destinationPath);
                    }
                    else
                    {
                        LogAuthorizationFailure(user, $"Source file or folder not found: {sourcePath}");
                        throw CreateSystemFault($"Source file or folder not found: {sourcePath}");
                    }
                }
                catch (Exception ex)
                {
                    LogAuthorizationFailure(user, $"Move operation failed: {ex.Message}");
                    throw CreateSecurityFault($"Move operation failed: {ex.Message}");
                }

                LogAuthorizationSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogAuthorizationFailure(user, $"Exception in MoveTo: {ex.Message}");
                throw CreateSystemFault($"Error while moving: {ex.Message}");
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
                LogAuthorizationFailure(user, "ReadFile requires See permission.");
                throw CreateSecurityFault($"User {user} is not authorized for ReadFile.");
            }

            try
            {
                if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    LogAuthorizationFailure(user, "Only .txt files are supported.");
                    throw CreateSystemFault("Only .txt files are supported.");
                }

                string resolvedPath = ResolvePath(path);

                if (!File.Exists(resolvedPath))
                {
                    LogAuthorizationFailure(user, $"File not found: {path}");
                    throw CreateSystemFault($"File not found: {path}");
                }
                byte[] fileContent = File.ReadAllBytes(resolvedPath);
                FileData encryptedData = EncryptionHelper.EncryptContent(fileContent);

                LogAuthorizationSuccess(user);
                LogFileAccessed(user, path);
                return encryptedData;
            }
            catch (Exception ex)
            {
                LogAuthorizationFailure(user, $"Exception in ReadFile: {ex.Message}");
                throw CreateSystemFault($"Error while reading file: {ex.Message}");
            }
        }

        public string[] ShowFolderContent(string path)
        {
            string user = GetCallingUser();
            var principal = Thread.CurrentPrincipal as Principal;

            if (principal == null || !principal.IsInRole(Permission.See))
            {
                LogAuthorizationFailure(user, "ShowFolderContent requires See permission.");
                throw CreateSecurityFault($"User {user} is not authorized for ShowFolderContent.");
            }

            try
            {
                string resolvedPath = ResolvePath(path);

                if (!Directory.Exists(resolvedPath))
                {
                    LogAuthorizationFailure(user, $"Folder not found: {path}");
                    throw CreateSystemFault($"Folder not found: {path}");
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

                LogAuthorizationSuccess(user);
                LogFolderAccessed(user, path);
                return entries;
            }
            catch (Exception ex)
            {
                LogAuthorizationFailure(user, $"Exception in ShowFolderContent: {ex.Message}");
                throw CreateSystemFault($"Error while showing folder content: {ex.Message}");
            }
        }
    }
}

