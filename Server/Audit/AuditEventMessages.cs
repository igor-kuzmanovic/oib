using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Server.Audit
{
    public enum AuditEventTypes
    {
        AuthenticationSuccess,
        AuthenticationFailed,
        AuthorizationSuccess,
        AuthorizationFailed,
        FileCreated,
        FolderCreated,
        FileDeleted,
        FolderDeleted,
        FileMoved,
        FolderMoved,
        FileRenamed,
        FolderRenamed,
        FileAccessed,
        FolderAccessed,
        ServerStarted,
        ServerStopped,
        ServerTransitioned,
        ServerError,
        ServerSynchronized,
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
