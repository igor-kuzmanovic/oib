using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Server.Audit
{
    public enum AuditEventTypes
    {
        AuthenticationSuccess = 0,
        AuthorizationSuccess = 1,
        AuthorizationFailed = 2,
        FileCreated = 3,
        FolderCreated = 4,
        FileDeleted = 5,
        FolderDeleted = 6,
        FileMoved = 7,
        FolderMoved = 8,
        FileAccessed = 9,
        FolderAccessed = 10,
        ServerStarted = 11,
        ServerStopped = 12,
        ServerTransitioned = 13,
        ServerError = 14,
        ServerSynchronized = 15,
    }

    public static class AuditEventMessages
    {
        private static ResourceManager resourceManager = null;
        private static readonly object resourceLock = new object();

        private static ResourceManager ResourceMgr
        {
            get
            {
                lock (resourceLock)
                {
                    if (resourceManager == null)
                    {
                        resourceManager = new ResourceManager(typeof(AuditEventFile));
                    }
                    return resourceManager;
                }
            }
        }

        public static string GetMessage(AuditEventTypes eventType)
        {
            try
            {
                string resourceName = eventType.ToString();
                string message = ResourceMgr.GetString(resourceName, CultureInfo.CurrentCulture);
                return message ?? $"Message for event type {eventType} not found.";
            }
            catch (Exception ex)
            {
                return $"Error retrieving message for event type {eventType}: {ex.Message}";
            }
        }

        public static string GetMessage(string messageName)
        {
            try
            {
                string message = ResourceMgr.GetString(messageName, CultureInfo.CurrentCulture);
                return message ?? $"Message '{messageName}' not found.";
            }
            catch (Exception ex)
            {
                return $"Error retrieving message '{messageName}': {ex.Message}";
            }
        }
    }
}
