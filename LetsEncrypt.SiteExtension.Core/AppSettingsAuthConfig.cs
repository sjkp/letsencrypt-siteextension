using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace LetsEncrypt.SiteExtension.Models
{
    public class AppSettingsAuthConfig : IAuthSettings
    {
        public const string clientIdKey = "letsencrypt:ClientId";
        public const string clientSecretKey = "letsencrypt:ClientSecret";
        public const string tenantKey = "letsencrypt:Tenant";
        public const string subscriptionIdKey = "letsencrypt:SubscriptionId";
        public const string resourceGroupNameKey = "letsencrypt:ResourceGroupName";
        public const string hostNamesKey = "letsencrypt:Hostnames";
        public const string emailKey = "letsencrypt:Email";
        public const string acmeBaseUriKey = "letsencrypt:AcmeBaseUri";
        public const string webAppNameKey = "WEBSITE_SITE_NAME";
        public const string servicePlanResourceGroupNameKey = "letsencrypt:ServicePlanResourceGroupName";
        public const string rsaKeyLengthKey = "letsencrypt:RSAKeyLength";
        private readonly WebAppEnviromentVariables environemntVariables;
        public const string pfxPasswordKey = "letsencrypt:PfxPassword";

        public AppSettingsAuthConfig()
        {
            this.environemntVariables = new WebAppEnviromentVariables();
        }

        public Guid ClientId
        {
            get
            {
                Guid g;
                Guid.TryParse(ConfigurationManager.AppSettings[clientIdKey], out g);
                return g;

            }
        }

        public string ClientSecret
        {
            get
            {
                return ConfigurationManager.AppSettings[clientSecretKey];
            }
        }

        public string Tenant
        {
            get
            {
                return ConfigurationManager.AppSettings[tenantKey];
            }
        }

        public int RenewXNumberOfDaysBeforeExpiration
        {
            get
            {
                var s = ConfigurationManager.AppSettings["RenewXNumberOfDaysBeforeExpiration"];
                int days = 14;
                if (string.IsNullOrEmpty(s) || !int.TryParse(s, out days))
                {
                    return 14;
                }
                return days;
            }
        }

        public Guid SubscriptionId
        {
            get
            {
                Guid g;
                if (!Guid.TryParse(ConfigurationManager.AppSettings[subscriptionIdKey], out g))
                {
                    g = environemntVariables.SubscriptionId;
                }
                
                return g;
            }
        }

        public string WebAppName
        {
            get
            {
                return ConfigurationManager.AppSettings[webAppNameKey]; 
            }
        }

        public string ResourceGroupName
        {
            get
            {
                var resourceGroupName = ConfigurationManager.AppSettings[resourceGroupNameKey];
                if (string.IsNullOrEmpty(resourceGroupName))
                    resourceGroupName = environemntVariables.ResourceGroupName;
                return resourceGroupName;
            }            
        }

        public IEnumerable<string> Hostnames
        {
            get
            {
                return (ConfigurationManager.AppSettings[hostNamesKey] ?? "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public string Email
        {
            get
            {
                return ConfigurationManager.AppSettings[emailKey];
            }
        }

        public string BaseUri
        {
            get
            {
                return ConfigurationManager.AppSettings[acmeBaseUriKey];
            }
        }

        public string ServicePlanResourceGroupName
        {
            get
            {
                var servicePlanResourceGroupName = ConfigurationManager.AppSettings[servicePlanResourceGroupNameKey];
                if (string.IsNullOrEmpty(servicePlanResourceGroupName))
                {
                    return ResourceGroupName;
                }
                return servicePlanResourceGroupName;
            }
        }

        public int RSAKeyLength
        {
            get
            {
                var rsaKeyLength = ConfigurationManager.AppSettings[rsaKeyLengthKey];
                var rsa = 0;
                if (!string.IsNullOrEmpty(rsaKeyLength) && int.TryParse(rsaKeyLength, out rsa))
                {
                    return rsa;
                }
                return 2048;
            }
        }

        public string PFXPassword
        {
            get
            {
                return ConfigurationManager.AppSettings[pfxPasswordKey];
            }
        }
    }
}