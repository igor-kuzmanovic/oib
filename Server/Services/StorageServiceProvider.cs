using Server.Infrastructure;
using System;

namespace Server.Services
{
    public static class StorageServiceProvider
    {
        private static readonly IStorageService _instance = CreateInstance();

        private static IStorageService CreateInstance()
        {
            switch (Configuration.StorageService)
            {
                case "FileStorageService":
                    return new FileStorageService();
                case "MemoryStorageService":
                    return new MemoryStorageService();
                default:
                    throw new NotSupportedException($"Unknown storage service: {Configuration.StorageService}");
            }
        }

        public static IStorageService Instance => _instance;
    }
}
