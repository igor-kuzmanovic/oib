using Contracts.Interfaces;
using Contracts.Models;
using Contracts.Helpers;
using System;
using System.ServiceModel;
using Contracts.Encryption;

namespace Server.Services
{
    // TODO Fix impersonation
    public class FileWCFService : IFileWCFService
    {
        private static readonly IFileStorageService fileStorageService = new FileStorageService();

        public string[] ShowFolderContent(string path)
        {
            try
            {
                return fileStorageService.ShowFolderContent(path);
            }
            catch (Exception ex)
            {
                string serverAddress = Server.Infrastructure.Configuration.PrimaryServerAddress;
                Server.Audit.AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<Contracts.Exceptions.FileSystemException>(new Contracts.Exceptions.FileSystemException(ex.Message), new FaultReason(ex.Message));
            }
        }

        public FileData ReadFile(string path)
        {
            try
            {
                var decryptedContent = fileStorageService.ReadFile(path);
                return EncryptionHelper.EncryptContent(decryptedContent);
            }
            catch (Exception ex)
            {
                string serverAddress = Server.Infrastructure.Configuration.PrimaryServerAddress;
                Server.Audit.AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<Contracts.Exceptions.FileSystemException>(new Contracts.Exceptions.FileSystemException(ex.Message), new FaultReason(ex.Message));
            }
        }

        public bool CreateFile(string path, FileData fileData)
        {
            var principal = System.Threading.Thread.CurrentPrincipal as Server.Authorization.Principal;
            string user = Contracts.Helpers.SecurityHelper.GetName(System.Security.Principal.WindowsIdentity.GetCurrent());
            string serverAddress = Server.Infrastructure.Configuration.PrimaryServerAddress;
            if (principal == null || !principal.IsInRole(Contracts.Authorization.Permission.Change))
            {
                Server.Audit.AuditFacade.AuthorizationFailed(user, "CreateFile", serverAddress, "Change permission required");
                throw new FaultException<Contracts.Exceptions.FileSecurityException>(new Contracts.Exceptions.FileSecurityException($"User {user} is not authorized for CreateFile."));
            }
            Server.Audit.AuditFacade.AuthorizationSuccess(user, "CreateFile", serverAddress);
            if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                Server.Audit.AuditFacade.ServerError("Only .txt files are supported.", serverAddress);
                throw new FaultException<Contracts.Exceptions.FileSystemException>(new Contracts.Exceptions.FileSystemException("Only .txt files are supported."), new FaultReason("Only .txt files are supported."));
            }
            if (fileData == null || fileData.Content == null || fileData.InitializationVector == null)
            {
                Server.Audit.AuditFacade.ServerError("FileData or its properties cannot be null.", serverAddress);
                throw new FaultException<Contracts.Exceptions.FileSystemException>(new Contracts.Exceptions.FileSystemException("FileData or its properties cannot be null."), new FaultReason("FileData or its properties cannot be null."));
            }
            try
            {
                bool result;
                var windowsIdentity = System.ServiceModel.OperationContext.Current.ServiceSecurityContext.WindowsIdentity;
                //using (windowsIdentity.Impersonate())
                {
                    var decryptedContent = Contracts.Encryption.EncryptionHelper.DecryptContent(fileData);
                    result = fileStorageService.CreateFile(path, decryptedContent);
                }
                Server.Audit.AuditFacade.FileCreated(user, path, serverAddress);
                return result;
            }
            catch (Exception ex)
            {
                Server.Audit.AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<Contracts.Exceptions.FileSystemException>(new Contracts.Exceptions.FileSystemException(ex.Message), new FaultReason(ex.Message));
            }
        }

