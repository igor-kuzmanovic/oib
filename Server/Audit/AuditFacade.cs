namespace Server.Audit
{
    public static class AuditFacade
    {
        private static readonly Audit audit = new Audit();

        public static void AuthenticationSuccess(string userName, string serverAddress)
        {
            Audit.AuthenticationSuccess(userName, serverAddress);
        }

        public static void AuthorizationSuccess(string userName, string serviceName, string serverAddress)
        {
            Audit.AuthorizationSuccess(userName, serviceName, serverAddress);
        }

        public static void AuthorizationFailed(string userName, string serviceName, string serverAddress, string reason)
        {
            Audit.AuthorizationFailed(userName, serviceName, serverAddress, reason);
        }

        public static void FileCreated(string userName, string filePath, string serverAddress)
        {
            Audit.FileCreated(userName, filePath, serverAddress);
        }

        public static void FolderCreated(string userName, string folderPath, string serverAddress)
        {
            Audit.FolderCreated(userName, folderPath, serverAddress);
        }

        public static void FileDeleted(string userName, string filePath, string serverAddress)
        {
            Audit.FileDeleted(userName, filePath, serverAddress);
        }

        public static void FolderDeleted(string userName, string folderPath, string serverAddress)
        {
            Audit.FolderDeleted(userName, folderPath, serverAddress);
        }

        public static void FileMoved(string userName, string sourcePath, string destinationPath, string serverAddress)
        {
            Audit.FileMoved(userName, sourcePath, destinationPath, serverAddress);
        }

        public static void FolderMoved(string userName, string sourcePath, string destinationPath, string serverAddress)
        {
            Audit.FolderMoved(userName, sourcePath, destinationPath, serverAddress);
        }

        public static void FileAccessed(string userName, string filePath, string serverAddress)
        {
            Audit.FileAccessed(userName, filePath, serverAddress);
        }

        public static void FolderAccessed(string userName, string folderPath, string serverAddress)
        {
            Audit.FolderAccessed(userName, folderPath, serverAddress);
        }

        public static void ServerStarted(string serverAddress)
        {
            Audit.ServerStarted(serverAddress);
        }

        public static void ServerStopped(string serverAddress)
        {
            Audit.ServerStopped(serverAddress);
        }

        public static void ServerTransitioned(string fromAddress, string toAddress)
        {
            Audit.ServerTransitioned(fromAddress, toAddress);
        }

        public static void ServerError(string errorMessage, string serverAddress)
        {
            Audit.ServerError(errorMessage, serverAddress);
        }
        
        public static void ServerSynchronized(string fromAddress, string toAddress)
        {
            Audit.ServerSynchronized(fromAddress, toAddress);
        }
    }
}
