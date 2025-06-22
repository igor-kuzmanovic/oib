using Contracts.Authorization;
using Contracts.Helpers;
using System;

namespace Server.Authorization
{
    public static class RolesConfig
    {

        public static bool GetPermissions(string username, out Permission[] permissions)
        {
            string[] permissionStrings = new string[10];
            bool result = false;

            string normalizedUsername = SecurityHelper.ParseName(username ?? string.Empty);
            string permissionString = (string)RolesConfigFile.ResourceManager.GetObject(normalizedUsername);

            if (permissionString != null)
            {
                permissionStrings = permissionString.Split(',');

                result = true;
            }

            if (result)
            {
                permissions = new Permission[permissionStrings.Length];

                for (int i = 0; i < permissionStrings.Length; i++)
                {
                    if (Enum.TryParse(permissionStrings[i], out Permission permission))
                    {
                        permissions[i] = permission;
                    }
                }

                return true;
            }
            
            permissions = new Permission[0];

            return false;
        }
    }
}
