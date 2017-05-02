using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.Azure.Management.WebSites;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;

namespace LetsEncrypt.Azure.Core.Services
{
    /// <summary>
    /// Installs and assigns the certificate directly to the app service plan. 
    /// </summary>
    public class CertificateService : ICertificateService
    {
        private readonly IAzureEnvironment azureEnvironment;
        private readonly ICertificateServiceSettings settings;

        public CertificateService(IAzureEnvironment azureEnvironment, ICertificateServiceSettings settings)
        {
            this.azureEnvironment = azureEnvironment;
            this.settings = settings;
        }
        public void Install(ICertificateInstallModel model)
        {
            var cert = model.CertificateInfo;
            using (var webSiteClient = ArmHelper.GetWebSiteManagementClient(azureEnvironment))
            {


                var s = webSiteClient.WebApps.GetSiteOrSlot(azureEnvironment.ResourceGroupName, azureEnvironment.WebAppName, azureEnvironment.SiteSlotName);

                Trace.TraceInformation(String.Format("Installing certificate {0} on azure with server farm id {1}", cert.Name, s.ServerFarmId));
                var newCert = new Certificate()
                {
                    PfxBlob = cert.PfxCertificate,
                    Password = cert.Password,
                    Location = s.Location,
                    ServerFarmId = s.ServerFarmId,
                    Name = model.Host + "-" + cert.Certificate.Thumbprint

                };
                //BUG https://github.com/sjkp/letsencrypt-siteextension/issues/99
                //using this will not install the certificate with the correct webSpace property set, 
                //and the app service will be unable to find the certificate if the app service plan has been moved between resource groups.
                //webSiteClient.Certificates.CreateOrUpdate(azureEnvironment.ServicePlanResourceGroupName, cert.Certificate.Subject.Replace("CN=", ""), newCert);

                var client = ArmHelper.GetHttpClient(azureEnvironment);

                var body = JsonConvert.SerializeObject(newCert, JsonHelper.DefaultSerializationSettings);

                var t = client.PutAsync($"/subscriptions/{azureEnvironment.SubscriptionId}/resourceGroups/{azureEnvironment.ServicePlanResourceGroupName}/providers/Microsoft.Web/certificates/{newCert.Name}?api-version=2016-03-01", new StringContent(body, Encoding.UTF8, "application/json")).Result;

                t.EnsureSuccessStatusCode();


                foreach (var dnsName in model.AllDnsIdentifiers)
                {
                    var sslState = s.HostNameSslStates.FirstOrDefault(g => g.Name == dnsName);

                    if (sslState == null)
                    {
                        sslState = new HostNameSslState()
                        {
                            Name = model.Host,
                            SslState = settings.UseIPBasedSSL ? SslState.IpBasedEnabled : SslState.SniEnabled,
                        };
                        s.HostNameSslStates.Add(sslState);
                    }
                    else
                    {
                        //First time setting the HostNameSslState it is set to disabled.
                        sslState.SslState = settings.UseIPBasedSSL ? SslState.IpBasedEnabled : SslState.SniEnabled;
                    }
                    sslState.ToUpdate = true;
                    sslState.Thumbprint = cert.Certificate.Thumbprint;
                }
                webSiteClient.WebApps.BeginCreateOrUpdateSiteOrSlot(azureEnvironment.ResourceGroupName, azureEnvironment.WebAppName, azureEnvironment.SiteSlotName, s);
            }


        }

        public List<string> RemoveExpired(int removeXNumberOfDaysBeforeExpiration = 0)
        {
            using (var webSiteClient = ArmHelper.GetWebSiteManagementClient(azureEnvironment))
            {
                var certs = webSiteClient.Certificates.ListByResourceGroup(azureEnvironment.ServicePlanResourceGroupName);

                var tobeRemoved = certs.Where(s => s.ExpirationDate < DateTime.UtcNow.AddDays(removeXNumberOfDaysBeforeExpiration) && (s.Issuer.Contains("Let's Encrypt") || s.Issuer.Contains("Fake LE"))).ToList();

                tobeRemoved.ForEach(s => RemoveCertificate(webSiteClient, s));

                return tobeRemoved.Select(s => s.Thumbprint).ToList();
            }
        }

        private void RemoveCertificate(WebSiteManagementClient webSiteClient,  Certificate s)
        {
            webSiteClient.Certificates.Delete(azureEnvironment.ServicePlanResourceGroupName, s.Name);
        }

        
    }
}
