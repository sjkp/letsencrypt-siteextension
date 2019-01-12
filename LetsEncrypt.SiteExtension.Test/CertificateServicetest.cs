using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LetsEncrypt.Azure.Core.Services;
using LetsEncrypt.Azure.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class CertificateServiceTest
    {
        [DeploymentItem("letsencrypt.sjkp.dk-all.pfx")]
        [DeploymentItem("App.secret.config")]
        [TestMethod]
        public async Task TestInstall()
        {
            Console.WriteLine(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.AdalOption).AssemblyQualifiedName);
            var config = new AppSettingsAuthConfig();
            var service = new WebAppCertificateService(config, new CertificateServiceSettings { });
            var pfx = File.ReadAllBytes("letsencrypt.sjkp.dk-all.pfx");
          
            await service.Install(new CertificateInstallModel
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
        public async Task TestRemove()
        {
            var config = new AppSettingsAuthConfig();
            var service = new WebAppCertificateService(config, new CertificateServiceSettings { });            
            
            await service.RemoveExpired(180);
        }
    }
}
