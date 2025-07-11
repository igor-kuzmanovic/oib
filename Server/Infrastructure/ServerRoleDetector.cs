using System;
using System.Net;
using System.Net.Sockets;

namespace Server.Infrastructure
{
    public static class ServerRoleDetector
    {
        public static ServerRole IsPrimaryPortAvailable(string address)
        {
            try
            {
                var uri = new Uri(address.Replace("net.tcp", "http"));
                int port = uri.Port;
                var tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                tcpListener.Stop();

                return ServerRole.Primary;
            }
            catch
            {
            }

            return ServerRole.Backup;
        }
    }
}
