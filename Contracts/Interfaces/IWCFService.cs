using System;
using System.ServiceModel;
using Contracts.Exceptions;
using Contracts.Models;

namespace Contracts.Interfaces
{
    [ServiceContract]
    public interface IWCFService : IDisposable
    {
        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        [FaultContract(typeof(FileSystemException))]
        string[] ShowFolderContent(string path);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        [FaultContract(typeof(FileSystemException))]
        FileData ReadFile(string path);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        [FaultContract(typeof(FileSystemException))]
        bool CreateFolder(string path);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        [FaultContract(typeof(FileSystemException))]
        bool CreateFile(string path, FileData fileData);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        [FaultContract(typeof(FileSystemException))]
        bool Delete(string path);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        [FaultContract(typeof(FileSystemException))]
        bool Rename(string sourcePath, string destinationPath);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        [FaultContract(typeof(FileSystemException))]
        bool MoveTo(string sourcePath, string destinationPath);
    }
}
