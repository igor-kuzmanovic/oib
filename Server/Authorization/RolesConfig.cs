using Contracts.Authorization;
using Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Server.Authorization
{
    public static class RolesConfig
    {

        public static bool GetPermissions(IEnumerable<Role> roles, out Permission[] permissions)
        {
            Console.WriteLine($"[RolesConfig] Called 'GetPermissions' with roles: {string.Join(", ", roles)}");

            var permissionSet = new HashSet<Permission>();

            foreach (var role in roles)
            {
                Console.WriteLine($"[RolesConfig] Checking role: {role}");

                string normalizedRoleName = role.ToString().Trim().ToUpperInvariant();

                string permissionString = ConfigurationManager.AppSettings[normalizedRoleName];

                if (permissionString == null)
                {
                    Console.WriteLine($"[RolesConfig] No permissions found for role: {normalizedRoleName}");
                    continue;
                }

                Console.WriteLine($"[RolesConfig] Permissions string found for role '{normalizedRoleName}': {permissionString}");

                var parts = permissionString.Split(',');

                if (parts.Length == 0)
                {
                    Console.WriteLine($"[RolesConfig] Empty permissions for role: {normalizedRoleName}");
                    continue;
                }

                foreach (var part in parts)
                {
                    if (Enum.TryParse(part.Trim(), out Permission p))
                    {
                        permissionSet.Add(p);
                        Console.WriteLine($"[RolesConfig] Added permission: {p}");
                    }
                    else
                    {
                        Console.WriteLine($"[RolesConfig] Failed to parse permission: '{part}' for role: {normalizedRoleName}");
                    }
                }
            }

            if (permissionSet.Count == 0)
            {
                Console.WriteLine("[RolesConfig] No permissions found for any roles.");
                permissions = Array.Empty<Permission>();
                return false;
            }

            permissions = permissionSet.ToArray();
            Console.WriteLine($"[RolesConfig] Total permissions loaded: {permissions.Length}");
            return true;
        }
    }
}
