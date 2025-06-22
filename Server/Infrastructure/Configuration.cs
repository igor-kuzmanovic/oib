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

        public static string ServerAddress => ConfigurationManager.AppSettings["ServerAddress"] ?? "net.tcp://localhost:9999/WCFService";

        public static string DataDirectory => ConfigurationManager.AppSettings["DataDirectory"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\Files");

        public static string AuditSourceName => ConfigurationManager.AppSettings["AuditSourceName"] ?? "FileServer.Audit";
        public static string AuditLogName => ConfigurationManager.AppSettings["AuditLogName"] ?? "FileServerAuditLog";

        public static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(DataDirectory);
        }
    }
}
