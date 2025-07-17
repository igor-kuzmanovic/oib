using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Security.Principal;
using Contracts.Models;

namespace Server.Services
{
    public class MemoryStorageService : IStorageService
    {
        private readonly Dictionary<string, FileData> entries = new Dictionary<string, FileData>(StringComparer.OrdinalIgnoreCase);
        private readonly List<StorageEvent> eventStore = new List<StorageEvent>();
        private int lastEventId = 0;

        public MemoryStorageService()
        {
            entries[""] = new FileData
            {
                Content = null,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                IsFile = false
            };
        }

        private string NormalizePath(string path)
        {
            return path.TrimStart('/', '\\');
        }

        public bool CreateFile(string path, byte[] content) => CreateFile(path, content, true);
        private bool CreateFile(string path, byte[] content, bool appendEvent)
        {
            path = NormalizePath(path);
            if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only .txt files are supported.");
            if (content == null)
                throw new Exception("Content cannot be null.");
            if (entries.ContainsKey(path))
                throw new Exception("File or folder already exists at the specified path.");
            string directory = System.IO.Path.GetDirectoryName(path) ?? "";
            if (!entries.ContainsKey(directory) || entries[directory].IsFile)
                throw new Exception("Parent folder does not exist.");
            string createdBy = GetCurrentUser();
            entries[path] = new FileData
            {
                Path = path,
                Content = content,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsFile = true
            };

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
            path = NormalizePath(path);
            if (entries.ContainsKey(path))
                throw new Exception("File or folder already exists at the specified path.");
            string parent = System.IO.Path.GetDirectoryName(path) ?? "";
            if (!entries.ContainsKey(parent) || entries[parent].IsFile)
                throw new Exception("Parent folder does not exist.");
            string createdBy = GetCurrentUser();
            entries[path] = new FileData
            {
                Path = path,
                Content = null,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsFile = false
            };

            if (appendEvent)
                AppendEvent(new StorageEvent
                {
                    EventType = StorageEventType.CreateFolder,
                    SourcePath = path
                });

            return true;
        }

        private string GetCurrentUser()
        {
            var context = OperationContext.Current;
            if (context == null || context.ServiceSecurityContext == null)
                throw new InvalidOperationException("OperationContext or ServiceSecurityContext is not available.");
            var winIdentity = context.ServiceSecurityContext.WindowsIdentity;
            if (winIdentity == null)
                throw new InvalidOperationException("WindowsIdentity is not available.");
            var impLevel = winIdentity.ImpersonationLevel;
            if (impLevel != TokenImpersonationLevel.Impersonation && impLevel != TokenImpersonationLevel.Delegation)
                throw new InvalidOperationException($"Impersonation level is not sufficient: {impLevel}");
            return winIdentity.Name;
        }

        public bool Delete(string path) => Delete(path, true);
        private bool Delete(string path, bool appendEvent)
        {
            path = NormalizePath(path);
            if (!entries.ContainsKey(path))
                throw new Exception("File or folder does not exist at the specified path.");
            var entry = entries[path];
            if (entry.IsFile)
            {
                entries.Remove(path);
                if (appendEvent)
                    AppendEvent(new StorageEvent
                    {
                        EventType = StorageEventType.Delete,
                        SourcePath = path
                    });
                return true;
            }
            var toRemove = entries.Keys.Where(k => k == path
                || k.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase)
                || k.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var k in toRemove)
                entries.Remove(k);

            if (appendEvent)
                AppendEvent(new StorageEvent
                {
                    EventType = StorageEventType.Delete,
                    SourcePath = path
                });

            return true;
        }

        public bool MoveTo(string sourcePath, string destinationPath) => MoveTo(sourcePath, destinationPath, true);
        private bool MoveTo(string sourcePath, string destinationPath, bool appendEvent)
        {
            sourcePath = NormalizePath(sourcePath);
            destinationPath = NormalizePath(destinationPath);
            if (entries.ContainsKey(destinationPath))
                throw new Exception("File or folder already exists at the destination path.");
            if (!entries.ContainsKey(sourcePath))
                throw new Exception("Source file or folder does not exist at the specified path.");
            string destDir = System.IO.Path.GetDirectoryName(destinationPath) ?? "";
            if (!entries.ContainsKey(destDir) || entries[destDir].IsFile)
                throw new Exception("Parent folder does not exist.");
            var entry = entries[sourcePath];
            if (entry.IsFile)
            {
                entries[destinationPath] = new FileData
                {
                    Path = destinationPath,
                    Content = entry.Content,
                    CreatedBy = entry.CreatedBy,
                    CreatedAt = entry.CreatedAt,
                    IsFile = true
                };
                entries.Remove(sourcePath);
                if (appendEvent)
                    AppendEvent(new StorageEvent
                    {
                        EventType = StorageEventType.Move,
                        SourcePath = sourcePath,
                        DestinationPath = destinationPath
                    });
                return true;
            }
            var toMove = entries.Keys.Where(k => k == sourcePath
                || k.StartsWith(sourcePath + "/", StringComparison.OrdinalIgnoreCase)
                || k.StartsWith(sourcePath + "\\", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var k in toMove)
            {
                var e = entries[k];
                string newPath = destinationPath + k.Substring(sourcePath.Length);
                entries[newPath] = new FileData
                {
                    Path = newPath,
                    Content = e.Content,
                    CreatedBy = e.CreatedBy,
                    CreatedAt = e.CreatedAt,
                    IsFile = e.IsFile
                };
                entries.Remove(k);
            }

            if (appendEvent)
                AppendEvent(new StorageEvent
                {
                    EventType = StorageEventType.Move,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath
                });

            return true;
        }

        public bool Rename(string sourcePath, string destinationPath) => MoveTo(sourcePath, destinationPath);

        public FileData ReadFile(string path)
        {
            path = NormalizePath(path);
            if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only .txt files are supported.");
            if (!entries.ContainsKey(path) || !entries[path].IsFile)
                throw new Exception("File does not exist at the specified path.");
            var entry = entries[path];
            entry.IsFile = true;
            entry.Path = path;
            return entry;
        }

        public FileData[] ShowFolderContent(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "." || path == "/" || path == "\\")
                path = "";
            path = NormalizePath(path);
            if (!entries.ContainsKey(path) || entries[path].IsFile)
                throw new Exception("Folder does not exist at the specified path.");
            List<KeyValuePair<string, FileData>> result;
            if (path == "")
            {
                result = entries.Where(kvp => !string.IsNullOrEmpty(kvp.Key) && !kvp.Key.Contains("/") && !kvp.Key.Contains("\\"))
                    .ToList();
            }
            else
            {
                result = entries.Where(kvp => {
                    if (string.IsNullOrEmpty(kvp.Key) || kvp.Key == path) return false;
                    var parent = System.IO.Path.GetDirectoryName(kvp.Key);
                    return string.Equals(parent, path, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }
            return result.Select(kvp => {
                kvp.Value.IsFile = kvp.Value.IsFile;
                kvp.Value.Path = kvp.Key;
                return kvp.Value;
            }).ToArray();
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
                    if (!entries.ContainsKey(ev.SourcePath))
                        CreateFile(ev.SourcePath, ev.Content, appendEvent: false);
                    break;

                case StorageEventType.CreateFolder:
                    if (!entries.ContainsKey(ev.SourcePath))
                        CreateFolder(ev.SourcePath, appendEvent: false);
                    break;

                case StorageEventType.Delete:
                    if (entries.ContainsKey(ev.SourcePath))
                        Delete(ev.SourcePath, appendEvent: false);
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
