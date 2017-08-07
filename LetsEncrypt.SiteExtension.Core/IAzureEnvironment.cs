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

        string SiteSlotName { get; }

        string AzureWebSitesDefaultDomainName { get; }

      
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

        public Uri AuthenticationEndpoint
        {
            get
            {
                return new Uri("https://login.windows.net/");
            }
        }

        [Required]
        public Guid ClientId
        {
            get; private set;
        }

        [Required]
        public string ClientSecret
        {
            get; private set;
        }

        public Uri ManagementEndpoint
        {
            get
            {
                return new Uri("https://management.azure.com");
            }
        }

        [Required]
        public string ResourceGroupName
        {
            get; private set;
        }

        [Required]
        public Guid SubscriptionId
        {
            get; private set;
        }

        [Required]
        public string Tenant
        {
            get; private set;
        }

        public Uri TokenAudience
        {
            get
            {
                return new Uri("https://management.core.windows.net/");
            }
        }
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

    public class AzureWebAppEnvironment : AzureEnvironment, IAzureWebAppEnvironment
    {
        public AzureWebAppEnvironment(string tenant, Guid subscription, Guid clientId, string clientSecret, string resourceGroup, string webAppName, string servicePlanResourceGroupName = null, string siteSlotName = null)
            : base(tenant, subscription, clientId, clientSecret, resourceGroup)
        {          
            this.WebAppName = webAppName;
            this.ServicePlanResourceGroupName = string.IsNullOrEmpty(servicePlanResourceGroupName) ? resourceGroup : servicePlanResourceGroupName;
            this.SiteSlotName = siteSlotName;            
        }

        public string AzureWebSitesDefaultDomainName
        {
            get
            {
                return "azurewebsites.net";
            }
        }     

        public string ServicePlanResourceGroupName
        {
            get; private set;
        }
        
        public string SiteSlotName
        {
            get; private set;
        }      

        [Required]
        public string WebAppName
        {
            get; private set;
        }
    }
}