using Contracts.Helpers;
using Server.Audit;
using System;
using System.IdentityModel.Selectors;
using System.Security.Cryptography.X509Certificates;

namespace Server.Authorization
{
    public class CertificateValidator : X509CertificateValidator
    {
        private readonly X509Certificate2 expectedCertificate;

        public CertificateValidator(X509Certificate2 expectedCertificate)
        {
            this.expectedCertificate = expectedCertificate;
        }

        public override void Validate(X509Certificate2 certificate)
        {
            Console.WriteLine("[CertificateValidator] Validate called");

            try
            {
                Console.WriteLine($"[CertificateValidator] Validating certificate: Subject='{certificate.Subject}', Thumbprint='{certificate.Thumbprint}'");

                X509Chain chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                bool isValid = chain.Build(certificate);

                Console.WriteLine($"[CertificateValidator] Chain build result: {isValid}");

                if (!isValid)
                {
                    Console.WriteLine("[CertificateValidator] Certificate validation failed. Chain statuses:");
                    foreach (var status in chain.ChainStatus)
                    {
                        Console.WriteLine($"[CertificateValidator] - {status.StatusInformation.Trim()}");
                    }
                    throw new Exception("Certificate chain build failed.");
                }

                if (chain.ChainElements.Count > 0 && chain.ChainElements[0].Certificate.Subject == chain.ChainElements[chain.ChainElements.Count - 1].Certificate.Subject)
                {
                    throw new Exception("Certificate is self-signed.");
                }

                if (expectedCertificate != null)
                {
                    Console.WriteLine($"[CertificateValidator] Expected certificate: Subject='{expectedCertificate.Subject}', Thumbprint='{expectedCertificate.Thumbprint}'");
                    if (!certificate.Thumbprint.Equals(expectedCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception("Certificate does not match expected endpoint identity.");
                    }
                    Console.WriteLine("[CertificateValidator] Certificate matches expected endpoint identity.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CertificateValidator] Certificate validation exception: {ex.Message}");
                AuditFacade.AuthenticationFailed(SecurityHelper.GetName(certificate), $"Certificate validation exception: {ex.Message}");
                throw;
            }

            Console.WriteLine("[CertificateValidator] Validate success");
            AuditFacade.AuthenticationSuccess(SecurityHelper.GetName(certificate));
        }
    }
}
