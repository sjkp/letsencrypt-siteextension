using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class CertificateManagerTest
    {
        [TestMethod]
        public async Task RenewCertificateTest()
        {
            var result = await new Core.CertificateManager(new Models.AppSettingsAuthConfig()).RenewCertificate();

            Assert.AreNotEqual(0, result.Count());
        }
    }
}
