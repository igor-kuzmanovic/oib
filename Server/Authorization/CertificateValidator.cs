using System;
using System.IdentityModel.Selectors;
using System.Security.Cryptography.X509Certificates;

public class CertificateValidator : X509CertificateValidator
{
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
                throw new Exception("[CertificateValidator] Certificate chain build failed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CertificateValidator] Certificate validation exception: {ex.Message}");
            throw;
        }

        Console.WriteLine("[CertificateValidator] Validate success");
    }
}
