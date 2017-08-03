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
        private readonly IAzureEnvironment settings;
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
            this.certificateService = new CertificateService(this.settings, new CertificateServiceSettings()
            {
                UseIPBasedSSL = config.UseIPBasedSSL
            });

        }

        public CertificateManager(IAzureEnvironment settings, IAcmeConfig acmeConfig, ICertificateServiceSettings certSettings, IAuthorizationChallengeProviderConfig authProviderConfig) 
            : this(settings, acmeConfig, new CertificateService(settings, certSettings), new KuduFileSystemAuthorizationChallengeProvider(settings, authProviderConfig))
        {

        }

        public CertificateManager(IAzureEnvironment settings, IAcmeConfig acmeConfig, ICertificateService certificateService, IAuthorizationChallengeProvider challengeProvider)
        {
            this.settings = settings;
            this.certificateService = certificateService;
            this.acmeConfig = acmeConfig;
            this.challengeProvider = challengeProvider;
        }


        /// <summary>
        /// Used for automatic installation of letsencrypt certificate 
        /// </summary>
        public bool AddCertificate()
        {
            Trace.TraceInformation("Staring add certificate");
            using (var client = ArmHelper.GetWebSiteManagementClient(settings))
            {
                Trace.TraceInformation($"Add certificate for acmeConfig hostname {string.Join(", ", acmeConfig.Hostnames)}");

                if (acmeConfig.Hostnames.Any())
                {
                    return RequestAndInstallInternal(this.acmeConfig) != null;
                }
                else
                {
                    Trace.TraceWarning("No hostnames found in configuration cannot add certificate automatically. Please run the manual configuration, or provide the all required app settings for automated deployment and delete firstrun.job in letsencrypt in the blob storage account to enable the job to be rerun.");
                }

            }
            return false;
        }

        public async Task<List<AcmeConfig>> RenewCertificate(bool skipInstallCertificate = false, int renewXNumberOfDaysBeforeExpiration = 0)
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
                var res = new List<AcmeConfig>();
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
                        await RequestAndInstallInternalAsync(target);
                    }
                    res.Add(target);
                }
                return res;
            }
        }             


        internal string RequestAndInstallInternal(IAcmeConfig config)
        {
           return RequestAndInstallInternalAsync(config).GetAwaiter().GetResult();
        }

        internal async Task<string> RequestAndInstallInternalAsync(IAcmeConfig config)
        {
            try
            {
                Trace.TraceInformation("RequestAndInstallInternal");
                var service = new AcmeService(config, this.challengeProvider);

                var cert = await service.RequestCertificate();
                this.certificateService.Install(new CertificateInstallModel()
                {
                    CertificateInfo = cert,
                    AllDnsIdentifiers = config.Hostnames.ToList(),
                    Host = config.Host,
                });
                return cert.Certificate.Thumbprint;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unabled to create Azure Web Site Management client " + ex.ToString());
                throw;
            }
        }        
       
        public List<string> Cleanup()
        {
            return this.certificateService.RemoveExpired();
        }
    }
}
