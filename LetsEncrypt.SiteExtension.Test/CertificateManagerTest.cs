using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core;

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
    }
}
