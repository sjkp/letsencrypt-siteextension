using ACMESharp.ACME;
using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class AzureDnsAuthorizationChallengeProviderTest
    {
        [TestMethod]
        public async Task AddChallenge()
        {
            var config = new AppSettingsAuthConfig();


            var provider = new AzureDnsAuthorizationChallengeProvider(new AzureDnsEnvironment(config.Tenant, new Guid("14fe4c66-c75a-4323-881b-ea53c1d86a9d"), config.ClientId, config.ClientSecret, "dns", "ai4bots.com", "@"));
            await provider.PersistsChallenge(new DnsChallenge("dns01", new DnsChallengeAnswer())
            {
                RecordValue = "Test 1"
            });
        }


        [TestMethod]
        public async Task RemoveChallenge()
        {
            var config = new AppSettingsAuthConfig();


            var provider = new AzureDnsAuthorizationChallengeProvider(new AzureDnsEnvironment(config.Tenant, new Guid("14fe4c66-c75a-4323-881b-ea53c1d86a9d"), config.ClientId, config.ClientSecret, "dns", "ai4bots.com", "@"));
            await provider.PersistsChallenge(new DnsChallenge("dns01", new DnsChallengeAnswer())
            {
                RecordValue = "Test 1"
            });

            await provider.CleanupChallenge(new DnsChallenge("dns01", new DnsChallengeAnswer()) {
                RecordValue = "Test 1"
            });
        }
    }
}
