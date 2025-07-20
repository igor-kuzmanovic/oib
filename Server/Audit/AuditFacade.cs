namespace Server.Audit
{
    public static class AuditFacade
    {
        private static readonly Audit audit = new Audit();

        public static void AuthenticationSuccess(string userName)
        {
            Audit.AuthenticationSuccess(userName);
        }

        public static void AuthenticationFailed(string userName, string reason)
        {
            Audit.AuthenticationFailed(userName, reason);
        }

        public static void AuthorizationSuccess(string userName, string serviceName)
        {
            Audit.AuthorizationSuccess(userName, serviceName);
        }

        public static void AuthorizationFailed(string userName, string serviceName, string reason)
        {
            Audit.AuthorizationFailed(userName, serviceName, reason);
        }

        public static void FileCreated(string userName, string filePath)
        {
            Audit.FileCreated(userName, filePath);
        }

        public static void FolderCreated(string userName, string folderPath)
        {
            Audit.FolderCreated(userName, folderPath);
        }

        public static void FileDeleted(string userName, string filePath)
        {
            Audit.FileDeleted(userName, filePath);
        }

        public static void FolderDeleted(string userName, string folderPath)
        {
            Audit.FolderDeleted(userName, folderPath);
        }

        public static void FileMoved(string userName, string sourcePath, string destinationPath)
        {
            Audit.FileMoved(userName, sourcePath, destinationPath);
        }

        public static void FileRenamed(string userName, string oldFilePath, string newFilePath)
        {
            Audit.FileRenamed(userName, oldFilePath, newFilePath);
        }

        public static void FolderMoved(string userName, string sourcePath, string destinationPath)
        {
            Audit.FolderMoved(userName, sourcePath, destinationPath);
        }

        public static void FolderRenamed(string userName, string oldFolderPath, string newFolderPath)
        {
            Audit.FolderRenamed(userName, oldFolderPath, newFolderPath);
        }

        public static void FileAccessed(string userName, string filePath)
        {
            Audit.FileAccessed(userName, filePath);
        }

        public static void FolderAccessed(string userName, string folderPath)
        {
            Audit.FolderAccessed(userName, folderPath);
        }

        public static void ServerStarted(string serverName)
        {
            Audit.ServerStarted(serverName);
        }

        public static void ServerStopped(string serverName)
        {
            Audit.ServerStopped(serverName);
        }

        public static void ServerTransitioned(string serverName)
        {
            Audit.ServerTransitioned(serverName);
        }

        public static void ServerError(string errorMessage)
        {
            Audit.ServerError(errorMessage);
        }
        
        public static void ServerSynchronized(string serverName, string remoteServerName, int eventCount)
        {
            Audit.ServerSynchronized(serverName, remoteServerName, eventCount);
        }
    }
}
