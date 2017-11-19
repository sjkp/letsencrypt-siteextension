using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.Services;

namespace LetsEncrypt.SiteExtension.Test
{
    
    [TestClass]
    public class CertificateManagerTest
    {
        [TestCategory("Integration")]
        [TestMethod]
        public async Task RenewCertificateTest()
        {
            var result = await new CertificateManager(new AppSettingsAuthConfig()).RenewCertificate();

            Assert.AreNotEqual(0, result.Count());
        }

        [TestCategory("Integration")]
        [TestMethod]
        public async Task RenewCertificateConstructorTest()
        {
            var settings = new AppSettingsAuthConfig();
            var mgr = new CertificateManager(settings, settings, new WebAppCertificateService(settings, new CertificateServiceSettings()
            {
                UseIPBasedSSL = settings.UseIPBasedSSL
            }), new KuduFileSystemAuthorizationChallengeProvider(settings, new AuthorizationChallengeProviderConfig()));
            var result = await mgr.RenewCertificate(renewXNumberOfDaysBeforeExpiration: 200);

            Assert.AreNotEqual(0, result.Count());
        }

        [TestCategory("Integration")]
        [TestMethod]
        public async Task RenewCertificateDnsChallengeTest()
        {
            var config = new AppSettingsAuthConfig();
            var dnsEnvironment = new AzureDnsEnvironment(config.Tenant, new Guid("14fe4c66-c75a-4323-881b-ea53c1d86a9d"), config.ClientId, config.ClientSecret, "dns", "ai4bots.com", "@");
            var mgr = new CertificateManager(config, config, new WebAppCertificateService(config, new CertificateServiceSettings()
            {
                UseIPBasedSSL = config.UseIPBasedSSL
            }), new AzureDnsAuthorizationChallengeProvider(dnsEnvironment));
            var result = await mgr.RenewCertificate(renewXNumberOfDaysBeforeExpiration: 200);

            Assert.AreNotEqual(0, result.Count());
        }

        [TestCategory("Integration")]
        [TestMethod]
        public async Task AddCertificateDnsChallengeTest()
        {
            var config = new AppSettingsAuthConfig();

            var dnsEnvironment = new AzureDnsEnvironment(config.Tenant, new Guid("14fe4c66-c75a-4323-881b-ea53c1d86a9d"), config.ClientId, config.ClientSecret, "dns", "ai4bots.com", "letsencrypt");
            var mgr = new CertificateManager(config, new AcmeConfig()
            {
                Host = "letsencrypt.ai4bots.com",
                PFXPassword = "Simon123",
                RegistrationEmail = "mail@sjkp.dk",
                RSAKeyLength = 2048                
            }, new WebAppCertificateService(config, new CertificateServiceSettings()
            {
                UseIPBasedSSL = config.UseIPBasedSSL
            }), new AzureDnsAuthorizationChallengeProvider(dnsEnvironment));
            var result = await mgr.AddCertificate();

            Assert.IsNotNull(result);
        }

        [TestCategory("Integration")]
        [TestMethod]
        public async Task RequestCertificateDnsChallengeTest()
        {
            var config = new AppSettingsAuthConfig();
            var dnsEnvironment = new AzureDnsEnvironment(config.Tenant, new Guid("14fe4c66-c75a-4323-881b-ea53c1d86a9d"), config.ClientId, config.ClientSecret, "dns", "ai4bots.com", "@");

            var res = await CertificateManager.RequestDnsChallengeCertificate(dnsEnvironment, new AcmeConfig()
            {
                Host = "ai4bots.com",
                PFXPassword = "Simon123",
                RegistrationEmail = "mail@sjkp.dk",
                RSAKeyLength = 2048
            });

            Assert.IsTrue(res.CertificateInfo.Certificate.Subject.Contains("ai4bots.com"));
        }
    }
}
