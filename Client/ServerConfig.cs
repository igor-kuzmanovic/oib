using System;
using System.ServiceModel;
using System.Configuration;

namespace Client
{
    public static class ServerConfig
    {
        public static readonly string PrimaryServerAddress = GetConfigValue("PrimaryServerAddress", "net.tcp://localhost:9999/WCFService");
        public static readonly string BackupServerAddress = GetConfigValue("BackupServerAddress", "net.tcp://localhost:8888/WCFService");

        public static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(10);

        public static readonly int MaxRetryCount = 3;
        public static readonly int RetryDelayMs = 1000;

        // Helper method to get config value with a default fallback
        private static string GetConfigValue(string key, string defaultValue)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrEmpty(value))
                {
                    Console.WriteLine($"Found {key} in App.config: {value}");
                    return value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading {key} from App.config: {ex.Message}");
            }

            Console.WriteLine($"Using default {key}: {defaultValue}");
            return defaultValue;
        }

        public static NetTcpBinding GetBinding()
        {
            NetTcpBinding binding = new NetTcpBinding();
            binding.OpenTimeout = OpenTimeout;
            binding.SendTimeout = SendTimeout;
            binding.ReceiveTimeout = ReceiveTimeout;
            binding.Security.Mode = SecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            return binding;
        }
    }
}
