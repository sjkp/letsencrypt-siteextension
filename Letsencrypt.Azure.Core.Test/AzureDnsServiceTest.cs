using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Letsencrypt.Azure.Core.Test
{
    [TestClass]
    public class AzureDnsServiceTest
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var config = TestHelper.AzureDnsEnvironment;

            var client = await TestHelper.MakeDnsClient(config);
            var service = new AzureDnsProviderService(client, config);

            var id = Guid.NewGuid().ToString();
            await service.PersistChallenge("_acme-challenge", id);


            var exists = await new DnsLookupService().Exists("*.ai4bots.com", id);
            Assert.IsTrue(exists);

            await service.Cleanup("_acme-challenge");
        }
    }
}
