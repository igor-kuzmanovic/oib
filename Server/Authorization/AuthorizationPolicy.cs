using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Security.Principal;
using Contracts.Helpers;

namespace Server.Authorization
{
    public class AuthorizationPolicy : IAuthorizationPolicy
    {
        public AuthorizationPolicy()
        {
            Id = Guid.NewGuid().ToString();
        }

        public ClaimSet Issuer => ClaimSet.System;

        public string Id { get; }

        public bool Evaluate(EvaluationContext evaluationContext, ref object state)
        {
            if (!evaluationContext.Properties.TryGetValue("Identities", out object list))
            {
                return false;
            }

            if (!(list is IList<IIdentity> identities) || identities.Count <= 0)
            {
                return false;
            }

            if (!(identities[0] is WindowsIdentity windowsIdentity))
            {
                return false;
            }

            Principal principal = new Principal(windowsIdentity);
            evaluationContext.Properties["Principal"] = principal;

            return true;
        }
    }
}
