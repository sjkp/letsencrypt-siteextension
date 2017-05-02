using System;

namespace LetsEncrypt.Azure.Core.Models
{
    public interface IAzureEnvironment
    {
        string Tenant { get; }

        Guid ClientId { get; }

        string ClientSecret { get;}
        Guid SubscriptionId { get; }
        string WebAppName { get; }
        string ResourceGroupName { get; }

        string ServicePlanResourceGroupName { get; }

        string SiteSlotName { get; }

        string AzureWebSitesDefaultDomainName { get; }

        Uri ManagementEndpoint { get; }

        Uri TokenAudience { get; }

        Uri AuthenticationEndpoint { get; }
    }

    public class AzureEnvironment : IAzureEnvironment
    {
        public AzureEnvironment(string tenant, Guid subscription, Guid clientId, string clientSecret, string resourceGroup, string webAppName, string servicePlanResourceGroupName = null, string siteSlotName = null)
        {
            this.Tenant = tenant;
            this.SubscriptionId = subscription;
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
            this.ResourceGroupName = resourceGroup;
            this.WebAppName = webAppName;
            this.ServicePlanResourceGroupName = string.IsNullOrEmpty(servicePlanResourceGroupName) ? resourceGroup : servicePlanResourceGroupName;
            this.SiteSlotName = siteSlotName;
        }


        public Uri AuthenticationEndpoint
        {
            get
            {
                return new Uri("https://login.windows.net/");
            }
        }

        public string AzureWebSitesDefaultDomainName
        {
            get
            {
                return "azurewebsites.net";
            }
        }

        public Guid ClientId
        {
            get; private set;
        }

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

        public string ResourceGroupName
        {
            get; private set;
        }

        public string ServicePlanResourceGroupName
        {
            get; private set;
        }

        public string SiteSlotName
        {
            get; private set;
        }

        public Guid SubscriptionId
        {
            get; private set;
        }

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

        public string WebAppName
        {
            get; private set;
        }
    }
}