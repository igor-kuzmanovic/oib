using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;

namespace Contracts.Helpers
{
    public static class SecurityHelper
    {
        public static X509Certificate2 GetCertificate(StoreName storeName, StoreLocation storeLocation, string subjectName)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                var certCollection = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);

                return certCollection
                    .OfType<X509Certificate2>()
                    .FirstOrDefault(cert => string.Equals(cert.SubjectName.Name, $"CN={subjectName}", StringComparison.OrdinalIgnoreCase));
            }
        }

        public static X509Certificate2 GetCurrentUserCertificate()
        {
            string subjectName = ParseName(WindowsIdentity.GetCurrent().Name);
            var cert = GetCertificate(StoreName.My, StoreLocation.LocalMachine, subjectName);
            return (cert != null && cert.HasPrivateKey) ? cert : null;
        }

        public static string GetName(X509Certificate2 certificate)
        {
            var subject = certificate.SubjectName.Name;
            return subject?.Split(',')
                          .Select(s => s.Trim())
                          .FirstOrDefault(s => s.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                          ?.Substring(3);
        }

        public static string GetName(OperationContext context)
        {
            return GetName(context.ServiceSecurityContext.WindowsIdentity);
        }

        public static string GetName(WindowsIdentity identity)
        {
            return ParseName(identity.Name);
        }

        public static string ParseName(string windowsName)
        {
            if (windowsName.Contains("@"))
                return windowsName.Split('@')[0];
            if (windowsName.Contains("\\"))
                return windowsName.Split('\\')[1];
            return windowsName;
        }
    }
}
