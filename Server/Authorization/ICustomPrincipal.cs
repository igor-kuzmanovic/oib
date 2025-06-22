using System.Security.Principal;
using Contracts.Authorization;

namespace Server.Authorization
{
    public interface ICustomPrincipal : IPrincipal
    {
        bool IsInRole(Permission permission);
    }
}
