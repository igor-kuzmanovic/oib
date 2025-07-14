using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Security.Principal;

namespace Server.Services
{
    public class MemoryStorageService : IStorageService
    {
        private class Entry
        {
            public string Path { get; set; }
            public bool IsFile { get; set; }
            public byte[] Content { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public MemoryStorageService()
        {
            entries[""] = new Entry
            {
                Path = "",
                IsFile = false,
                Content = null,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };
        }

        private string NormalizePath(string path)
        {
            return path.TrimStart('/', '\\');
        }

        public bool CreateFile(string path, byte[] content)
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
            entries[path] = new Entry
            {
                Path = path,
                IsFile = true,
                Content = content,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
            return true;
        }

        public bool CreateFolder(string path)
        {
            path = NormalizePath(path);
            if (entries.ContainsKey(path))
                throw new Exception("File or folder already exists at the specified path.");
            string parent = System.IO.Path.GetDirectoryName(path) ?? "";
            if (!entries.ContainsKey(parent) || entries[parent].IsFile)
                throw new Exception("Parent folder does not exist.");
            string createdBy = GetCurrentUser();
            entries[path] = new Entry
            {
                Path = path,
                IsFile = false,
                Content = null,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
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

        public bool Delete(string path)
        {
            path = NormalizePath(path);
            if (!entries.ContainsKey(path))
                throw new Exception("File or folder does not exist at the specified path.");
            var entry = entries[path];
            if (entry.IsFile)
            {
                entries.Remove(path);
                return true;
            }
            var toRemove = entries.Keys.Where(k => k == path || k.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase) || k.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var k in toRemove)
                entries.Remove(k);
            return true;
        }

        public bool MoveTo(string sourcePath, string destinationPath)
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
                entries[destinationPath] = new Entry
                {
                    Path = destinationPath,
                    IsFile = true,
                    Content = entry.Content,
                    CreatedBy = entry.CreatedBy,
                    CreatedAt = entry.CreatedAt
                };
                entries.Remove(sourcePath);
                return true;
            }
            var toMove = entries.Keys.Where(k => k == sourcePath || k.StartsWith(sourcePath + "/", StringComparison.OrdinalIgnoreCase) || k.StartsWith(sourcePath + "\\", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var k in toMove)
            {
                var e = entries[k];
                string newPath = destinationPath + k.Substring(sourcePath.Length);
                entries[newPath] = new Entry
                {
                    Path = newPath,
                    IsFile = e.IsFile,
                    Content = e.Content,
                    CreatedBy = e.CreatedBy,
                    CreatedAt = e.CreatedAt
                };
                entries.Remove(k);
            }
            return true;
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            return MoveTo(sourcePath, destinationPath);
        }

        public byte[] ReadFile(string path)
        {
            path = NormalizePath(path);
            if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only .txt files are supported.");
            if (!entries.ContainsKey(path) || !entries[path].IsFile)
                throw new Exception("File does not exist at the specified path.");
            return entries[path].Content;
        }

        public string[] ShowFolderContent(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "." || path == "/" || path == "\\")
                path = "";
            path = NormalizePath(path);
            if (!entries.ContainsKey(path) || entries[path].IsFile)
                throw new Exception("Folder does not exist at the specified path.");
            List<string> result;
            if (path == "")
            {
                result = entries.Values
                    .Where(e => !string.IsNullOrEmpty(e.Path) && !e.Path.Contains("/") && !e.Path.Contains("\\"))
                    .Select(e => e.Path)
                    .ToList();
            }
            else
            {
                result = entries.Values
                    .Where(e => {
                        if (string.IsNullOrEmpty(e.Path) || e.Path == path) return false;
                        var parent = System.IO.Path.GetDirectoryName(e.Path);
                        return string.Equals(parent, path, StringComparison.OrdinalIgnoreCase);
                    })
                    .Select(e => e.Path)
                    .ToList();
            }
            return result.ToArray();
        }
    }
}
