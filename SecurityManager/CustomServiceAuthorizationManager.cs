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

            System.Threading.Thread.CurrentPrincipal = principal;

            bool retValue = principal.IsInRole("See");

            if (!retValue)
            {
                try
                {
                    string serviceAddress = OperationContext.Current?.IncomingMessageHeaders?.To?.ToString() ?? "Authorization";
                    Audit.AuthorizationFailed(Formatter.ParseName(principal.Identity.Name),
                        OperationContext.Current.IncomingMessageHeaders.Action, "Need See permission.", serviceAddress);
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
