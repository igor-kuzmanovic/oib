using Contracts.Models;

namespace Server.Services
{
    public interface IFileStorageService
    {
        string[] ShowFolderContent(string path);
        byte[] ReadFile(string path);
        bool CreateFile(string path, byte[] content);
        bool CreateFolder(string path);
        bool Delete(string path);
        bool Rename(string sourcePath, string destinationPath);
        bool MoveTo(string sourcePath, string destinationPath);
    }
}
