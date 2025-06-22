using System;
using System.ServiceModel;
using Contracts.Authorization;

namespace Server.Authorization
{
    public class AuthorizationManager : ServiceAuthorizationManager
    {
        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            Principal principal = operationContext.ServiceSecurityContext.AuthorizationContext.Properties["Principal"] as Principal;

            return principal != null && principal.IsInRole(Permission.See);
        }
    }
}
