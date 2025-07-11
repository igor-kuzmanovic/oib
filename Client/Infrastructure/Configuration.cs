using System.Configuration;

namespace Client.Infrastructure
{
    public static class Configuration
    {
        public static string PrimaryServerAddress => ConfigurationManager.AppSettings["PrimaryServerAddress"] ?? "net.tcp://localhost:9999/FileWCFService";
        public static string BackupServerAddress => ConfigurationManager.AppSettings["BackupServerAddress"] ?? "net.tcp://localhost:8888/FileWCFService";
    }
}
