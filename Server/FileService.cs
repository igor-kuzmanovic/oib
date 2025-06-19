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

        private string GetAction()
        {
            return OperationContext.Current?.IncomingMessageHeaders?.Action ?? "UnknownAction";
        }

        private void LogSuccess(string user)
        {
            try
            {
                Audit.AuthorizationSuccess(user, GetAction());
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
                Audit.AuthorizationFailed(user, GetAction(), reason);
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
                bool result = ImpersonationHelper.ExecuteAsEditor(
                    ImpersonationConfig.Domain,
                    ImpersonationConfig.Username,
                    ImpersonationConfig.Password,
                    action);

                if (!result)
                {
                    LogFailure(user, $"Failed to impersonate Editor user for {operationName}");
                    throw new FaultException($"Failed to impersonate Editor user for {operationName}");
                }
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Impersonation error in {operationName}: {ex.Message}");
                throw new FaultException($"Error during {operationName} operation.");
            }
        }
        public bool CreateFile(string path, FileData fileData)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("Change"))
            {
                LogFailure(user, "CreateFile requires Change permission.");
                throw new FaultException($"User {user} is not authorized for CreateFile.");
            }
            try
            {
                // Use the encryption helper to decrypt content received through secure transmission
                byte[] decryptedContent = EncryptionHelper.DecryptContent(fileData);

                bool result = PerformAsEditor(() =>
                {
                    File.WriteAllBytes(path, decryptedContent);
                    Audit.FileCreated(user, path);
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
                throw new FaultException($"User {user} is not authorized for CreateFolder.");
            }

            try
            {
                bool result = PerformAsEditor(() =>
                {
                    Directory.CreateDirectory(path);
                    Audit.FolderCreated(user, path);
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
                throw new FaultException($"User {user} is not authorized for Delete.");
            }

            try
            {
                bool result = PerformAsEditor(() =>
                {
                    bool isFile = File.Exists(path);
                    bool isDirectory = Directory.Exists(path);

                    if (isFile)
                    {
                        File.Delete(path);
                        Audit.FileDeleted(user, path);
                    }
                    else if (isDirectory)
                    {
                        Directory.Delete(path, true);
                        Audit.FolderDeleted(user, path);
                    }
                    else
                    {
                        throw new FileNotFoundException("File or folder not found.");
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
                throw new FaultException($"User {user} is not authorized for MoveTo.");
            }

            try
            {
                bool result = PerformAsEditor(() =>
                {
                    bool isFile = File.Exists(sourcePath);
                    bool isDirectory = Directory.Exists(sourcePath);

                    if (isFile)
                    {
                        File.Move(sourcePath, destinationPath);
                        Audit.FileMoved(user, sourcePath, destinationPath);
                    }
                    else if (isDirectory)
                    {
                        Directory.Move(sourcePath, destinationPath);
                        Audit.FolderMoved(user, sourcePath, destinationPath);
                    }
                    else
                    {
                        throw new FileNotFoundException("Source path does not exist.");
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
                throw new FaultException($"User {user} is not authorized for ReadFile.");
            }
            try
            {
                if (!File.Exists(path))
                {
                    LogFailure(user, $"File not found: {path}");
                    throw new FileNotFoundException($"File not found: {path}");
                }

                byte[] fileContent = File.ReadAllBytes(path);

                // Use the encryption helper to encrypt content for secure transmission
                FileData encryptedData = EncryptionHelper.EncryptContent(fileContent);

                LogSuccess(user);
                Audit.FileAccessed(user, path);

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
                throw new FaultException($"User {user} is not authorized for ShowFolderContent.");
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    LogFailure(user, $"Folder not found: {path}");
                    throw new DirectoryNotFoundException($"Folder not found: {path}");
                }

                string[] entries = Directory.GetFileSystemEntries(path);
                LogSuccess(user);
                Audit.FolderAccessed(user, path);
                return entries;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in ShowFolderContent: {ex.Message}");
                throw new FaultException("Error while listing folder content.");
            }
        }
    }
}