using LetsEncrypt.Azure.Core.V2;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static LetsEncrypt.Azure.Core.V2.DnsProviders.GoDaddyDnsProvider;

namespace Letsencrypt.Azure.Core.Test
{
    [TestClass]
    public class GoDaddyDnsProviderTest
    {
        private const string Domain = "åbningstider.info";

        public IConfiguration Configuration { get; }
        public GoDaddyDnsProvider DnsService { get; }

        public GoDaddyDnsProviderTest()
        {
            this.Configuration = new ConfigurationBuilder()            
            .AddUserSecrets<GoDaddyDnsProviderTest>()
            .Build();

            this.DnsService = new GoDaddyDnsProvider(new GoDaddyDnsSettings()
            {
                ApiKey = this.Configuration["GoDaddyApiKey"],
                ApiSecret = this.Configuration["GoDaddyApiSecret"],
                ShopperId = this.Configuration["GoDaddyShopperId"],
                Domain = Domain
            });
        }

        [TestMethod]
        public async Task TestPersistChallenge()
        {
            var id = Guid.NewGuid().ToString();
            await DnsService.PersistChallenge("_acme-challenge", id);


            var exists = await new DnsLookupService().Exists("*." + Domain, id);
            Assert.IsTrue(exists);

            await DnsService.Cleanup("_acme-challenge");
        }
    }
}
