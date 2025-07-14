using Contracts.Authorization;
using Contracts.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Principal;

namespace Server.Authorization
{
    public class Principal : ICustomPrincipal
    {
        private readonly WindowsIdentity identity;
        private readonly List<Permission> permissions;

        public Principal(WindowsIdentity windowsIdentity)
        {
            this.identity = windowsIdentity ?? throw new ArgumentNullException(nameof(windowsIdentity));
            Console.WriteLine($"[Principal] Created for identity: {identity.Name}");
            Console.WriteLine($"[Principal] Impersonation level: {identity.ImpersonationLevel}");
            this.permissions = new List<Permission>();

            LoadRoles();
        }

        public IIdentity Identity => identity;

        private void LoadRoles()
        {
            Console.WriteLine("[Principal] 'LoadRoles' called");

            List<Role> roles = new List<Role>();
            foreach (var group in identity.Groups)
            {
                try
                {
                    var name = SecurityHelper.ParseName(group.Translate(typeof(NTAccount))?.Value);
                    if (!string.IsNullOrEmpty(name) && Role.TryParse(name, out Role role))
                        roles.Add(role);
                }
                catch { }
            }

            if (!RolesConfig.GetPermissions(roles, out Permission[] permissions))
            {
                Console.WriteLine($"[Principal] No permissions found for roles: {string.Join(", ", roles)}");
                return;
            }

            foreach (Permission permission in permissions)
            {
                Console.WriteLine($"[Principal] Adding permission: {permission}");
                this.permissions.Add(permission);
            }

            Console.WriteLine("[Principal] 'LoadRoles' success");
        }

        public bool IsInRole(Permission permission)
        {
            Console.WriteLine($"[Principal] 'IsInRole' called with permission: {permission}");

            if (!permissions.Contains(permission))
            {
                Console.WriteLine($"[Principal] Permission {permission} not found in {string.Join(", ", permissions)}");
                return false;
            }

            Console.WriteLine($"[Principal] 'IsInRole' success with permission: {permission}");
            return true;
        }

        public bool IsInRole(string role)
        {
            Console.WriteLine($"[Principal] 'IsInRole' called with role: {role}");

            if (!Enum.TryParse(role, out Permission parsedRole))
            {
                Console.WriteLine($"[Principal] Failed to parse role: {role}");
                return false;
            }

            if (!permissions.Contains(parsedRole))
            {
                Console.WriteLine($"[Principal] Role {parsedRole} not found in {string.Join(", ", permissions)}");
                return false;
            }

            Console.WriteLine($"[Principal] 'IsInRole' success with role: {role}");
            return true;
        }
    }
}
