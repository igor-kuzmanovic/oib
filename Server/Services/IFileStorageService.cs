using Contracts.Models;

namespace Server.Services
{
    public interface IFileStorageService
    {
        string[] ShowFolderContent(string path);
        FileData ReadFile(string path);
        bool CreateFile(string path, FileData fileData);
        bool CreateFolder(string path);
        bool Delete(string path);
        bool Rename(string sourcePath, string destinationPath);
        bool MoveTo(string sourcePath, string destinationPath);
    }
}
