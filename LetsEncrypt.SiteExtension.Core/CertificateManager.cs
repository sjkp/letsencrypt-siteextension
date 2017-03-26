using ACMESharp;
using ACMESharp.ACME;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using LetsEncrypt.SiteExtension.Core.Models;
using LetsEncrypt.SiteExtension.Core.Services;
using LetsEncrypt.SiteExtension.Models;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core
{
    public class CertificateManager
    {
        static string configPath = "";
        private static string BaseURI;
        static AppSettingsAuthConfig settings = new AppSettingsAuthConfig();
       
        private static WebSiteManagementClient webSiteClient;

        /// <summary>
        /// Used for automatic installation of hostnames bindings and certificate 
        /// upon first installation on the site extension and if hostnames are specified in app settings
        /// </summary>
        public void SetupHostnameAndCertificate()
        {
            Trace.TraceInformation("Setup hostname and certificates");
            var settings = new AppSettingsAuthConfig();
            using (var client = ArmHelper.GetWebSiteManagementClient(settings))
            {
                var s = client.Sites.GetSite(settings.ResourceGroupName, settings.WebAppName);
                foreach (var hostname in settings.Hostnames)
                {
                    if (s.HostNames.Any(existingHostname => string.Equals(existingHostname, hostname, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        Trace.TraceInformation("Hostname already configured skipping installation");
                        continue;
                    }
                    Trace.TraceInformation("Setting up hostname and lets encrypt certificate for " + hostname);
                    client.Sites.CreateOrUpdateSiteOrSlotHostNameBinding(settings.ResourceGroupName, settings.WebAppName, settings.SiteSlotName, hostname, new HostNameBinding()
                    {
                        CustomHostNameDnsRecordType = CustomHostNameDnsRecordType.CName,
                        HostNameType = HostNameType.Verified,
                        SiteName = settings.WebAppName,
                        Location = s.Location
                    });                    
                }
                if (settings.Hostnames.Any())
                {
                    RequestAndInstallInternal(new Target()
                    {
                        BaseUri = settings.BaseUri,
                        ClientId = settings.ClientId,
                        ClientSecret = settings.ClientSecret,
                        Email = settings.Email,
                        Host = settings.Hostnames.First(),
                        ResourceGroupName = settings.ResourceGroupName,
                        SubscriptionId = settings.SubscriptionId,
                        Tenant = settings.Tenant,
                        WebAppName = settings.WebAppName,
                        ServicePlanResourceGroupName = settings.ServicePlanResourceGroupName,
                        AlternativeNames = settings.Hostnames.Skip(1).ToList(),
                        SiteSlotName = settings.SiteSlotName,
                        UseIPBasedSSL = settings.UseIPBasedSSL,
                        DisableWebConfigUpdate = settings.DisableWebConfigUpdate
                    });
                }
            }
        }

        public IEnumerable<Target> RenewCertificate(bool debug = false)
        {
            Trace.TraceInformation("Checking certificate");
            var settings = new AppSettingsAuthConfig();
            var ss = SettingsStore.Instance.Load();
            using (var client = ArmHelper.GetWebSiteManagementClient(settings))
            {
                var certs = client.Certificates.GetCertificates(settings.ServicePlanResourceGroupName).Value;
                var expiringCerts = certs.Where(s => s.ExpirationDate < DateTime.UtcNow.AddDays(settings.RenewXNumberOfDaysBeforeExpiration) && (s.Issuer.Contains("Let's Encrypt") || s.Issuer.Contains("Fake LE")));

                if (expiringCerts.Count() == 0)
                {
                    Trace.TraceInformation(string.Format("No certificates installed issued by Let's Encrypt that are about to expire within the next {0} days. Skipping.", settings.RenewXNumberOfDaysBeforeExpiration));
                }

                foreach (var toExpireCert in expiringCerts)
                {
                    Trace.TraceInformation("Starting renew of certificate " + toExpireCert.Name + " expiration date " + toExpireCert.ExpirationDate);
                    var site = client.Sites.GetSite(settings.ResourceGroupName, settings.WebAppName);
                    var sslStates = site.HostNameSslStates.Where(s => s.Thumbprint == toExpireCert.Thumbprint);
                    if (!sslStates.Any())
                    {
                        Trace.TraceInformation(String.Format("Certificate {0} was not assigned any hostname, skipping update", toExpireCert.Thumbprint));
                        continue;
                    }
                    var target = new Target()
                    {
                        WebAppName = settings.WebAppName,
                        Tenant = settings.Tenant,
                        SubscriptionId = settings.SubscriptionId,
                        ClientId = settings.ClientId,
                        ClientSecret = settings.ClientSecret,
                        ResourceGroupName = settings.ResourceGroupName,
                        Email = settings.Email ?? ss.FirstOrDefault(s => s.Name == "email").Value,
                        Host = sslStates.First().Name,
                        BaseUri = settings.BaseUri ?? ss.FirstOrDefault(s => s.Name == "baseUri").Value,
                        ServicePlanResourceGroupName = settings.ServicePlanResourceGroupName,
                        AlternativeNames = sslStates.Skip(1).Select(s => s.Name).ToList(),
                        UseIPBasedSSL = settings.UseIPBasedSSL,
                        SiteSlotName = settings.SiteSlotName,
                        DisableWebConfigUpdate = settings.DisableWebConfigUpdate
                    };
                    if (!debug)
                    {
                        RequestAndInstallInternal(target);
                    }
                    yield return target;
                }
            }
        }             


        public static string RequestAndInstallInternal(Target target)
        {           
            try
            {
                webSiteClient = ArmHelper.GetWebSiteManagementClient(target);
                var service = new AcmeService(new Models.AcmeConfig()
                {
                    AlternativeNames = target.AlternativeNames,
                    Endpoint = target.BaseUri,
                    Host = target.Host,
                    PFXPassword = settings.PFXPassword,
                    RegistrationEmail = target.Email,
                    RSAKeyLength = settings.RSAKeyLength
                }, new KuduFileSystemAuthorizationChallengeProvider(settings));

                var cert = service.RequestCertificate().Result;
                Install(target, cert);
                return cert.Certificate.Thumbprint;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unabled to create Azure Web Site Management client " + ex.ToString());
                throw;
            }
        }

        public async static Task<string> RequestAndInstallInternalAsync(Target target)
        {
            try
            {
                webSiteClient = ArmHelper.GetWebSiteManagementClient(target);
                var service = new AcmeService(new Models.AcmeConfig()
                {
                    AlternativeNames = target.AlternativeNames,
                    Endpoint = target.BaseUri,
                    Host = target.Host,
                    PFXPassword = settings.PFXPassword,
                    RegistrationEmail = target.Email,
                    RSAKeyLength = settings.RSAKeyLength
                }, new KuduFileSystemAuthorizationChallengeProvider(settings));

                var cert = await service.RequestCertificate();
                Install(target, cert);
                return cert.Certificate.Thumbprint;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unabled to create Azure Web Site Management client " + ex.ToString());
                throw;
            }
        }

        public static void Install(Target target, CertificateInfo cert)
        {
            Console.WriteLine(String.Format("Installing certificate {0} on azure", cert.Name));
            Trace.TraceInformation(String.Format("Installing certificate {0} on azure", cert.Name));
            

            var s = webSiteClient.Sites.GetSiteOrSlot(target.ResourceGroupName, target.WebAppName, target.SiteSlotName);
            webSiteClient.Certificates.CreateOrUpdateCertificate(target.ServicePlanResourceGroupName, cert.Certificate.Subject.Replace("CN=", ""), new Certificate()
            {
                PfxBlob = cert.PfxCertificateBase64,
                Password = settings.PFXPassword,
                Location = s.Location                
            });
            foreach (var dnsName in target.AllDnsIdentifiers)
            {
                var sslState = s.HostNameSslStates.FirstOrDefault(g => g.Name == dnsName);

                if (sslState == null)
                {
                    sslState = new HostNameSslState()
                    {
                        Name = target.Host,
                        SslState = target.UseIPBasedSSL ? SslState.IpBasedEnabled : SslState.SniEnabled,
                    };
                    s.HostNameSslStates.Add(sslState);
                }
                else
                {
                    //First time setting the HostNameSslState it is set to disabled.
                    sslState.SslState = target.UseIPBasedSSL ? SslState.IpBasedEnabled : SslState.SniEnabled;
                }
                sslState.ToUpdate = true;
                sslState.Thumbprint = cert.Certificate.Thumbprint;
            }
            webSiteClient.Sites.BeginCreateOrUpdateSiteOrSlot(target.ResourceGroupName, target.WebAppName, target.SiteSlotName, s);

        }
       
    }
}
