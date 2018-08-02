using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.V2;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Letsencrypt.Azure.Core.Test
{
    [TestClass]
    public class AcmeClientTest
    {
        private readonly ILogger<AcmeClient> logger;

        public AcmeClientTest()
        {

        }

        public AcmeClientTest(ILogger<AcmeClient> logger)
        {
            this.logger = logger;
        }
        [TestMethod]
        public async Task TestEndToEndAzure()
        {
            var config = TestHelper.AzureDnsSettings;

            var manager = new AcmeClient(new AzureDnsProvider(config), new DnsLookupService(), null, this.logger);

            var dnsRequest = new AcmeDnsRequest()
            {
                Host = "*.ai4bots.com",
                PFXPassword = "Pass@word",
                RegistrationEmail = "mail@sjkp.dk",
                AcmeEnvironment = new LetsEncryptStagingV2(),
                CsrInfo = new CsrInfo()
                {
                    CountryName = "DK",
                    Locality = "DK",
                    Organization = "SJKP",
                    OrganizationUnit = "",
                    State = "DK"
                }
            };

            var res = await manager.RequestDnsChallengeCertificate(dnsRequest);

            Assert.IsNotNull(res);

            File.WriteAllBytes($"{dnsRequest.Host.Substring(2)}.pfx", res.CertificateInfo.PfxCertificate);

            var pass = new System.Security.SecureString();
            Array.ForEach(dnsRequest.PFXPassword.ToCharArray(), c =>
            {
                pass.AppendChar(c);
            });
            File.WriteAllBytes($"exported-{dnsRequest.Host.Substring(2)}.pfx", res.CertificateInfo.Certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, pass));


            var certService = new AzureWebAppService(new[] { TestHelper.AzureWebAppSettings });

            await certService.Install(res);
        }

        [TestMethod]
        public async Task TestEndToEndUnoEuro()
        {
           
            var dnsProvider = TestHelper.UnoEuroDnsProvider;

            var manager = new AcmeClient(dnsProvider, new DnsLookupService());

            var dnsRequest = new AcmeDnsRequest()
            {
                Host = "*.tiimo.dk",
                PFXPassword = "Pass@word",
                RegistrationEmail = "mail@sjkp.dk",
                AcmeEnvironment = new LetsEncryptStagingV2(),
                CsrInfo = new CsrInfo()
                {
                    CountryName = "DK",
                    Locality = "Copenhagen",
                    Organization = "tiimo ApS",
                    OrganizationUnit = "",
                    State = "DK"
                }
            };

            var res = await manager.RequestDnsChallengeCertificate(dnsRequest);

            Assert.IsNotNull(res);

            File.WriteAllBytes($"{dnsRequest.Host.Substring(2)}.pfx", res.CertificateInfo.PfxCertificate);
        }


        [TestMethod]
        public async Task TestEndToEndGoDaddy()
        {

            var dnsProvider = new GoDaddyDnsProviderTest().DnsService;

            var manager = new AcmeClient(dnsProvider, new DnsLookupService());

            var dnsRequest = new AcmeDnsRequest()
            {
                Host = "*.åbningstider.info",
                PFXPassword = "Pass@word",
                RegistrationEmail = "mail@sjkp.dk",
                AcmeEnvironment = new LetsEncryptStagingV2(),
                CsrInfo = new CsrInfo()
                {
                    CountryName = "DK",
                    Locality = "Copenhagen",
                    Organization = "Sjkp",
                    OrganizationUnit = "",
                    State = "DK"
                }
            };

            var res = await manager.RequestDnsChallengeCertificate(dnsRequest);

            Assert.IsNotNull(res);

            File.WriteAllBytes($"{dnsRequest.Host.Substring(2)}.pfx", res.CertificateInfo.PfxCertificate);

            var certService = new AzureWebAppService(new[] { TestHelper.AzureWebAppSettings });

            await certService.Install(res);
        }
    }
}
