using System;
using System.Net;
using System.Net.Sockets;

namespace Server.Infrastructure
{
    public static class ServerRoleHelper
    {
        public static bool IsPortAvailable(string address)
        {
            try
            {
                var uri = new Uri(address.Replace("net.tcp", "http"));
                int port = uri.Port;
                var tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                tcpListener.Stop();

                return true;
            }
            catch { }

            return false;
        }

        public static string GetRemoteServerCN(string address)
        {
            if (address.IndexOf("Primary", StringComparison.OrdinalIgnoreCase) >= 0)
                return Configuration.PrimaryServerCN;
            if (address.IndexOf("Backup", StringComparison.OrdinalIgnoreCase) >= 0)
                return Configuration.BackupServerCN;
            return Configuration.PrimaryServerCN;
        }
    }
}
