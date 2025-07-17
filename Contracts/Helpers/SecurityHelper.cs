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
            Console.WriteLine($"[SecurityHelper] GetCertificate called with StoreName={storeName}, StoreLocation={storeLocation}, subjectName={subjectName}");
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                Console.WriteLine($"[SecurityHelper] Opened certificate store {storeName} at {storeLocation}");

                var certCollection = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);
                Console.WriteLine($"[SecurityHelper] Found {certCollection.Count} certificates matching subject name '{subjectName}'");

                var cert = certCollection
                    .OfType<X509Certificate2>()
                    .FirstOrDefault(c =>
                    {
                        var match = string.Equals(c.SubjectName.Name, $"CN={subjectName}", StringComparison.OrdinalIgnoreCase);
                        Console.WriteLine($"[SecurityHelper] Checking cert SubjectName.Name='{c.SubjectName.Name}' against 'CN={subjectName}': {match}");
                        return match;
                    });

                if (cert != null)
                {
                    Console.WriteLine($"[SecurityHelper] Returning certificate with Thumbprint: {cert.Thumbprint}");
                }
                else
                {
                    Console.WriteLine($"[SecurityHelper] No matching certificate found");
                }

                return cert;
            }
        }

        public static X509Certificate2 GetCertificate(string subjectName)
        {
            Console.WriteLine($"[SecurityHelper] GetCertificate called with subjectName='{subjectName}'");
            return GetCertificate(StoreName.My, StoreLocation.LocalMachine, subjectName);
        }

        public static X509Certificate2 GetCertificate()
        {
            string subjectName = ParseName(WindowsIdentity.GetCurrent().Name);
            Console.WriteLine($"[SecurityHelper] GetCurrentUserCertificate: current Windows identity parsed as subjectName='{subjectName}'");

            var cert = GetCertificate(StoreName.My, StoreLocation.LocalMachine, subjectName);

            if (cert != null && cert.HasPrivateKey)
            {
                Console.WriteLine($"[SecurityHelper] Certificate found and has private key. Thumbprint: {cert.Thumbprint}");
                return cert;
            }
            else
            {
                Console.WriteLine($"[SecurityHelper] Certificate not found or missing private key.");
                return null;
            }
        }

        public static string GetName(X509Certificate2 certificate)
        {
            var subject = certificate.SubjectName.Name;
            Console.WriteLine($"[SecurityHelper] GetName from certificate: SubjectName.Name='{subject}'");

            var cn = subject?.Split(',')
                          .Select(s => s.Trim())
                          .FirstOrDefault(s => s.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                          ?.Substring(3);

            Console.WriteLine($"[SecurityHelper] Parsed CN from certificate subject: '{cn}'");
            return cn;
        }

        public static string GetName(OperationContext context)
        {
            Console.WriteLine("[SecurityHelper] GetName from OperationContext called");
            var name = GetName(context.ServiceSecurityContext.WindowsIdentity);
            Console.WriteLine($"[SecurityHelper] Got name from OperationContext WindowsIdentity: '{name}'");
            return name;
        }

        public static string GetName(IIdentity identity)
        {
            Console.WriteLine($"[SecurityHelper] GetName from Identity: Name='{identity.Name}'");
            var name = ParseName(identity.Name);
            Console.WriteLine($"[SecurityHelper] Parsed name from Identity: '{name}'");
            return name;
        }

        public static string ParseName(string windowsName)
        {
            Console.WriteLine($"[SecurityHelper] ParseName called with windowsName='{windowsName}'");

            if (windowsName.Contains("@"))
            {
                var parsed = windowsName.Split('@')[0];
                Console.WriteLine($"[SecurityHelper] Parsed name (before @): '{parsed}'");
                return parsed;
            }

            if (windowsName.Contains("\\"))
            {
                var parsed = windowsName.Split('\\')[1];
                Console.WriteLine($"[SecurityHelper] Parsed name (after \\): '{parsed}'");
                return parsed;
            }

            Console.WriteLine($"[SecurityHelper] Returning original windowsName: '{windowsName}'");
            return windowsName;
        }
    }
}
