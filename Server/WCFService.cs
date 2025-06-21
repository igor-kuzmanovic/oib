using Contracts;
using System.Security.Permissions;
using System.ServiceModel;

namespace Server
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class WCFService : IWCFService
    {
        private readonly FileService _fileService = new FileService();

        public string[] ShowFolderContent(string path)
        {
            return _fileService.ShowFolderContent(path);
        }

        public FileData ReadFile(string path)
        {
            return _fileService.ReadFile(path);
        }

        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public bool CreateFile(string path, FileData fileData)
        {
            _fileService.CreateFile(path, fileData);
            return true;
        }

        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public bool CreateFolder(string path)
        {
            _fileService.CreateFolder(path);
            return true;
        }

        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public bool Delete(string path)
        {
            _fileService.Delete(path);
            return true;
        }

        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public bool Rename(string sourcePath, string destinationPath)
        {
            _fileService.Rename(sourcePath, destinationPath);
            return true;
        }

        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public bool MoveTo(string sourcePath, string destinationPath)
        {
            _fileService.MoveTo(sourcePath, destinationPath);
            return true;
        }
    }
}