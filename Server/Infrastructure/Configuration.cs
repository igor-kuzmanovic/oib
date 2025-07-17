using System;
using System.Configuration;
using System.IO;

namespace Server.Infrastructure
{
    public static class Configuration
    {
        static Configuration()
        {
            EnsureDirectoriesExist();
        }

        public static string PrimaryServerAddress => ConfigurationManager.AppSettings["PrimaryServerAddress"] ?? "net.tcp://localhost:9999/FileWCFService";
        public static string BackupServerAddress => ConfigurationManager.AppSettings["BackupServerAddress"] ?? "net.tcp://localhost:8888/FileWCFService";

        public static string ServerSyncAddress => ConfigurationManager.AppSettings["ServerSyncAddress"] ?? "net.tcp://localhost:7777/SyncWCFService";

        public static string ServerSyncCertificateCN => ConfigurationManager.AppSettings["ServerSyncCertificateCN"] ?? "FileServerAlpha";
        public static string ClientSyncCertificateCN => ConfigurationManager.AppSettings["ClientSyncCertificateCN"] ?? "FileServerBeta";
        public static string RemoteServerSyncCertificateCN => ConfigurationManager.AppSettings["RemoteServerSyncCertificateCN"] ?? "FileServerAlpha";

        public static string StorageService => ConfigurationManager.AppSettings["StorageService"] ?? "MemoryStorageService";
        public static string DataDirectory => ConfigurationManager.AppSettings["DataDirectory"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\Files");

        public static string AuditSourceName => ConfigurationManager.AppSettings["AuditSourceName"] ?? "FileServer.Audit";
        public static string AuditLogName => ConfigurationManager.AppSettings["AuditLogName"] ?? "FileServerAuditLog";

        public static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(DataDirectory);
        }
    }
}
