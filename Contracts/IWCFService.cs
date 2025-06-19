using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.ServiceModel;

namespace Contracts
{
    [ServiceContract]
    public interface IWCFService
    {
        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        string[] ShowFolderContent(string path);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        FileData ReadFile(string path);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        bool CreateFolder(string path);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        bool CreateFile(string path, FileData fileData);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        bool Delete(string path);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        bool Rename(string sourcePath, string destinationPath);

        [OperationContract]
        [FaultContract(typeof(SecurityException))]
        bool MoveTo(string sourcePath, string destinationPath);
    }
}