        public bool CreateFolder(string path)
        {
            var principal = System.Threading.Thread.CurrentPrincipal as Server.Authorization.Principal;
            string user = Contracts.Helpers.SecurityHelper.GetName(System.Security.Principal.WindowsIdentity.GetCurrent());
            string serverAddress = Server.Infrastructure.Configuration.PrimaryServerAddress;
            if (principal == null || !principal.IsInRole(Contracts.Authorization.Permission.Change))
            {
                Server.Audit.AuditFacade.AuthorizationFailed(user, "CreateFolder", serverAddress, "Change permission required");
                throw new FaultException<Contracts.Exceptions.FileSecurityException>(new Contracts.Exceptions.FileSecurityException($"User {user} is not authorized for CreateFolder."));
            }
            Server.Audit.AuditFacade.AuthorizationSuccess(user, "CreateFolder", serverAddress);
            try
            {
                bool result;
                var windowsIdentity = System.ServiceModel.OperationContext.Current.ServiceSecurityContext.WindowsIdentity;
                //using (windowsIdentity.Impersonate())
                {
                    result = fileStorageService.CreateFolder(path);
                }
                Server.Audit.AuditFacade.FolderCreated(user, path, serverAddress);
                return result;
            }
            catch (Exception ex)
            {
                Server.Audit.AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<Contracts.Exceptions.FileSystemException>(new Contracts.Exceptions.FileSystemException(ex.Message), new FaultReason(ex.Message));
            }
        }

        public bool Delete(string path)
        {
            var principal = System.Threading.Thread.CurrentPrincipal as Server.Authorization.Principal;
            string user = Contracts.Helpers.SecurityHelper.GetName(System.Security.Principal.WindowsIdentity.GetCurrent());
            string serverAddress = Server.Infrastructure.Configuration.PrimaryServerAddress;
            if (principal == null || !principal.IsInRole(Contracts.Authorization.Permission.Delete))
            {
                Server.Audit.AuditFacade.AuthorizationFailed(user, "Delete", serverAddress, "Delete permission required");
                throw new FaultException<Contracts.Exceptions.FileSecurityException>(new Contracts.Exceptions.FileSecurityException($"User {user} is not authorized for Delete."));
            }
            Server.Audit.AuditFacade.AuthorizationSuccess(user, "Delete", serverAddress);
            try
            {
                bool result;
                var windowsIdentity = System.ServiceModel.OperationContext.Current.ServiceSecurityContext.WindowsIdentity;
                //using (windowsIdentity.Impersonate())
                {
                    result = fileStorageService.Delete(path);
                }
                Server.Audit.AuditFacade.FileDeleted(user, path, serverAddress);
                return result;
            }
            catch (Exception ex)
            {
                Server.Audit.AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<Contracts.Exceptions.FileSystemException>(new Contracts.Exceptions.FileSystemException(ex.Message), new FaultReason(ex.Message));
            }
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            try
            {
                return MoveTo(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                string serverAddress = Server.Infrastructure.Configuration.PrimaryServerAddress;
                Server.Audit.AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<Contracts.Exceptions.FileSystemException>(new Contracts.Exceptions.FileSystemException(ex.Message), new FaultReason(ex.Message));
            }
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            var principal = System.Threading.Thread.CurrentPrincipal as Server.Authorization.Principal;
            string user = Contracts.Helpers.SecurityHelper.GetName(System.Security.Principal.WindowsIdentity.GetCurrent());
            string serverAddress = Server.Infrastructure.Configuration.PrimaryServerAddress;
            if (principal == null || !principal.IsInRole(Contracts.Authorization.Permission.Change))
            {
                Server.Audit.AuditFacade.AuthorizationFailed(user, "MoveTo", serverAddress, "Change permission required");
                throw new FaultException<Contracts.Exceptions.FileSecurityException>(new Contracts.Exceptions.FileSecurityException($"User {user} is not authorized for MoveTo."));
            }
            Server.Audit.AuditFacade.AuthorizationSuccess(user, "MoveTo", serverAddress);
            try
            {
                bool result;
                var windowsIdentity = System.ServiceModel.OperationContext.Current.ServiceSecurityContext.WindowsIdentity;
                //using (windowsIdentity.Impersonate())
                {
                    result = fileStorageService.MoveTo(sourcePath, destinationPath);
                }
                Server.Audit.AuditFacade.FileMoved(user, sourcePath, destinationPath, serverAddress);
                return result;
            }
            catch (Exception ex)
            {
                Server.Audit.AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<Contracts.Exceptions.FileSystemException>(new Contracts.Exceptions.FileSystemException(ex.Message), new FaultReason(ex.Message));
            }
        }

        public void Dispose()
        {

        }
    }
}
