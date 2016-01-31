using System;

namespace LetsEncrypt.SiteExtension.Models
{
    public interface IAuthSettings
    {
        string Tenant { get; }

        Guid ClientId { get; }

        string ClientSecret { get;}
        Guid SubscriptionId { get; }
        string WebAppName { get; }
        string ResourceGroupName { get; }

        string ServicePlanResourceGroupName { get; }
    }
}