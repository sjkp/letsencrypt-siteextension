using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core.V2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Letsencrypt.Azure.Core.Test
{
    [TestClass]
    public class CertificateManagerTest
    {

        [TestMethod]
        public async Task TestEndToEnd()
        {
            var config = TestHelper.AzureDnsEnvironment;

            var client = await TestHelper.MakeDnsClient(config);

            var manager = new CertificateManager(new AzureDnsProviderService(client, config), new DnsLookupService());

            await manager.RequestDnsChallengeCertificatev2(config, new AcmeConfig()
            {
                Host = "*.ai4bots.com",
                PFXPassword = "Pass@word",
                RegistrationEmail = "mail@sjkp.dk",
                UseProduction = false
            });
        }
    }
}
