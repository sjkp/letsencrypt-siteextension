using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core.Services;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core
{
    public class CertificateManager
    {
        private readonly ICertificateService certificateService;
        private readonly IAzureWebAppEnvironment settings;
        private readonly IAcmeConfig acmeConfig;
        private readonly IAuthorizationChallengeProvider challengeProvider;

        /// <summary>
        /// For backwards compatability
        /// </summary>
        /// <param name="config"></param>
        public CertificateManager(AppSettingsAuthConfig config)
        {
            
            this.settings = config;
            this.acmeConfig = config;
            this.challengeProvider = new KuduFileSystemAuthorizationChallengeProvider(this.settings, new AuthorizationChallengeProviderConfig()
            {
                DisableWebConfigUpdate = config.DisableWebConfigUpdate
            });
            this.certificateService = new WebAppCertificateService(this.settings, new CertificateServiceSettings()
            {
                UseIPBasedSSL = config.UseIPBasedSSL
            });

        } 

        public CertificateManager(IAzureWebAppEnvironment settings, IAcmeConfig acmeConfig, ICertificateService certificateService, IAuthorizationChallengeProvider challengeProvider)
        {
            this.settings = settings;
            this.certificateService = certificateService;
            this.acmeConfig = acmeConfig;
            this.challengeProvider = challengeProvider;
        }

        /// <summary>
        /// Returns a <see cref="CertificateManager"/> configured to use HTTP Challenge, placing the challenge file on Azure Web App 
        /// using Kudu, and assigning the obtained certificate directly to the web app service. 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="acmeConfig"></param>
        /// <param name="certSettings"></param>
        /// <param name="authProviderConfig"></param>
        /// <returns></returns>
        public static CertificateManager CreateKuduWebAppCertificateManager(IAzureWebAppEnvironment settings, IAcmeConfig acmeConfig, IWebAppCertificateSettings certSettings, IAuthorizationChallengeProviderConfig authProviderConfig)
        {
            return new CertificateManager(settings, acmeConfig, new WebAppCertificateService(settings, certSettings), new KuduFileSystemAuthorizationChallengeProvider(settings, authProviderConfig));
        }

        /// <summary>
        /// Returns a <see cref="CertificateManager"/> configured to use DNS Challenge, placing the challenge record in Azure DNS,
        /// and assigning the obtained certificate directly to the web app service. 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="acmeConfig"></param>
        /// <param name="certSettings"></param>
        /// <param name="dnsEnvironment"></param>
        /// <returns></returns>
        public static CertificateManager CreateAzureDnsWebAppCertificateManager(IAzureWebAppEnvironment settings, IAcmeConfig acmeConfig, IWebAppCertificateSettings certSettings, IAzureDnsEnvironment dnsEnvironment)
        {
            return new CertificateManager(settings, acmeConfig, new WebAppCertificateService(settings, certSettings), new AzureDnsAuthorizationChallengeProvider(dnsEnvironment));
        }

        /// <summary>
        /// Request a certificate from lets encrypt using the DNS challenge, placing the challenge record in Azure DNS. 
        /// The certifiacte is not assigned, but just returned. 
        /// </summary>
        /// <param name="azureDnsEnvironment"></param>
        /// <param name="acmeConfig"></param>
        /// <returns></returns>
        public static async Task<CertificateInstallModel> RequestDnsChallengeCertificate(IAzureDnsEnvironment azureDnsEnvironment, IAcmeConfig acmeConfig)
        {
            return await new CertificateManager(null, acmeConfig, null, new AzureDnsAuthorizationChallengeProvider(azureDnsEnvironment)).RequestInternalAsync(acmeConfig);
        }


        /// <summary>
        /// Used for automatic installation of letsencrypt certificate 
        /// </summary>
        public async Task<CertificateInstallModel> AddCertificate()
        {
            Trace.TraceInformation("Staring add certificate");
            using (var client = ArmHelper.GetWebSiteManagementClient(settings))
            {
                Trace.TraceInformation($"Add certificate for acmeConfig hostname {string.Join(", ", acmeConfig.Hostnames)}");

                if (acmeConfig.Hostnames.Any())
                {
                    return await RequestAndInstallInternalAsync(this.acmeConfig);
                }
                else
                {
                    Trace.TraceWarning("No hostnames found in configuration cannot add certificate automatically. Please run the manual configuration, or provide the all required app settings for automated deployment and delete firstrun.job in letsencrypt in the blob storage account to enable the job to be rerun.");
                }

            }
            return null;
        }

        public async Task<List<CertificateInstallModel>> RenewCertificate(bool skipInstallCertificate = false, int renewXNumberOfDaysBeforeExpiration = 0)
        {
            Trace.TraceInformation("Checking certificate");
            var ss = SettingsStore.Instance.Load();
            using (var client = ArmHelper.GetWebSiteManagementClient(settings))
            using (var httpClient = ArmHelper.GetHttpClient(settings))
            {
                //Cant just get certificates by resource group, because sites that have been moved, have their certs sitting in the old RG.
                //Also cant use client.Certificates.List() due to bug in the nuget
                var response = await httpClient.GetAsync($"/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Web/certificates?api-version=2016-03-01");
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                IEnumerable<Certificate> certs = JsonConvert.DeserializeObject<Certificate[]>(body, JsonHelper.DefaultSerializationSettings);

                var expiringCerts = certs.Where(s => s.ExpirationDate < DateTime.UtcNow.AddDays(renewXNumberOfDaysBeforeExpiration) && (s.Issuer.Contains("Let's Encrypt") || s.Issuer.Contains("Fake LE")));

                if (expiringCerts.Count() == 0)
                {
                    Trace.TraceInformation(string.Format("No certificates installed issued by Let's Encrypt that are about to expire within the next {0} days. Skipping.", renewXNumberOfDaysBeforeExpiration));
                }
                var res = new List<CertificateInstallModel>();
                foreach (var toExpireCert in expiringCerts)
                {
                    Trace.TraceInformation("Starting renew of certificate " + toExpireCert.Name + " expiration date " + toExpireCert.ExpirationDate);
                    var site = client.WebApps.GetSiteOrSlot(settings.ResourceGroupName, settings.WebAppName, settings.SiteSlotName);
                    var sslStates = site.HostNameSslStates.Where(s => s.Thumbprint == toExpireCert.Thumbprint);
                    if (!sslStates.Any())
                    {
                        Trace.TraceInformation(String.Format("Certificate {0} was not assigned any hostname, skipping update", toExpireCert.Thumbprint));
                        continue;
                    }
                    var target = new AcmeConfig()
                    {
                     
                        RegistrationEmail = this.acmeConfig.RegistrationEmail ?? ss.FirstOrDefault(s => s.Name == "email").Value,
                        Host = sslStates.First().Name,
                        BaseUri = this.acmeConfig.BaseUri ?? ss.FirstOrDefault(s => s.Name == "baseUri").Value,                        
                        AlternateNames = sslStates.Skip(1).Select(s => s.Name).ToList(),
                        PFXPassword = this.acmeConfig.PFXPassword,
                        RSAKeyLength = this.acmeConfig.RSAKeyLength
                        
                    };
                    if (!skipInstallCertificate)
                    {
                        res.Add(await RequestAndInstallInternalAsync(target));
                    }                    
                }
                return res;
            }
        }             


        internal CertificateInstallModel RequestAndInstallInternal(IAcmeConfig config)
        {
           return RequestAndInstallInternalAsync(config).GetAwaiter().GetResult();
        }

        internal async Task<CertificateInstallModel> RequestInternalAsync(IAcmeConfig config)
        {
            var service = new AcmeService(config, this.challengeProvider);

            var cert = await service.RequestCertificate();
            var model = new CertificateInstallModel()
            {
                CertificateInfo = cert,
                AllDnsIdentifiers = config.Hostnames.ToList(),
                Host = config.Host,
            };
            return model;
        }

        internal async Task<CertificateInstallModel> RequestAndInstallInternalAsync(IAcmeConfig config)
        {
            Trace.TraceInformation("RequestAndInstallInternal");
            var model = await RequestInternalAsync(config);
            this.certificateService.Install(model);
            return model;
        }

        public List<string> Cleanup()
        {
            return this.certificateService.RemoveExpired();
        }
    }
}
