using System;
using System.Collections.Generic;
using System.Security.Principal;
using Contracts.Authorization;
using Contracts.Helpers;

namespace Server.Authorization
{
    public class Principal : ICustomPrincipal
    {
        private readonly WindowsIdentity identity;
        private readonly List<Permission> permissions;

        public Principal(WindowsIdentity windowsIdentity)
        {
            this.identity = windowsIdentity ?? throw new ArgumentNullException(nameof(windowsIdentity));
            this.permissions = new List<Permission>();

            LoadRoles();
        }

        public IIdentity Identity => identity;

        private void LoadRoles()
        {
            string parsedName = SecurityHelper.GetName(identity);
            if (RolesConfig.GetPermissions(parsedName, out Permission[] permissions))
            {
                foreach (Permission permission in permissions)
                {
                    this.permissions.Add(permission);
                }
            }
        }

        public bool IsInRole(Permission permission)
        {
            return permissions.Contains(permission);
        }

        public bool IsInRole(string role)
        {
            if (Enum.TryParse(role, out Permission parsedRole))
            {
                return permissions.Contains(parsedRole);
            }

            return false;
        }
    }
}
