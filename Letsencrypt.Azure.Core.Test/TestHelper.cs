using LetsEncrypt.Azure.Core.Models;
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
        public static IAzureDnsEnvironment AzureDnsEnvironment
        {
            get
            {
                var config = new ConfigurationBuilder()
                  .AddJsonFile("secrets.json")
                  .Build();

                string tenantId = config["tenantId"];
                Guid subscriptionId = new Guid(config["subscriptionId"]);
                string clientId = config["clientId"];
                string secret = config["clientSecret"];


                return new AzureDnsEnvironment(tenantId, subscriptionId, new Guid(clientId), secret, "dns", "ai4bots.com", "@");
            }
        }

        public static async Task<DnsManagementClient> MakeDnsClient(IAzureDnsEnvironment config)
        {
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(config.Tenant, config.ClientId.ToString(), config.ClientSecret);
            var client = new Microsoft.Azure.Management.Dns.Fluent.DnsManagementClient(serviceCreds);
            client.SubscriptionId = config.SubscriptionId.ToString();
            return client;
        }
    }
}
