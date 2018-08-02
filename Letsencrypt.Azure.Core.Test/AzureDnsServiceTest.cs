using LetsEncrypt.Azure.Core.V2;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
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
        public async Task AzureDnsTest()
        {
            var config = TestHelper.AzureDnsSettings;
            
            var service = new AzureDnsProvider(config);

            var id = Guid.NewGuid().ToString();
            await service.PersistChallenge("_acme-challenge", id);


            var exists = await new DnsLookupService().Exists("*.ai4bots.com", id, service.MinimumTtl);
            Assert.IsTrue(exists);

            await service.Cleanup("_acme-challenge");
        }       
    }
}
