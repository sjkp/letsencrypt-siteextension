using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Letsencrypt.Azure.Core.Test
{
    public class TestHelper
    {
        private static readonly string tenantId;
        private static readonly string subscriptionId;
        private static string clientId;
        private static string secret;

        static TestHelper()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<TestHelper>()
                  .Build();

            tenantId = config["tenantId"];
            subscriptionId = config["subscriptionId"];
            clientId = config["clientId"];
            secret = config["clientSecret"];
        }
        public static AzureDnsSettings AzureDnsSettings
        {
            get
            {                
                return new AzureDnsSettings("dns", "ai4bots.com", AzureServicePrincipal, new AzureSubscription()
                {
                    AzureRegion = "AzureGlobalCloud",
                    SubscriptionId = subscriptionId,
                    Tenant = tenantId
                });
            }
        }

        public static UnoEuroDnsProvider UnoEuroDnsProvider
        {
            get
            {
                var config = new ConfigurationBuilder()
          .AddUserSecrets<UnoEuroDnsProviderTest>()
          .Build();

                return new UnoEuroDnsProvider(new UnoEuroDnsSettings()
                {
                    AccountName = config["accountName"],
                    ApiKey = config["apiKey"],
                    Domain = config["domain"]
                });
            }
        }

        public static AzureServicePrincipal AzureServicePrincipal => new AzureServicePrincipal()
        {
            ClientId = clientId,
            ClientSecret = secret
        };

        public static AzureWebAppSettings AzureWebAppSettings
        {
            get
            {               
                return new AzureWebAppSettings("webappcfmv5fy7lcq7o", "LetsEncrypt-SiteExtension2", AzureServicePrincipal, new AzureSubscription()
                {
                    Tenant = tenantId,
                    SubscriptionId = "3f09c367-93e0-4b61-bbe5-dcb5c686bf8a",
                    AzureRegion = "AzureGlobalCloud"
                });
            }
        }
    }
}
