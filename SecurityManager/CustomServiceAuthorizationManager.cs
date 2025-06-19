using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;

namespace SecurityManager
{
    public class CustomServiceAuthorizationManager : ServiceAuthorizationManager
    {
        protected override bool CheckAccessCore(OperationContext operationContext)
        {
            CustomPrincipal principal = operationContext.ServiceSecurityContext.
                AuthorizationContext.Properties["Principal"] as CustomPrincipal;

            bool retValue = principal.IsInRole("Read");

            if (!retValue)
            {
                try
                {
                    Audit.AuthorizationFailed(Formatter.ParseName(principal.Identity.Name),
                        OperationContext.Current.IncomingMessageHeaders.Action, "Need Read permission.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return retValue;
        }
    }
}
