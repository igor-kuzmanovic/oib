using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using Contracts.Models;
using Contracts.Exceptions;
using Contracts.Encryption;
using Contracts.Authorization;
using Contracts.Helpers;
using Server.Authorization;
using Server.Infrastructure;
using Server.Audit;

namespace Server.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string dataDirectory;

        public FileStorageService()
        {
            dataDirectory = Configuration.DataDirectory;
        }

        private string ResolvePath(string relativePath)
        {
            string safePath = Path.GetFullPath(Path.Combine(dataDirectory, relativePath.TrimStart(new char[] { '\\', '/' })));
            if (!safePath.StartsWith(dataDirectory))
            {
                throw new Exception("Access to paths outside the data directory is not allowed.");
            }
            return safePath;
        }

        public bool CreateFile(string path, byte[] content)
        {
            if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only .txt files are supported.");
            if (content == null)
                throw new Exception("Content cannot be null.");
            string resolvedPath = ResolvePath(path);
            if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
                throw new Exception("File or folder already exists at the specified path.");
            string directory = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(resolvedPath, content);
            return true;
        }

        public bool CreateFolder(string path)
        {
            string resolvedPath = ResolvePath(path);
            if (Directory.Exists(resolvedPath) || File.Exists(resolvedPath))
                throw new Exception("File or folder already exists at the specified path.");
            Directory.CreateDirectory(resolvedPath);
            return true;
        }

        public bool Delete(string path)
        {
            string resolvedPath = ResolvePath(path);
            bool isFile = File.Exists(resolvedPath);
            bool isDirectory = Directory.Exists(resolvedPath);
            if (isFile)
            {
                File.Delete(resolvedPath);
                return true;
            }
            if (isDirectory)
            {
                Directory.Delete(resolvedPath, true);
                return true;
            }
            throw new Exception("File or folder does not exist at the specified path.");
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            string resolvedSourcePath = ResolvePath(sourcePath);
            string resolvedDestinationPath = ResolvePath(destinationPath);
            if (File.Exists(resolvedDestinationPath) || Directory.Exists(resolvedDestinationPath))
                throw new Exception("File or folder already exists at the destination path.");
            bool isFile = File.Exists(resolvedSourcePath);
            bool isDirectory = Directory.Exists(resolvedSourcePath);
            if (isFile)
            {
                string destDir = Path.GetDirectoryName(resolvedDestinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                File.Move(resolvedSourcePath, resolvedDestinationPath);
                return true;
            }
            if (isDirectory)
            {
                string destDir = Path.GetDirectoryName(resolvedDestinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                Directory.Move(resolvedSourcePath, resolvedDestinationPath);
                return true;
            }
            throw new Exception("Source file or folder does not exist at the specified path.");
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            return MoveTo(sourcePath, destinationPath);
        }

        public byte[] ReadFile(string path)
        {
            if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only .txt files are supported.");
            string resolvedPath = ResolvePath(path);
            if (!File.Exists(resolvedPath))
                throw new Exception("File does not exist at the specified path.");
            return File.ReadAllBytes(resolvedPath);
        }

        public string[] ShowFolderContent(string path)
        {
            string resolvedPath = ResolvePath(path);
            if (!Directory.Exists(resolvedPath))
                throw new Exception("Folder does not exist at the specified path.");
            var entries = Directory.GetFileSystemEntries(resolvedPath)
                .Where(entry => Directory.Exists(entry) || (File.Exists(entry) && entry.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                .Select(entry =>
                {
                    if (entry.StartsWith(dataDirectory, StringComparison.OrdinalIgnoreCase))
                        return entry.Substring(dataDirectory.Length).TrimStart(new char[] { '\\', '/' });
                    return entry;
                })
                .ToArray();
            return entries;
        }
    }
}

