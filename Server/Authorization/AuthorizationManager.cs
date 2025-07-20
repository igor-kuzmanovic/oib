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

            if (!principal.HasRole(Role.Reader))
            {
                Console.WriteLine($"[AuthorizationManager] Principal has no role {Role.Reader}");
                AuditFacade.AuthorizationFailed(SecurityHelper.GetName(principal.Identity), OperationContext.Current.IncomingMessageHeaders.Action, $"Principal has no role {Role.Reader}");
                return false;
            }

            Console.WriteLine($"[AuthorizationManager] 'CheckAccessCore' success for principal {principal.Identity.Name}");
            AuditFacade.AuthorizationSuccess(SecurityHelper.GetName(principal.Identity), OperationContext.Current.IncomingMessageHeaders.Action);
            return true;
        }
    }
}
