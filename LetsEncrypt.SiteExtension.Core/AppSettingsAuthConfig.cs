using LetsEncrypt.Azure.Core.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Web;

namespace LetsEncrypt.Azure.Core.Models
{
    public class AppSettingsAuthConfig : IAzureWebAppEnvironment, IAcmeConfig
    {
        public const string clientIdKey = "letsencrypt:ClientId";
        public const string clientSecretKey = "letsencrypt:ClientSecret";
        public const string tenantKey = "letsencrypt:Tenant";
        public const string subscriptionIdKey = "letsencrypt:SubscriptionId";
        public const string resourceGroupNameKey = "letsencrypt:ResourceGroupName";
        public const string hostNamesKey = "letsencrypt:Hostnames";
        public const string useIPBasedSSL = "letsencrypt:UseIPBasedSSL";
        public const string emailKey = "letsencrypt:Email";
        public const string acmeBaseUriKey = "letsencrypt:AcmeBaseUri";
        public const string siteSlotNameKey = "letsencrypt:SiteSlot";
        public const string webAppNameKey = "WEBSITE_SITE_NAME";
        public const string webRootPath = "letsencrypt:WebRootPath";
        public const string servicePlanResourceGroupNameKey = "letsencrypt:ServicePlanResourceGroupName";
        public const string rsaKeyLengthKey = "letsencrypt:RSAKeyLength";
        private readonly WebAppEnviromentVariables environemntVariables;
        public const string pfxPasswordKey = "letsencrypt:PfxPassword";
        public const string renewBeforeExpirationKey = "letsencrypt:RenewXNumberOfDaysBeforeExpiration";
        public const string authenticationEndpointKey = "letsencrypt:AzureAuthenticationEndpoint";
        public const string tokenAudienceKey = "letsencrypt:AzureTokenAudience";
        public const string managementEndpointKey = "letsencrypt:AzureManagementEndpoint";
        public const string azureDefaultWebSiteDomainName = "letsencrypt:AzureDefaultWebSiteDomainName";
        public const string disableWebConfigUpdateKey = "letsencrypt:DisableWebConfigUpdate";
        public const string authorizationChallengeBlobStorageAccount = "letsencrypt:AuthorizationChallengeBlobStorageAccount";
        public const string authorizationChallengeBlobStorageContainer = "letsencrypt:AuthorizationChallengeBlobStorageContainer";
        public const string disableVirtualApplication = "letsencrypt:DisableVirtualApplication";
        public const string webjobDashboard = "AzureWebJobsDashboard";
        public const string webjobStorage = "AzureWebJobsStorage";

        public AppSettingsAuthConfig()
        {
            this.environemntVariables = new WebAppEnviromentVariables();
        }

        [RequiredGuid(ErrorMessage = clientIdKey + " appSettings is required")]
        public Guid ClientId
        {
            get
            {
                Guid g;
                Guid.TryParse(ConfigurationManager.AppSettings[clientIdKey], out g);
                return g;

            }
        }

        [Required(ErrorMessage = clientIdKey + " appSetting is required")]
        public string ClientSecret
        {
            get
            {
                return ConfigurationManager.AppSettings[clientSecretKey];
            }
        }

        [Required (ErrorMessage = tenantKey + " appSetting is required")]
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
                var s = ConfigurationManager.AppSettings[renewBeforeExpirationKey];
                int days = 22;
                if (string.IsNullOrEmpty(s) || !int.TryParse(s, out days))
                {
                    return 22;
                }
                return days;
            }
        }

        [RequiredGuid (ErrorMessage = subscriptionIdKey + " appSetting is required")]
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

        [Required(ErrorMessage = webAppNameKey + " appSetting is required")]
        public string WebAppName
        {
            get
            {
                return ConfigurationManager.AppSettings[webAppNameKey]; 
            }
        }

        [Required(ErrorMessage = webjobDashboard + " connectionString is required")]
        public string DashboardConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings[webjobDashboard]?.ConnectionString;
            }
        }

        [Required(ErrorMessage = webjobStorage + " connectionString is required")]
        public string StorageConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings[webjobStorage]?.ConnectionString;
            }
        }

        public string WebRootPath
        {
            get
            {
                return ConfigurationManager.AppSettings[webRootPath];
            }
        }

        public string SiteSlotName
        {
            get
            {
                return ConfigurationManager.AppSettings[siteSlotNameKey];
            }
        }

        [Required(ErrorMessage = resourceGroupNameKey + " appSetting is required")]
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

        public string Host
        {
            get
            {
                return Hostnames.First();
            }
        }

        public List<string> AlternateNames
        {
            get
            {
                return Hostnames.Skip(1).ToList();
            }
        }

        public bool UseIPBasedSSL
        {
            get
            {
                bool b;
                if (bool.TryParse(ConfigurationManager.AppSettings[useIPBasedSSL], out b))
                {
                    return b;
                }

                return false;
            }
        }

        public bool DisableWebConfigUpdate
        {
            get
            {
                bool b; 
                if (bool.TryParse(ConfigurationManager.AppSettings[disableWebConfigUpdateKey], out b))
                {
                    return b;
                }
                return false;
            }
        }

        public string RegistrationEmail
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

        public bool UseProduction
        {
            get
            {
                return false; //Default behavior is not to use production environment.
            }
        }


        #region overrideable settings to enable support for azure azure regions

        [DataType(DataType.Url)]
        public Uri AuthenticationEndpoint
        {
            get
            {
                var authEndpoint = ConfigurationManager.AppSettings[authenticationEndpointKey];                
                if (string.IsNullOrEmpty(authEndpoint))
                {
                    return new Uri("https://login.windows.net/"); 
                }
                return new Uri(authEndpoint);
            }
        }

        [DataType(DataType.Url)]
        public Uri TokenAudience
        {
            get
            {
                var tokenAudience = ConfigurationManager.AppSettings[tokenAudienceKey];
                if (string.IsNullOrEmpty(tokenAudience))
                {
                    return new Uri("https://management.core.windows.net/");
                }
                return new Uri(tokenAudience);
            }
        }

        [DataType(DataType.Url)]
        public Uri ManagementEndpoint
        {
            get
            {
                var managementEndpoint = ConfigurationManager.AppSettings[managementEndpointKey];
                if (string.IsNullOrEmpty(managementEndpoint))
                {
                    return new Uri("https://management.azure.com");
                }
                return new Uri(managementEndpoint);
            }
        }

        public string AzureWebSitesDefaultDomainName
        {
            get
            {
                var domain = ConfigurationManager.AppSettings[azureDefaultWebSiteDomainName]; 
                if (string.IsNullOrEmpty(domain))
                {
                    return "azurewebsites.net";
                }
                return domain;
            }
        }        

        #endregion

        public bool IsValid(out List<ValidationResult> result)
        {

            var context = new ValidationContext(this, serviceProvider: null, items: null);
            result = new List<ValidationResult>();

            return Validator.TryValidateObject(this, context, result, true);
        }

        public bool RunFromPackage
        {
            get
            {
                return RunFromZip;
            }
        }

        /// <summary>
        /// Uses the environment variables to determine in site is using run from zip/package deployment.
        /// </summary>
        private static bool RunFromZip
        {
            get
            {
                var runFromZip = ConfigurationManager.AppSettings["WEBSITE_RUN_FROM_PACKAGE"];
                if (string.IsNullOrEmpty(runFromZip))
                {
                    runFromZip = ConfigurationManager.AppSettings["WEBSITE_RUN_FROM_ZIP"]; //The old variable name
                }
                return "1".Equals(runFromZip) || (runFromZip?.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) ?? false);
            }
        }
    }
}