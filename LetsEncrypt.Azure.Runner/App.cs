using LetsEncrypt.Azure.Core.V2;
using LetsEncrypt.Azure.Core.V2.CertificateStores;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Runner
{
    public class App
    {
        private readonly AcmeClient acmeClient;
        private readonly ICertificateStore certificateStore;
        private readonly AzureWebAppService azureWebAppService;
        private readonly ILogger<App> logger;

        public App(AcmeClient acmeClient, ICertificateStore certificateStore, AzureWebAppService azureWebAppService, ILogger<App> logger = null)
        {
            this.acmeClient = acmeClient;
            this.certificateStore = certificateStore;
            this.azureWebAppService = azureWebAppService;
            this.logger = logger ?? NullLogger<App>.Instance;
        }
        public async Task Run(AcmeDnsRequest acmeDnsRequest, int renewXNumberOfDaysBeforeExpiration)
        {
            try
            {
                CertificateInstallModel model = null;

                var certname = acmeDnsRequest.Host + "-" + acmeDnsRequest.AcmeEnvironment.Name + ".pfx";
                CertificateInfo cert = await certificateStore.GetCertificate(certname, acmeDnsRequest.PFXPassword);
                if (cert == null || cert.Certificate.NotAfter < DateTime.UtcNow.AddDays(renewXNumberOfDaysBeforeExpiration)) //Cert doesnt exist or expires in less than 21 days, lets renew.
                {
                    logger.LogInformation("Certificate store didn't contain certificate or certificate was expired starting renewing");
                    model = await acmeClient.RequestDnsChallengeCertificate(acmeDnsRequest);
                    model.CertificateInfo.Name = certname;
                    await certificateStore.SaveCertificate(model.CertificateInfo);
                }
                else
                {
                    logger.LogInformation("Certificate expires in more than {renewXNumberOfDaysBeforeExpiration} days, reusing certificate from certificate store", renewXNumberOfDaysBeforeExpiration);
                    model = new CertificateInstallModel()
                    {
                        CertificateInfo = cert,
                        Host = acmeDnsRequest.Host
                    };
                }
                await azureWebAppService.Install(model);

                logger.LogInformation("Removing expired certificates");
                System.Collections.Generic.List<string> expired = azureWebAppService.RemoveExpired();
                logger.LogInformation("The following certificates was removed {Thumbprints}", string.Join(", ", expired.ToArray()));

            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed");
                throw;
            }
        }
    }
}
