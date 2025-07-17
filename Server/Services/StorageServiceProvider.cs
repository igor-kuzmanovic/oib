using Server.Infrastructure;
using System;

namespace Server.Services
{
    public static class StorageServiceProvider
    {
        private static IStorageService _instance;

        public static void Initialize(string dataDirectory = null)
        {
            if (_instance != null)
                throw new InvalidOperationException("StorageServiceProvider is already initialized.");

            switch (Configuration.StorageService)
            {
                case "FileStorageService":
                    if (dataDirectory == null)
                        throw new ArgumentNullException(nameof(dataDirectory), "Data directory is required for FileStorageService.");
                    _instance = new FileStorageService(dataDirectory);
                    break;

                case "MemoryStorageService":
                    _instance = new MemoryStorageService();
                    break;

                default:
                    throw new NotSupportedException($"Unknown storage service: {Configuration.StorageService}");
            }
        }

        public static IStorageService Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("StorageServiceProvider is not initialized. Call Initialize() first.");
                return _instance;
            }
        }
    }
}
