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
        [TestMethod]
        public async Task RenewCertificateTest()
        {
            var result = await new CertificateManager(new AppSettingsAuthConfig()).RenewCertificate();

            Assert.AreNotEqual(0, result.Count());
        }

        [TestMethod]
        public async Task RenewCertificateConstructorTest()
        {
            var settings = new AppSettingsAuthConfig();
            var mgr = new CertificateManager(settings, settings, new CertificateService(settings, new CertificateServiceSettings()
            {
                UseIPBasedSSL = settings.UseIPBasedSSL
            }), new KuduFileSystemAuthorizationChallengeProvider(settings, new AuthorizationChallengeProviderConfig()));
            var result = await mgr.RenewCertificate(renewXNumberOfDaysBeforeExpiration: 200);

            Assert.AreNotEqual(0, result.Count());
        }


        [TestMethod]
        public async Task RenewCertificateDnsChallengeTest()
        {
            //var settings = new AppSettingsAuthConfig();
            //var mgr = new CertificateManager(settings, settings, new CertificateService(settings, new CertificateServiceSettings()
            //{
            //    UseIPBasedSSL = settings.UseIPBasedSSL
            //}), new AzureDnsAuthorizationChallengeProvider());
            //var result = await mgr.RenewCertificate(renewXNumberOfDaysBeforeExpiration: 200);

            //Assert.AreNotEqual(0, result.Count());
        }
    }
}
