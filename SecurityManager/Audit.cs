using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SecurityManager
{
    public class Audit : IDisposable
    {

        private static EventLog customLog = null;
        private static readonly string SourceName = GetConfigValue("AuditSourceName", "SecurityManager.Audit");
        private static readonly string LogName = GetConfigValue("AuditLogName", "FileServerAuditLog");

        private static string GetConfigValue(string key, string defaultValue)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch { }
            return defaultValue;
        }

        static Audit()
        {
            try
            {
                if (!EventLog.SourceExists(SourceName))
                {
                    EventLog.CreateEventSource(SourceName, LogName);
                }
                customLog = new EventLog(LogName,
                    Environment.MachineName, SourceName);
            }
            catch (Exception e)
            {
                customLog = null;
                Console.WriteLine("Error while trying to create log handle. Error = {0}", e.Message);
            }
        }
        public static void AuthenticationSuccess(string userName, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string UserAuthenticationSuccess = AuditEvents.AuthenticationSuccess;
                string message = $"Server [{serverIdentifier}]: {String.Format(UserAuthenticationSuccess, userName)}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.AuthenticationSuccess));
            }
        }
        public static void AuthorizationSuccess(string userName, string serviceName, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string AuthorizationSuccess = AuditEvents.AuthorizationSuccess;
                string message = $"Server [{serverIdentifier}]: {String.Format(AuthorizationSuccess, userName, serviceName)}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.AuthorizationSuccess));
            }
        }       /// <summary>
                /// 
                /// </summary>
                /// <param name="userName"></param>
                /// <param name="serviceName"> should be read from the OperationContext as follows: OperationContext.Current.IncomingMessageHeaders.Action</param>
                /// <param name="reason">permission name</param>
        public static void AuthorizationFailed(string userName, string serviceName, string reason, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string AuthorizationFailed = AuditEvents.AuthorizationFailed;
                string message = $"Server [{serverIdentifier}]: {String.Format(AuthorizationFailed, userName, serviceName, reason)}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
                    (int)AuditEventTypes.AuthorizationFailed));
            }
        }
        public static void FileCreated(string userName, string filePath, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Server [{serverIdentifier}]: User {userName} created file {filePath}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException("Error while trying to write file creation event to event log.");
            }
        }
        public static void FolderCreated(string userName, string folderPath, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Server [{serverIdentifier}]: User {userName} created folder {folderPath}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException("Error while trying to write folder creation event to event log.");
            }
        }
        public static void FileDeleted(string userName, string filePath, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Server [{serverIdentifier}]: User {userName} deleted file {filePath}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException("Error while trying to write file deletion event to event log.");
            }
        }
        public static void FolderDeleted(string userName, string folderPath, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Server [{serverIdentifier}]: User {userName} deleted folder {folderPath}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException("Error while trying to write folder deletion event to event log.");
            }
        }
        public static void FileMoved(string userName, string sourcePath, string destinationPath, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Server [{serverIdentifier}]: User {userName} moved file from {sourcePath} to {destinationPath}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException("Error while trying to write file move event to event log.");
            }
        }
        public static void FolderMoved(string userName, string sourcePath, string destinationPath, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Server [{serverIdentifier}]: User {userName} moved folder from {sourcePath} to {destinationPath}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException("Error while trying to write folder move event to event log.");
            }
        }
        public static void FileAccessed(string userName, string filePath, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Server [{serverIdentifier}]: User {userName} accessed file {filePath}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException("Error while trying to write file access event to event log.");
            }
        }
        public static void FolderAccessed(string userName, string folderPath, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Server [{serverIdentifier}]: User {userName} accessed folder {folderPath}";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException("Error while trying to write folder access event to event log.");
            }
        }
        public static void BackupServerStarted(string address)
        {
            if (customLog != null)
            {
                string serverIdentifier = address;
                string message = $"Backup Server [{serverIdentifier}]: Server started successfully";
                customLog.WriteEntry(message);
            }
            else
            {
                throw new ArgumentException("Error while trying to write backup server startup event to event log.");
            }
        }
        public static void BackupServerError(string errorMessage, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Backup Server [{serverIdentifier}]: Error: {errorMessage}";
                customLog.WriteEntry(message, EventLogEntryType.Error);
            }
            else
            {
                throw new ArgumentException("Error while trying to write backup server error event to event log.");
            }
        }
        public static void ServerError(string errorMessage, string serverAddress = null)
        {
            if (customLog != null)
            {
                string serverIdentifier = serverAddress ?? "Unknown";
                string message = $"Server [{serverIdentifier}]: Error: {errorMessage}";
                customLog.WriteEntry(message, EventLogEntryType.Error);
            }
            else
            {
                throw new ArgumentException("Error while trying to write server error event to event log.");
            }
        }

        public void Dispose()
        {
            if (customLog != null)
            {
                customLog.Dispose();
                customLog = null;
            }
        }
    }
}
