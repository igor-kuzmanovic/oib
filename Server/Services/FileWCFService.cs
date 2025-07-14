using Contracts.Authorization;
using Contracts.Encryption;
using Contracts.Exceptions;
using Contracts.Helpers;
using Contracts.Interfaces;
using Contracts.Models;
using Server.Audit;
using Server.Authorization;
using Server.Infrastructure;
using System;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;

namespace Server.Services
{
    public class FileWCFService : IFileWCFService
    {
        private static readonly IStorageService storageService = new MemoryStorageService();

        public string[] ShowFolderContent(string path)
        {
            string serverAddress = Configuration.PrimaryServerAddress;
            try
            {
                return storageService.ShowFolderContent(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'ShowFolderContent' error: {ex}");
                AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public FileData ReadFile(string path)
        {
            string serverAddress = Configuration.PrimaryServerAddress;
            try
            {
                var decryptedContent = storageService.ReadFile(path);
                return EncryptionHelper.EncryptContent(decryptedContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'ReadFile' error: {ex}");
                AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool CreateFile(string path, FileData fileData)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(OperationContext.Current);
            string serverAddress = Configuration.PrimaryServerAddress;
            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                AuditFacade.AuthorizationFailed(user, "CreateFile", serverAddress, "Change permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for CreateFile."));
            }
            AuditFacade.AuthorizationSuccess(user, "CreateFile", serverAddress);
            try
            {
                var decryptedContent = EncryptionHelper.DecryptContent(fileData);
                bool result;
                using ((principal.Identity as WindowsIdentity).Impersonate())
                {
                    result = storageService.CreateFile(path, decryptedContent);
                }
                AuditFacade.FileCreated(user, path, serverAddress);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'CreateFile' error: {ex}");
                AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool CreateFolder(string path)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(OperationContext.Current);
            string serverAddress = Configuration.PrimaryServerAddress;
            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                AuditFacade.AuthorizationFailed(user, "CreateFolder", serverAddress, "Change permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for CreateFolder."));
            }
            AuditFacade.AuthorizationSuccess(user, "CreateFolder", serverAddress);
            try
            {
                bool result;
                using ((principal.Identity as WindowsIdentity).Impersonate())
                {
                    result = storageService.CreateFolder(path);
                }
                AuditFacade.FolderCreated(user, path, serverAddress);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'CreateFolder' error: {ex}");
                AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool Delete(string path)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(OperationContext.Current);
            string serverAddress = Configuration.PrimaryServerAddress;
            if (principal == null || !principal.IsInRole(Permission.Delete))
            {
                AuditFacade.AuthorizationFailed(user, "Delete", serverAddress, "Delete permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for Delete."));
            }
            AuditFacade.AuthorizationSuccess(user, "Delete", serverAddress);
            try
            {
                bool result;
                using ((principal.Identity as WindowsIdentity).Impersonate())
                {
                    result = storageService.Delete(path);
                }
                AuditFacade.FileDeleted(user, path, serverAddress);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'Delete' error: {ex}");
                AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            string serverAddress = Configuration.PrimaryServerAddress;
            try
            {
                return MoveTo(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'Rename' error: {ex}");
                AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(OperationContext.Current);
            string serverAddress = Configuration.PrimaryServerAddress;
            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                AuditFacade.AuthorizationFailed(user, "MoveTo", serverAddress, "Change permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for MoveTo."));
            }
            AuditFacade.AuthorizationSuccess(user, "MoveTo", serverAddress);
            try
            {
                bool result;
                using ((principal.Identity as WindowsIdentity).Impersonate())
                {
                    result = storageService.MoveTo(sourcePath, destinationPath);
                }
                AuditFacade.FileMoved(user, sourcePath, destinationPath, serverAddress);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'MoveTo' error: {ex}");
                AuditFacade.ServerError(ex.Message, serverAddress);
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public void Dispose()
        {

        }
    }
}
