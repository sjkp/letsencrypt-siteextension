using System;

namespace LetsEncrypt.SiteExtension.Models
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
}