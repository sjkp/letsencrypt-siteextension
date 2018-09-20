using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LetsEncrypt.Azure.Core.V2
{
    public class AzureWebAppService
    {
        private readonly AzureWebAppSettings[] settings;
        private readonly ILogger<AzureWebAppService> logger;

        public AzureWebAppService(AzureWebAppSettings[] settings, ILogger<AzureWebAppService> logger = null)
        {
            this.settings = settings;
            this.logger = logger ?? NullLogger<AzureWebAppService>.Instance;
        }
        public async Task Install(ICertificateInstallModel model)
        {
            logger.LogInformation("Starting installation of certificate {Thumbprint} for {Host}", model.CertificateInfo.Certificate.Thumbprint, model.Host);
            var cert = model.CertificateInfo;
            foreach (var setting in this.settings)
            {
                logger.LogInformation("Installing certificate for web app {WebApp}", setting.WebAppName);
                try
                {
                    IAppServiceManager appServiceManager = GetAppServiceManager(setting);
                    var s = appServiceManager.WebApps.GetByResourceGroup(setting.ResourceGroupName, setting.WebAppName);
                    IWebAppBase siteOrSlot = s;
                    if (!string.IsNullOrEmpty(setting.SiteSlotName))
                    {
                        var slot = s.DeploymentSlots.GetByName(setting.SiteSlotName);
                        siteOrSlot = slot;
                    }

                    var existingCerts = await appServiceManager.AppServiceCertificates.ListByResourceGroupAsync(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName);
                    if (existingCerts.Where(_=> _.RegionName == s.RegionName).All(_ => _.Thumbprint != cert.Certificate.Thumbprint))
                    {
                        await appServiceManager.AppServiceCertificates.Define($"{cert.Certificate.Thumbprint}-{model.Host}-{s.RegionName}").WithRegion(s.RegionName).WithExistingResourceGroup(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName).WithPfxByteArray(model.CertificateInfo.PfxCertificate).WithPfxPassword(model.CertificateInfo.Password).CreateAsync();
                    }



                    var sslStates = siteOrSlot.HostNameSslStates;
                    var domainSslMappings = new List<KeyValuePair<string, HostNameSslState>>(sslStates.Where(_ => _.Key.Contains($".{model.Host.Substring(2)}")));

                    if (domainSslMappings.Any())
                    {
                        foreach (var domainMapping in domainSslMappings)
                        {

                            string hostName = domainMapping.Value.Name;
                            if (domainMapping.Value.Thumbprint == cert.Certificate.Thumbprint)
                                continue;
                            logger.LogInformation("Binding certificate {Thumbprint} to {Host}", model.CertificateInfo.Certificate.Thumbprint, hostName);
                            var binding = new HostNameBindingInner()
                            {
                                SslState = setting.UseIPBasedSSL ? SslState.IpBasedEnabled : SslState.SniEnabled,
                                Thumbprint = model.CertificateInfo.Certificate.Thumbprint
                            };
                            if (!string.IsNullOrEmpty(setting.SiteSlotName))
                            {
                                await appServiceManager.Inner.WebApps.CreateOrUpdateHostNameBindingSlotAsync(setting.ResourceGroupName, setting.WebAppName, hostName, binding, setting.SiteSlotName);
                            }
                            else
                            {
                                await appServiceManager.Inner.WebApps.CreateOrUpdateHostNameBindingAsync( setting.ResourceGroupName, setting.WebAppName, hostName, binding);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogCritical(e, "Unable to install certificate for '{WebApp}'", setting.WebAppName);
                    throw;
                }
            }
        }

        private static IAppServiceManager GetAppServiceManager(AzureWebAppSettings settings)
        {
            return AppServiceManager.Authenticate(
                           AzureHelper.GetAzureCredentials(settings.AzureServicePrincipal, settings.AzureSubscription),
                           settings.AzureSubscription.SubscriptionId);
        }

        public List<string> RemoveExpired(int removeXNumberOfDaysBeforeExpiration = 0)
        {
            var removedCerts = new List<string>();
            foreach (var setting in this.settings)
            {
                var appServiceManager = GetAppServiceManager(setting);
                var certs = appServiceManager.AppServiceCertificates.ListByResourceGroup(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName);

                var tobeRemoved = certs.Where(s => s.ExpirationDate < DateTime.UtcNow.AddDays(removeXNumberOfDaysBeforeExpiration) && (s.Issuer.Contains("Let's Encrypt") || s.Issuer.Contains("Fake LE"))).ToList();

                tobeRemoved.ForEach(s => RemoveCertificate(appServiceManager, s, setting));

                removedCerts.AddRange(tobeRemoved.Select(s => s.Thumbprint).ToList());
            }
            return removedCerts;
        }

        private void RemoveCertificate(IAppServiceManager webSiteClient, IAppServiceCertificate s, AzureWebAppSettings setting)
        {
            webSiteClient.AppServiceCertificates.DeleteByResourceGroup(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName, s.Name);
        }


    }
}

