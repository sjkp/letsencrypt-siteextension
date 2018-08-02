using LetsEncrypt.Azure.Core.V2;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Letsencrypt.Azure.Core.Test
{
    [TestClass]
    public class UnoEuroDnsProviderTest
    {
        [TestMethod]
        public async Task CreateRecord()
        {
            var config = new ConfigurationBuilder()
              .AddUserSecrets<UnoEuroDnsProviderTest>()
              .Build();
            
            var dnsProvider = new UnoEuroDnsProvider(new UnoEuroDnsSettings()
            {
                AccountName = config["accountName"],
                ApiKey = config["apiKey"],
                Domain = config["domain"]
            });
            //Test create new
            await dnsProvider.PersistChallenge("_acme-challenge", Guid.NewGuid().ToString());
            //Test Update existing
            await dnsProvider.PersistChallenge("_acme-challenge", Guid.NewGuid().ToString());
            //Test clean up
            await dnsProvider.Cleanup("_acme-challenge");

        }

        [TestMethod]
        public async Task UnoEuroDnsTest()
        {
            var service = TestHelper.UnoEuroDnsProvider;

            var id = Guid.NewGuid().ToString();
            await service.PersistChallenge("_acme-challenge", id);


            var exists = await new DnsLookupService().Exists("*.tiimo.dk", id, service.MinimumTtl);
            Assert.IsTrue(exists);

            await service.Cleanup("_acme-challenge");
        }


    }
}
