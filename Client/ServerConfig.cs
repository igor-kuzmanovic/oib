using System;
using System.ServiceModel;

namespace Client
{
    public static class ServerConfig
    {
        public static readonly string PrimaryServerAddress = "net.tcp://localhost:9999/WCFService";
        public static readonly string BackupServerAddress = "net.tcp://localhost:8888/WCFService";

        public static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(10);

        public static readonly int MaxRetryCount = 3;
        public static readonly int RetryDelayMs = 1000; public static NetTcpBinding GetBinding()
        {
            NetTcpBinding binding = new NetTcpBinding();
            binding.OpenTimeout = OpenTimeout;
            binding.SendTimeout = SendTimeout;
            binding.ReceiveTimeout = ReceiveTimeout;
            return binding;
        }
    }
}
