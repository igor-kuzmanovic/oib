using Contracts.Interfaces;
using Contracts.Models;
using Contracts.Helpers;
using System;
using System.ServiceModel;

namespace Server.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class FileWCFService : IFileWCFService
    {
        private static readonly IFileStorageService fileStorageService = new FileStorageService();

        public string[] ShowFolderContent(string path)
        {
            return fileStorageService.ShowFolderContent(path);
        }

        public FileData ReadFile(string path)
        {
            return fileStorageService.ReadFile(path);
        }

        public bool CreateFile(string path, FileData fileData)
        {
            return fileStorageService.CreateFile(path, fileData);
        }

        public bool CreateFolder(string path)
        {
            return fileStorageService.CreateFolder(path);
        }

        public bool Delete(string path)
        {
            return fileStorageService.Delete(path);
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            return fileStorageService.Rename(sourcePath, destinationPath);
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            return fileStorageService.MoveTo(sourcePath, destinationPath);
        }

        public void Dispose()
        {
            
        }
    }
}
