using Contracts.Models;

namespace Client.Services
{
    public interface IFileServiceClient
    {
        FileData[] ShowFolderContent(string path);
        FileData ReadFile(string path);
        bool CreateFile(string path, FileData fileData);
        bool CreateFolder(string path);
        bool Delete(string path);
        bool Rename(string sourcePath, string destinationPath);
        bool MoveTo(string sourcePath, string destinationPath);
        string GetCurrentServerAddress();
    }
}
