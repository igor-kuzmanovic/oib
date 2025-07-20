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
        private static readonly IStorageService storageService = StorageServiceProvider.Instance;

        public FileData[] ShowFolderContent(string path)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(principal.Identity);
            if (principal == null || !principal.IsInRole(Permission.See))
            {
                AuditFacade.AuthorizationFailed(user, "ShowFolderContent", "See permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for ShowFolderContent."));
            }
            AuditFacade.AuthorizationSuccess(user, "ShowFolderContent");
            try
            {
                var result = storageService.ShowFolderContent(path);
                AuditFacade.FolderAccessed(user, path);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'ShowFolderContent' error: {ex}");
                AuditFacade.ServerError($"[FileWCFService] 'ShowFolderContent' error: {ex}");
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public FileData ReadFile(string path)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(principal.Identity);
            if (principal == null || !principal.IsInRole(Permission.See))
            {
                AuditFacade.AuthorizationFailed(user, "ReadFile", "See permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for ReadFile."));
            }
            AuditFacade.AuthorizationSuccess(user, "ReadFile");
            try
            {
                var fileData = storageService.ReadFile(path);
                var result = EncryptionHelper.EncryptContent(fileData);
                AuditFacade.FileAccessed(user, path);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'ReadFile' error: {ex}");
                AuditFacade.ServerError($"[FileWCFService] 'ReadFile' error: {ex}");
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool CreateFile(string path, FileData fileData)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(principal.Identity);
            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                AuditFacade.AuthorizationFailed(user, "CreateFile", "Change permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for CreateFile."));
            }
            AuditFacade.AuthorizationSuccess(user, "CreateFile");
            try
            {
                var decryptedFileData = EncryptionHelper.DecryptContent(fileData);
                bool result;
                using (WindowsImpersonationContext ctx = (principal.Identity as WindowsIdentity).Impersonate())
                {
                    DisplayIdentityInformation();
                    result = storageService.CreateFile(path, decryptedFileData.Content);
                }
                AuditFacade.FileCreated(user, path);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'CreateFile' error: {ex}");
                AuditFacade.ServerError($"[FileWCFService] 'CreateFile' error: {ex}");
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool CreateFolder(string path)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(principal.Identity);
            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                AuditFacade.AuthorizationFailed(user, "CreateFolder", "Change permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for CreateFolder."));
            }
            AuditFacade.AuthorizationSuccess(user, "CreateFolder");
            try
            {
                bool result;
                using (WindowsImpersonationContext ctx = (principal.Identity as WindowsIdentity).Impersonate())
                {
                    DisplayIdentityInformation();
                    result = storageService.CreateFolder(path);
                }
                AuditFacade.FolderCreated(user, path);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'CreateFolder' error: {ex}");
                AuditFacade.ServerError($"[FileWCFService] 'CreateFolder' error: {ex}");
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool Delete(string path)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(principal.Identity);
            if (principal == null || !principal.IsInRole(Permission.Delete))
            {
                AuditFacade.AuthorizationFailed(user, "Delete", "Delete permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for Delete."));
            }
            AuditFacade.AuthorizationSuccess(user, "Delete");
            try
            {
                bool result;
                using (WindowsImpersonationContext ctx = (principal.Identity as WindowsIdentity).Impersonate())
                {
                    DisplayIdentityInformation();
                    result = storageService.Delete(path);
                }
                if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    AuditFacade.FileDeleted(user, path);
                }
                else
                {
                    AuditFacade.FolderDeleted(user, path);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'Delete' error: {ex}");
                AuditFacade.ServerError($"[FileWCFService] 'Delete' error: {ex}");
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(principal.Identity);
            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                AuditFacade.AuthorizationFailed(user, "Rename", "Change permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for Rename."));
            }
            AuditFacade.AuthorizationSuccess(user, "Rename");
            try
            {
                bool result;
                using (WindowsImpersonationContext ctx = (principal.Identity as WindowsIdentity).Impersonate())
                {
                    DisplayIdentityInformation();
                    result = storageService.Rename(sourcePath, destinationPath);
                }
                if (sourcePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && destinationPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    AuditFacade.FileRenamed(user, sourcePath, destinationPath);
                }
                else
                {
                    AuditFacade.FolderRenamed(user, sourcePath, destinationPath);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'Rename' error: {ex}");
                AuditFacade.ServerError($"[FileWCFService] 'Rename' error: {ex}");
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            var principal = Thread.CurrentPrincipal as Principal;
            string user = SecurityHelper.GetName(principal.Identity);
            if (principal == null || !principal.IsInRole(Permission.Change))
            {
                AuditFacade.AuthorizationFailed(user, "MoveTo", "Change permission required");
                throw new FaultException<FileSecurityException>(new FileSecurityException($"User {user} is not authorized for MoveTo."));
            }
            AuditFacade.AuthorizationSuccess(user, "MoveTo");
            try
            {
                bool result;
                using (WindowsImpersonationContext ctx = (principal.Identity as WindowsIdentity).Impersonate())
                {
                    DisplayIdentityInformation();
                    result = storageService.MoveTo(sourcePath, destinationPath);
                }
                if (sourcePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && destinationPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    AuditFacade.FileMoved(user, sourcePath, destinationPath);
                }
                else
                {
                    AuditFacade.FolderMoved(user, sourcePath, destinationPath);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWCFService] 'MoveTo' error: {ex}");
                AuditFacade.ServerError($"[FileWCFService] 'MoveTo' error: {ex}");
                throw new FaultException<FileSystemException>(new FileSystemException(ex.Message));
            }
        }

        public void Dispose()
        {

        }

        static void DisplayIdentityInformation()
        {
            var identity = (Thread.CurrentPrincipal as Principal).Identity as WindowsIdentity;
            Console.WriteLine($"[Impersonated Identity] {identity.Name}");
            Console.WriteLine($"[Impersonation Level] {identity.ImpersonationLevel}");
        }
    }
}
