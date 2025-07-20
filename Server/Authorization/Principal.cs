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
        private readonly List<Role> roles = new List<Role>();
        private readonly List<Permission> permissions = new List<Permission>();

        public Principal(WindowsIdentity windowsIdentity)
        {
            this.identity = windowsIdentity ?? throw new ArgumentNullException(nameof(windowsIdentity));
            Console.WriteLine($"[Principal] Created for identity: {identity.Name}");
            Console.WriteLine($"[Principal] Impersonation level: {identity.ImpersonationLevel}");

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
            this.roles.AddRange(roles);
        }

        public bool HasPermission(Permission permission)
        {
            Console.WriteLine($"[Principal] 'HasPermission' called with permission: {permission}");

            if (!permissions.Contains(permission))
            {
                Console.WriteLine($"[Principal] Permission {permission} not found in {string.Join(", ", permissions)}");
                return false;
            }

            Console.WriteLine($"[Principal] 'HasPermission' success with permission: {permission}");
            return true;
        }

        public bool HasRole(Role role)
        {
            Console.WriteLine($"[Principal] 'HasRole' called with role: {role}");
            if (!roles.Contains(role))
            {
                Console.WriteLine($"[Principal] Role {role} not found in {string.Join(", ", roles)}");
                return false;
            }
            Console.WriteLine($"[Principal] 'HasRole' success with role: {role}");
            return true;
        }

        public bool IsInRole(string role)
        {
            Console.WriteLine($"[Principal] 'IsInRole' called with: {role}");

            bool isRole = Enum.TryParse(role, out Role parsedRole) && HasRole(parsedRole);
            if (isRole)
            {
                Console.WriteLine($"[Principal] Role {role} is valid and found in roles.");
                return true;
            }

            bool isPermission = Enum.TryParse(role, out Permission parsedPermission) && HasPermission(parsedPermission);
            if (isPermission)
            {
                Console.WriteLine($"[Principal] Permission {role} is valid and found in permissions.");
                return true;
            }

            Console.WriteLine($"[Principal] Role or permission {role} not found in roles or permissions.");
            return false;
        }
    }
}
