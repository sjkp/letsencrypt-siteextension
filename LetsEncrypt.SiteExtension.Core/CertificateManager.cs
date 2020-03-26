using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core.Services;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
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
            string storageAccount = AuthorizationChallengeBlobStorageAccount();

            if (string.IsNullOrEmpty(storageAccount))
            {
                this.challengeProvider = new KuduFileSystemAuthorizationChallengeProvider(this.settings, new AuthorizationChallengeProviderConfig()
                {
                    DisableWebConfigUpdate = config.DisableWebConfigUpdate
                });
            }
            else
            {
                this.challengeProvider = NewBlobStorageAuthorizationChallengeProvider();
            }
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
        /// Returns a <see cref="CertificateManager"/> configured to use HTTP Challenge, placing the challenge file on Azure Blob Storage,
        /// and assigning the obtained certificate directly to the web app service. 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="acmeConfig"></param>
        /// <param name="certSettings"></param>
        /// <returns></returns>
        public static CertificateManager CreateBlobWebAppCertificateManager(IAzureWebAppEnvironment settings, IAcmeConfig acmeConfig, IWebAppCertificateSettings certSettings)
        {
            return new CertificateManager(settings, acmeConfig, new WebAppCertificateService(settings, certSettings), NewBlobStorageAuthorizationChallengeProvider());
        }

        private static BlobStorageAuthorizationChallengeProvider NewBlobStorageAuthorizationChallengeProvider()
        {
            return new BlobStorageAuthorizationChallengeProvider(AuthorizationChallengeBlobStorageAccount(), ConfigurationManager.AppSettings[AppSettingsAuthConfig.authorizationChallengeBlobStorageContainer]);
        }

        private static string AuthorizationChallengeBlobStorageAccount()
        {
            return ConfigurationManager.AppSettings[AppSettingsAuthConfig.authorizationChallengeBlobStorageAccount];
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

        public async Task<List<CertificateInstallModel>> RenewCertificate(
            bool skipInstallCertificate = false, 
            int renewXNumberOfDaysBeforeExpiration = 0,
            bool throwOnRenewalFailure = true)
        {
            Trace.TraceInformation("Checking certificate");
            var ss = SettingsStore.Instance.Load();
            using (var client = await ArmHelper.GetWebSiteManagementClient(settings))
            using (var httpClient = await ArmHelper.GetHttpClient(settings))
            {
                var retryPolicy = ArmHelper.ExponentialBackoff();
                var body = string.Empty;
                //Cant just get certificates by resource group, because sites that have been moved, have their certs sitting in the old RG.
                //Also cant use client.Certificates.List() due to bug in the nuget
                var response = await retryPolicy.ExecuteAsync(async () =>
                {
                    return await httpClient.GetAsync($"/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Web/certificates?api-version=2016-03-01");
                });
                response.EnsureSuccessStatusCode();
                body = await response.Content.ReadAsStringAsync();
                IEnumerable<Certificate> certs = ExtractCertificates(body);

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
                        BaseUri = this.acmeConfig.BaseUri,
                        UseProduction = !bool.Parse(ss.FirstOrDefault(s => s.Name == "useStaging")?.Value ?? false.ToString()),
                        AlternateNames = sslStates.Skip(1).Select(s => s.Name).ToList(),
                        PFXPassword = this.acmeConfig.PFXPassword,
                        RSAKeyLength = this.acmeConfig.RSAKeyLength

                    };
                    if (!skipInstallCertificate)
                    {
                        try
                        {
                            res.Add(await RequestAndInstallInternalAsync(target));
                        }
                        catch (Exception e) when (throwOnRenewalFailure)
                        {
                            Console.WriteLine($"Error during Request or install certificate {e.ToString()}");
                            Trace.TraceError($"Error during Request or install certificate {e.ToString()}");
                            throw;
                        }
                    }
                }
                return res;
            }
        }

        internal static IEnumerable<Certificate> ExtractCertificates(string body)
        {

            var json = JToken.Parse(body);
            var certs = Enumerable.Empty<Certificate>();
            // Handle issue #269
            if (json.Type == JTokenType.Object && json["value"] != null)
            {
                certs = JsonConvert.DeserializeObject<Certificate[]>(json["value"].ToString(), JsonHelper.DefaultSerializationSettings);
            }
            else
            {
                certs = JsonConvert.DeserializeObject<Certificate[]>(body, JsonHelper.DefaultSerializationSettings);
            }

            return certs;
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
            try
            {
                var model = await RequestInternalAsync(config);
                await this.certificateService.Install(model);
                return model;

            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during Request or install certificate {e.ToString()}");
                Trace.TraceError($"Error during Request or install certificate {e.ToString()}");
                throw;
            }
        }

        public async Task<List<string>> Cleanup()
        {
            return await this.certificateService.RemoveExpired();
        }
    }
}
