using System;
using System.ServiceModel;

namespace Server.Services
{
    public class SyncWCFService : ISyncWCFService
    {
        public bool Ping()
        {
            Console.WriteLine("[SyncWCFService] Ping received");
            return true;
        }
    }
}
