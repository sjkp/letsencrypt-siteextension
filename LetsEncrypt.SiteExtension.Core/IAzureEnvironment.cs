using System;
using System.ComponentModel.DataAnnotations;

namespace LetsEncrypt.Azure.Core.Models
{
    public interface IAzureEnvironment
    {
        string Tenant { get; }

        Guid ClientId { get; }

        string ClientSecret { get; }
        Guid SubscriptionId { get; }

        Uri ManagementEndpoint { get; }

        Uri TokenAudience { get; }

        Uri AuthenticationEndpoint { get; }
    }

    public interface IAzureWebAppEnvironment : IAzureEnvironment
    {
       
        string WebAppName { get; }
        string ResourceGroupName { get; }

        string ServicePlanResourceGroupName { get; }

        string TipSlotName { get; }

        string SiteSlotName { get; }

        string AzureWebSitesDefaultDomainName { get; }

        string WebRootPath { get; }

        bool RunFromPackage { get; }
    }

    public interface IAzureDnsEnvironment : IAzureEnvironment
    {       
        string ResourceGroupName { get; }

        string RelativeRecordSetName { get; }

        string ZoneName { get; } 
    }


    public class AzureEnvironment : IAzureEnvironment
    {
        public AzureEnvironment(string tenant, Guid subscription, Guid clientId, string clientSecret, string resourceGroup)
        {
            this.Tenant = tenant;
            this.SubscriptionId = subscription;
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
            this.ResourceGroupName = resourceGroup;
        }

        /// <summary>
        /// The authentication endpoint to use when signin in the service principal.
        /// Defaults to https://login.windows.net/.
        /// </summary>
        public Uri AuthenticationEndpoint
        {
            get; set;
        } = new Uri("https://login.windows.net/");

        /// <summary>
        /// The client id of the service principal. 
        /// </summary>
        [Required]
        public Guid ClientId
        {
            get; set;
        }

        /// <summary>
        /// The client secret for the service principal to use.
        /// </summary>
        [Required]
        public string ClientSecret
        {
            get; set;
        }

        /// <summary>
        /// The Azure Management API endpoint to use. Defaults to https://management.azure.com
        /// </summary>
        public Uri ManagementEndpoint
        {
            get; set;
        } = new Uri("https://management.azure.com");

        /// <summary>
        /// The resource group name that the web app belongs to.
        /// </summary>
        [Required]
        public string ResourceGroupName
        {
            get; set;
        }

        /// <summary>
        /// The Azure subscription to use. 
        /// </summary>
        [Required]
        public Guid SubscriptionId
        {
            get; set;
        }

        /// <summary>
        /// The Azure AD tenant to use. 
        /// </summary>
        [Required]
        public string Tenant
        {
            get; set;
        }

        /// <summary>
        /// The token audience to use. Defaults to https://management.core.windows.net/
        /// </summary>
        public Uri TokenAudience { get; set; } = new Uri("https://management.core.windows.net/");
        
    }

    public class AzureDnsEnvironment : AzureEnvironment, IAzureDnsEnvironment
    {
        public AzureDnsEnvironment(string tenant, Guid subscription, Guid clientId, string clientSecret, string resourceGroup, string zoneName, string relativeRecordSetName)
            : base(tenant, subscription,clientId, clientSecret, resourceGroup)
        {
            this.ZoneName = zoneName;
            this.RelativeRecordSetName = relativeRecordSetName;
        }

        public string RelativeRecordSetName { get; }
        public string ZoneName { get; }
    }

    /// <summary>
    /// Description of the Azure environment.
    /// </summary>
    public class AzureWebAppEnvironment : AzureEnvironment, IAzureWebAppEnvironment
    {
        public AzureWebAppEnvironment(string tenant, Guid subscription, Guid clientId, string clientSecret, string resourceGroup, string webAppName, string servicePlanResourceGroupName = null, string siteSlotName = null, string webrootPath = null)
            : base(tenant, subscription, clientId, clientSecret, resourceGroup)
        {          
            this.WebAppName = webAppName;
            this.WebRootPath = webrootPath;
            this.ServicePlanResourceGroupName = string.IsNullOrEmpty(servicePlanResourceGroupName) ? resourceGroup : servicePlanResourceGroupName;
            this.SiteSlotName = siteSlotName;            
        }

        /// <summary>
        /// The azure domain name for the web app. Defaults to azurewebsites.net
        /// </summary>
        public string AzureWebSitesDefaultDomainName
        {
            get; set;
        } = "azurewebsites.net";


        string _servicePlanResourceGroupName;

        /// <summary>
        /// The app service plan resource group name, 
        /// only required if the web app and app service plan is in different resource groups.
        /// </summary>
        public string ServicePlanResourceGroupName
        {
            get
            {
                return _servicePlanResourceGroupName ?? this.ResourceGroupName;
            }
            set
            {
                _servicePlanResourceGroupName = value;
            }
        }

        /// <summary>
        /// The site slot where the challenge file is installed. Only required if multiple slots are used with testing-in-Production.
        /// </summary>
        public string TipSlotName
        {
            get; set;
        }

        /// <summary>
        /// The site slot to install the certificate on. Only required if multiple slots is used.
        /// </summary>
        public string SiteSlotName
        {
            get; set;
        }      

        /// <summary>
        /// The name of the web app (without .azurewebsites.net)
        /// </summary>
        [Required]
        public string WebAppName
        {
            get; set;
        }

        /// <summary>
        /// The path to the web root.
        /// </summary>
        public string WebRootPath
        {
            get; set;
        }

        /// <summary>
        /// Is the web app using RunFromPackage deployment (wwwroot is readonly)
        /// </summary>
        public bool RunFromPackage
        {
            get;set;
        }
    }
}