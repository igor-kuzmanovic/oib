using System;
using System.ServiceModel;
using System.Configuration;

namespace Client
{
    public static class ServerConfig
    {
        public static readonly string PrimaryServerAddress = ConfigurationManager.AppSettings["PrimaryServerAddress"] ?? throw new InvalidOperationException("PrimaryServerAddress is missing in App.config.");
        public static readonly string BackupServerAddress = ConfigurationManager.AppSettings["BackupServerAddress"] ?? throw new InvalidOperationException("BackupServerAddress is missing in App.config.");

        public static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(10);

        public static readonly int MaxRetryCount = 3;
        public static readonly int RetryDelayMs = 1000;

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
