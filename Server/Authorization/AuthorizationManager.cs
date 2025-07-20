using Contracts.Authorization;
using Contracts.Helpers;
using Server.Audit;
using System;
using System.Runtime.Serialization;
using System.ServiceModel;

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
                AuditFacade.AuthorizationFailed(SecurityHelper.GetName(principal.Identity), OperationContext.Current.IncomingMessageHeaders.Action, "There is no principal");
                return false;
            }

            if (!principal.IsInRole(Permission.See))
            {
                Console.WriteLine($"[AuthorizationManager] Principal has no permission {Permission.See}");
                AuditFacade.AuthorizationFailed(SecurityHelper.GetName(principal.Identity), OperationContext.Current.IncomingMessageHeaders.Action, $"Principal has no permission {Permission.See}");
                return false;
            }

            Console.WriteLine($"[AuthorizationManager] 'CheckAccessCore' success for principal {principal.Identity.Name}");
            AuditFacade.AuthorizationSuccess(SecurityHelper.GetName(principal.Identity), OperationContext.Current.IncomingMessageHeaders.Action);
            return true;
        }
    }
}
