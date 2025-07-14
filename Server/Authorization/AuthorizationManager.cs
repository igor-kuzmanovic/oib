using System;
using System.ServiceModel;
using Contracts.Authorization;

namespace Server.Authorization
{
    public class AuthorizationManager : ServiceAuthorizationManager
    {
        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            Console.WriteLine("[AuthorizationManager] 'CheckAccessCore' called");

            Principal principal = operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Principal"] as Principal;
            if (principal == null)
            {
                Console.WriteLine("[AuthorizationManager] There is no principal");
                return false;
            }

            if (!principal.IsInRole(Permission.See))
            {
                Console.WriteLine($"[AuthorizationManager] Principal has no permission {Permission.See}");
                return false;
            }

            Console.WriteLine($"[AuthorizationManager] 'CheckAccessCore' success for principal {principal.Identity.Name}");
            return true;
        }
    }
}
