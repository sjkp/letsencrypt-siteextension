using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using DnsClient;
using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core.V2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core
{
    public class CertificateManager
    {
        private readonly IDnsProviderService dnsProviderService;
        private readonly DnsLookupService dnsLookupService;

        public CertificateManager(IDnsProviderService dnsService, DnsLookupService dnsLookupService)
        {
            this.dnsProviderService = dnsService;
            this.dnsLookupService = dnsLookupService;
        }
        /// <summary>
        /// Request a certificate from lets encrypt using the DNS challenge, placing the challenge record in Azure DNS. 
        /// The certifiacte is not assigned, but just returned. 
        /// </summary>
        /// <param name="azureDnsEnvironment"></param>
        /// <param name="acmeConfig"></param>
        /// <returns></returns>
        public async Task<CertificateInstallModel> RequestDnsChallengeCertificatev2(IAzureDnsEnvironment azureDnsEnvironment, IAcmeConfig acmeConfig)
        { 
            var uri = GetAcmeDirectoryUri(acmeConfig);
            var acmeContext = GetOrCreateAcmeContext(uri, acmeConfig.RegistrationEmail);


            var order = await acmeContext.NewOrder(acmeConfig.Hostnames.ToArray());
            var a = await order.Authorizations();
            var authz = a.First();
            var challenge = await authz.Dns();
            var dnsTxt = acmeContext.AccountKey.DnsTxt(challenge.Token);
            Console.WriteLine(dnsTxt);

            ///add dns entry
            await this.dnsProviderService.PersistChallenge("_acme-challenge", dnsTxt);

            if (!(await this.dnsLookupService.Exists(acmeConfig.Host, dnsTxt)))
            {
                throw new Exception("Unable to validate that _acme-challenge was persist stored in txt record");
            }

            Challenge chalResp = await challenge.Validate();
            while (chalResp.Status == ChallengeStatus.Pending || chalResp.Status == ChallengeStatus.Processing)
            {
                await Task.Delay(5000);
                chalResp = await challenge.Resource();
            }



            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var cert = await order.Generate(new CsrInfo
            {
                CountryName = "DK",
                State = "Copenhagen",
                Locality = "Copenhagen",
                Organization = "tiimo",
                OrganizationUnit = "",
                CommonName = "*.tiimoapp.com",
            }, privateKey);

            var certPem = cert.ToPem();

            var pfxBuilder = cert.ToPfx(privateKey);
            var pfx = pfxBuilder.Build("tiimoappcert", "tiimo");

            File.WriteAllBytes("tiimoappcert.pfx", pfx);

            await this.dnsProviderService.Cleanup(dnsTxt);

            return null;
        }

        private static Uri GetAcmeDirectoryUri(IAcmeConfig acmeConfig)
        {
            if (!string.IsNullOrEmpty(acmeConfig.BaseUri))
            {
                return new Uri(acmeConfig.BaseUri);
            }

            if (acmeConfig.UseProduction)
            {
                return WellKnownServers.LetsEncryptV2;
            }
            else
            {
                return WellKnownServers.LetsEncryptStagingV2;
            }            
        }

        private static AcmeContext GetOrCreateAcmeContext(Uri acmeDirectoryUri, string email)
        {
            AcmeContext acme = null;
            if (!File.Exists("account.pem"))
            {
                acme = new AcmeContext(acmeDirectoryUri);
                var account = acme.NewAccount(email, true);

                // Save the account key for later use
                var pemKey = acme.AccountKey.ToPem();
                File.WriteAllText("account.pem", pemKey);
            }
            else
            {
                var pemKey = File.ReadAllText("account.pem");
                var accountKey = KeyFactory.FromPem(pemKey);
                acme = new AcmeContext(acmeDirectoryUri, accountKey, new AcmeHttpClient(acmeDirectoryUri, new HttpClient()));
            }

            return acme;
        }

        
        

    }
}
