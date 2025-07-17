using Contracts.Authorization;
using Contracts.Encryption;
using Contracts.Exceptions;
using Contracts.Helpers;
using Contracts.Models;
using Server.Audit;
using Server.Authorization;
using Server.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;

namespace Server.Services
{
    public class FileStorageService : IStorageService
    {
        private readonly string dataDirectory;
        private readonly List<StorageEvent> eventStore = new List<StorageEvent>();
        private int lastEventId = 0;

        public FileStorageService(string dataDirectory)
        {
            this.dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
            Directory.CreateDirectory(this.dataDirectory);
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

        public bool CreateFile(string path, byte[] content) => CreateFile(path, content, true);
        private bool CreateFile(string path, byte[] content, bool appendEvent)
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

            if (appendEvent)
                AppendEvent(new StorageEvent
                {
                    EventType = StorageEventType.CreateFile,
                    SourcePath = path,
                    Content = content
                });

            return true;
        }

        public bool CreateFolder(string path) => CreateFolder(path, true);
        private bool CreateFolder(string path, bool appendEvent)
        {
            string resolvedPath = ResolvePath(path);
            if (Directory.Exists(resolvedPath) || File.Exists(resolvedPath))
                throw new Exception("File or folder already exists at the specified path.");
            Directory.CreateDirectory(resolvedPath);

            if (appendEvent)
                AppendEvent(new StorageEvent
                {
                    EventType = StorageEventType.CreateFolder,
                    SourcePath = path
                });

            return true;
        }

        public bool Delete(string path) => Delete(path, true);
        private bool Delete(string path, bool appendEvent)
        {
            string resolvedPath = ResolvePath(path);
            bool isFile = File.Exists(resolvedPath);
            bool isDirectory = Directory.Exists(resolvedPath);
            if (isFile)
            {
                File.Delete(resolvedPath);
                if (appendEvent)
                    AppendEvent(new StorageEvent
                    {
                        EventType = StorageEventType.Delete,
                        SourcePath = path
                    });
                return true;
            }
            if (isDirectory)
            {
                Directory.Delete(resolvedPath, true);
                if (appendEvent)
                    AppendEvent(new StorageEvent
                    {
                        EventType = StorageEventType.Delete,
                        SourcePath = path
                    });
                return true;
            }
            throw new Exception("File or folder does not exist at the specified path.");
        }

        public bool MoveTo(string sourcePath, string destinationPath) => MoveTo(sourcePath, destinationPath, true);
        private bool MoveTo(string sourcePath, string destinationPath, bool appendEvent)
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
                if (appendEvent)
                    AppendEvent(new StorageEvent
                    {
                        EventType = StorageEventType.Move,
                        SourcePath = sourcePath,
                        DestinationPath = destinationPath
                    });
                return true;
            }
            if (isDirectory)
            {
                string destDir = Path.GetDirectoryName(resolvedDestinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                Directory.Move(resolvedSourcePath, resolvedDestinationPath);
                if (appendEvent)
                    AppendEvent(new StorageEvent
                    {
                        EventType = StorageEventType.Move,
                        SourcePath = sourcePath,
                        DestinationPath = destinationPath
                    });
                return true;
            }
            throw new Exception("Source file or folder does not exist at the specified path.");
        }

        public bool Rename(string sourcePath, string destinationPath) => MoveTo(sourcePath, destinationPath);

        public FileData ReadFile(string path)
        {
            if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only .txt files are supported.");
            string resolvedPath = ResolvePath(path);
            if (!File.Exists(resolvedPath))
                throw new Exception("File does not exist at the specified path.");
            var fileInfo = new FileInfo(resolvedPath);
            string createdBy = "unknown";
            DateTime createdAt = fileInfo.CreationTimeUtc;
            try
            {
                var fileSecurity = fileInfo.GetAccessControl();
                var owner = fileSecurity.GetOwner(typeof(System.Security.Principal.NTAccount));
                if (owner != null)
                    createdBy = owner.ToString();
            }
            catch { }
            return new FileData
            {
                Path = path,
                Content = File.ReadAllBytes(resolvedPath),
                CreatedBy = createdBy,
                CreatedAt = createdAt,
                IsFile = true
            };
        }

        public FileData[] ShowFolderContent(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "." || path == "/" || path == "\\")
                path = "";
            string resolvedPath = ResolvePath(path);
            if (!Directory.Exists(resolvedPath))
                throw new Exception("Folder does not exist at the specified path.");
            var entries = Directory.GetFileSystemEntries(resolvedPath)
                .Where(entry => Directory.Exists(entry) || (File.Exists(entry) && entry.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                .Select(entry =>
                {
                    bool isFile = File.Exists(entry);
                    string createdBy = "unknown";
                    DateTime createdAt = isFile ? File.GetCreationTimeUtc(entry) : Directory.GetCreationTimeUtc(entry);
                    try
                    {
                        if (isFile)
                        {
                            var fileInfo = new FileInfo(entry);
                            var fileSecurity = fileInfo.GetAccessControl();
                            var owner = fileSecurity.GetOwner(typeof(System.Security.Principal.NTAccount));
                            if (owner != null)
                                createdBy = owner.ToString();
                        }
                        else
                        {
                            var dirInfo = new DirectoryInfo(entry);
                            var dirSecurity = dirInfo.GetAccessControl();
                            var owner = dirSecurity.GetOwner(typeof(System.Security.Principal.NTAccount));
                            if (owner != null)
                                createdBy = owner.ToString();
                        }
                    }
                    catch { }
                    return new FileData
                    {
                        Path = entry,
                        Content = isFile ? File.ReadAllBytes(entry) : null,
                        CreatedBy = createdBy,
                        CreatedAt = createdAt,
                        IsFile = isFile
                    };
                })
                .Distinct()
                .ToArray();
            return entries;
        }

        public int GetLastEventId()
        {
            return lastEventId;
        }

        public void SetLastEventId(int eventId)
        {
            if (eventId > lastEventId)
                lastEventId = eventId;
        }

        public StorageEvent[] GetEventsSinceId(int lastId)
        {
            return eventStore.Where(e => e.Id > lastId).OrderBy(e => e.Id).ToArray();
        }

        public void ApplyEvent(StorageEvent ev)
        {
            switch (ev.EventType)
            {
                case StorageEventType.CreateFile:
                    if (!File.Exists(ResolvePath(ev.SourcePath)))
                        CreateFile(ev.SourcePath, ev.Content, appendEvent: false);
                    break;

                case StorageEventType.CreateFolder:
                    if (!Directory.Exists(ResolvePath(ev.SourcePath)))
                        CreateFolder(ev.SourcePath, appendEvent: false);
                    break;

                case StorageEventType.Delete:
                    try { Delete(ev.SourcePath, appendEvent: false); } catch { }
                    break;

                case StorageEventType.Move:
                case StorageEventType.Rename:
                    try
                    {
                        MoveTo(ev.SourcePath, ev.DestinationPath, appendEvent: false);
                    }
                    catch { }
                    break;
            }
        }

        private StorageEvent AppendEvent(StorageEvent ev)
        {
            ev.Id = ++lastEventId;
            ev.Timestamp = DateTime.UtcNow;
            eventStore.Add(ev);
            return ev;
        }
    }
}

