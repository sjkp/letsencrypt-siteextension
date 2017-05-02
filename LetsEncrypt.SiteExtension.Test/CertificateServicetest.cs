using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LetsEncrypt.Azure.Core.Services;
using LetsEncrypt.Azure.Core.Models;
using System.Collections.Generic;
using System.IO;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class CertificateServiceTest
    {
        [DeploymentItem("letsencrypt.sjkp.dk-all.pfx")]
        [TestMethod]
        public void TestInstall()
        {
            var config = new AppSettingsAuthConfig();
            var service = new CertificateService(config, new CertificateServiceSettings { });
            var pfx = File.ReadAllBytes("letsencrypt.sjkp.dk-all.pfx");
          
            service.Install(new CertificateInstallModel
            {
                AllDnsIdentifiers = new List<string>() { "letsencrypt.sjkp.dk" },
                Host = "letsencrypt.sjkp.dk",
                CertificateInfo = new CertificateInfo()
                {
                    Certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(pfx, "Simon123"),
                    Name = "letsencrypt.sjkp.dk-all.pfx",
                    Password = "Simon123",
                    PfxCertificate = pfx

                }

            });
        }
        [TestMethod]
        public void TestRemove()
        {
            var config = new AppSettingsAuthConfig();
            var service = new CertificateService(config, new CertificateServiceSettings { });            
            
            service.RemoveExpired(180);
        }
    }
}
