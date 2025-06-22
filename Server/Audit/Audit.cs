using Server.Infrastructure;
using System;
using System.Diagnostics;

namespace Server.Audit
{
    public class Audit : IDisposable
    {
        private static readonly EventLog customLog = null;

        static Audit()
        {
            try
            {
                if (!EventLog.SourceExists(Configuration.AuditSourceName))
                {
                    EventLog.CreateEventSource(Configuration.AuditSourceName, Configuration.AuditLogName);
                }
                customLog = new EventLog(Configuration.AuditLogName, Environment.MachineName, Configuration.AuditSourceName);
            }
            catch (Exception ex)
            {
                customLog = null;
                Console.WriteLine($"Error initializing audit log: {ex.Message}");
            }
        }

        public static void AuthenticationSuccess(string userName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.AuthenticationSuccess;
                string formattedMessage = String.Format(message, userName);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.SuccessAudit, (int)AuditEventTypes.AuthenticationSuccess);
            }
        }

        public static void AuthorizationSuccess(string userName, string actionName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.AuthorizationSuccess;
                string formattedMessage = String.Format(message, userName, actionName, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.SuccessAudit, (int)AuditEventTypes.AuthorizationSuccess);
            }
        }

        public static void AuthorizationFailed(string userName, string actionName, string serverAddress, string reason)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.AuthorizationFailed;
                string formattedMessage = String.Format(message, userName, actionName, serverAddress, reason);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.FailureAudit, (int)AuditEventTypes.AuthorizationFailed);
            }
        }

        public static void FileCreated(string userName, string fileName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.FileCreated;
                string formattedMessage = String.Format(message, userName, fileName, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.FileCreated);
            }
        }

        public static void FolderCreated(string userName, string folderName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.FolderCreated;
                string formattedMessage = String.Format(message, userName, folderName, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.FolderCreated);
            }
        }

        public static void FileDeleted(string userName, string fileName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.FileDeleted;
                string formattedMessage = String.Format(message, userName, fileName, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.FileDeleted);
            }
        }

        public static void FolderDeleted(string userName, string folderName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.FolderDeleted;
                string formattedMessage = String.Format(message, userName, folderName, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.FolderDeleted);
            }
        }

        public static void FileMoved(string userName, string sourceFileName, string destFileName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.FileMoved;
                string formattedMessage = String.Format(message, userName, sourceFileName, destFileName, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.FileMoved);
            }
        }

        public static void FolderMoved(string userName, string sourceFolderName, string destFolderName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.FolderMoved;
                string formattedMessage = String.Format(message, userName, sourceFolderName, destFolderName, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.FolderMoved);
            }
        }

        public static void FileAccessed(string userName, string fileName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.FileAccessed;
                string formattedMessage = String.Format(message, userName, fileName, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.FileAccessed);
            }
        }

        public static void FolderAccessed(string userName, string folderName, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.FolderAccessed;
                string formattedMessage = String.Format(message, userName, folderName, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.FolderAccessed);
            }
        }

        public static void ServerStarted(string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.ServerStarted;
                string formattedMessage = String.Format(message, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.ServerStarted);
            }
        }

        public static void ServerStopped(string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.ServerStopped;
                string formattedMessage = String.Format(message, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.ServerStopped);
            }
        }

        public static void ServerError(string errorMessage, string serverAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.ServerError;
                string formattedMessage = String.Format(message, errorMessage, serverAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Error, (int)AuditEventTypes.ServerError);
            }
        }

        public static void ServerTransitioned(string oldAddress, string newAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.ServerTransitioned;
                string formattedMessage = String.Format(message, oldAddress, newAddress);
                customLog.WriteEntry(formattedMessage, EventLogEntryType.Information, (int)AuditEventTypes.ServerTransitioned);
            }
        }

        public static void ServerSynchronized(string fromAddress, string toAddress)
        {
            if (customLog != null)
            {
                string message = AuditEventFile.ServerSynchronized;
                string formattedMessage = String.Format(message, fromAddress, toAddress);
                customLog.WriteEntry(message, EventLogEntryType.Information, (int)AuditEventTypes.ServerSynchronized);
            }
        }

        public void Dispose()
        {
            if (customLog != null)
            {
                customLog.Dispose();
            }
        }
    }
}
