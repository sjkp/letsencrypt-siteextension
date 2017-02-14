using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class CertificateManagerTest
    {
        [TestMethod]
        public void RenewCertificateTest()
        {
            var result = new Core.CertificateManager().RenewCertificate();

            Assert.AreNotEqual(0, result.Count());
        }
    }
}
