using System;
using System.Reflection;
using System.Resources;

namespace SecurityManager
{
    /// <summary>
    /// Configuration class for storing impersonation settings
    /// </summary>
    public static class ImpersonationConfig
    {
        private static ResourceManager resourceManager = null;
        private static object resourceLock = new object();

        private static ResourceManager ResourceMgr
        {
            get
            {
                lock (resourceLock)
                {
                    if (resourceManager == null)
                    {
                        resourceManager = new ResourceManager(
                            typeof(ImpersonationConfigFile).ToString(),
                            Assembly.GetExecutingAssembly());
                    }
                    return resourceManager;
                }
            }
        }

        /// <summary>
        /// Gets the domain for the Editor account
        /// </summary>
        public static string Domain
        {
            get
            {
                try
                {
                    return ResourceMgr.GetString("Domain") ?? ".";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading Domain from resources: {ex.Message}");
                    return ".";
                }
            }
        }

        /// <summary>
        /// Gets the username for the Editor account
        /// </summary>
        public static string Username
        {
            get
            {
                try
                {
                    return ResourceMgr.GetString("Username") ?? "Editor";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading Username from resources: {ex.Message}");
                    return "Editor";
                }
            }
        }

        /// <summary>
        /// Gets the password for the Editor account
        /// </summary>
        public static string Password
        {
            get
            {
                try
                {
                    return ResourceMgr.GetString("Password") ?? "";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading Password from resources: {ex.Message}");
                    return "";
                }
            }
        }
    }
}
