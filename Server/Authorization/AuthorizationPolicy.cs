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
            Console.WriteLine("[AuthorizationPolicy] 'Evaluate' called");

            if (!evaluationContext.Properties.TryGetValue("Identities", out object list))
            {
                Console.WriteLine("[AuthorizationPolicy] Context has no 'identities' property");
                return false;
            }

            if (!(list is IList<IIdentity> identities) || identities.Count <= 0)
            {
                Console.WriteLine("[AuthorizationPolicy] Context has no identities");
                return false;
            }

            if (!(identities[0] is WindowsIdentity windowsIdentity))
            {
                Console.WriteLine("[AuthorizationPolicy] First identity is not a Windows identity");
                return false;
            }

            Principal principal = new Principal(windowsIdentity);
            Console.WriteLine($"[AuthorizationPolicy] Setting principal {principal.Identity.Name}");
            evaluationContext.Properties["Principal"] = principal;

            Console.WriteLine("[AuthorizationPolicy] 'Evaluate' success");
            return true;
        }
    }
}
