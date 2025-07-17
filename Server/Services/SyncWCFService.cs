using System;
using System.ServiceModel;
using Contracts.Models;

namespace Server.Services
{
    public class SyncWCFService : ISyncWCFService
    {
        private static readonly IStorageService storageService = StorageServiceProvider.Instance;

        public StorageEvent[] GetEventsSinceId(int lastEventId)
        {
            Console.WriteLine($"[SyncWCFService] GetEventsSinceId called with lastEventId: {lastEventId}");
            return storageService.GetEventsSinceId(lastEventId);
        }
    }
}
