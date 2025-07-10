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
        public static X509Certificate2 GetCertificate(ChannelFactory factory)
        {
            X509Certificate2 certificate = new X509Certificate2();

            certificate = factory.Credentials.ClientCertificate.Certificate;

            return certificate;
        }

        public static X509Certificate2 GetCertificate(OperationContext context)
        {
            X509Certificate2 certificate = new X509Certificate2();

            certificate = (context.ServiceSecurityContext.AuthorizationContext.ClaimSets[0] as X509CertificateClaimSet).X509Certificate;

            return certificate;
        }

        public static X509Certificate2 GetCertificate()
        {
            string currentUser = ParseName(WindowsIdentity.GetCurrent().Name);

            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                foreach (var cert in store.Certificates)
                {
                    string subject = cert.Subject;

                    string cn = null;
                    var parts = subject.Split(',');
                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();
                        if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                        {
                            cn = trimmed.Substring(3).Trim();
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(cn))
                        continue;

                    if (cn.IndexOf(currentUser, StringComparison.OrdinalIgnoreCase) >= 0)
                        return cert;
                }

                return null;
            }
        }

        public static string GetName(X509Certificate2 certificate)
        {
            string name = string.Empty;

            string subjectName = certificate.SubjectName.Name;

            string[] subjectAttributes = subjectName.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (subjectAttributes.Any(s => s.Contains("CN=")))
            {
                string commonName = subjectAttributes.First(s => s.StartsWith("CN="));

                name = commonName.Substring("CN=".Length);
            }

            return name;
        }

        public static string GetName(OperationContext context)
        {
            string name = string.Empty;

            WindowsIdentity identity = context.ServiceSecurityContext.WindowsIdentity;

            name = GetName(identity);

            return name;
        }

        public static string GetName(WindowsIdentity identity)
        {
            string name = string.Empty;

            name = ParseName(identity.Name);

            return name;
        }

        public static HashSet<string> GetOrganizationalUnits(X509Certificate2 certificate)
        {
            HashSet<string> orgUnitSet = new HashSet<string>();

            string subjectName = certificate.SubjectName.Name;

            string[] subjectAttributes = subjectName.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (subjectAttributes.Any(s => s.Contains("OU=")))
            {
                string orgUnitsAttribute = subjectAttributes.First(s => s.StartsWith("OU="));

                string orgUnitsValues = orgUnitsAttribute.Substring("OU=".Length);

                string[] orgUnits = orgUnitsValues.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string orgUnit in orgUnits)
                {
                    orgUnitSet.Add(orgUnit);
                }
            }

            return orgUnitSet;
        }

        public static string ParseName(string windowsName)
        {
            string name = string.Empty;

            if (windowsName.Contains("@"))
            {
                name = windowsName.Split('@')[0];
            }
            else if (windowsName.Contains("\\"))
            {
                name = windowsName.Split('\\')[1];
            }
            else
            {
                name = windowsName;
            }

            return name;
        }
    }
}
