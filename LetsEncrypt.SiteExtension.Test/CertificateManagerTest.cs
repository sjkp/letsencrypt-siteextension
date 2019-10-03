using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.Services;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;

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

            Assert.AreEqual(0, result.Count());            
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
            ValidateCertificate(result, "https://letsencrypt.sjkp.dk");
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
        public async Task AddCertificateHttpChallengeTest()
        {
            var config = new AppSettingsAuthConfig();


            var dnsEnvironment = new AzureDnsEnvironment(config.Tenant, new Guid("14fe4c66-c75a-4323-881b-ea53c1d86a9d"), config.ClientId, config.ClientSecret, "dns", "ai4bots.com", "letsencrypt");
            var mgr = new CertificateManager(config, new AcmeConfig()
            {
                Host = "letsencrypt.sjkp.dk",
                PFXPassword = "Simon123",
                RegistrationEmail = "mail@sjkp.dk",
                RSAKeyLength = 2048
            }, new WebAppCertificateService(config, new CertificateServiceSettings()
            {
                UseIPBasedSSL = config.UseIPBasedSSL
            }), new KuduFileSystemAuthorizationChallengeProvider(config, new AuthorizationChallengeProviderConfig()));
            var result = await mgr.AddCertificate();

            Assert.IsNotNull(result);
            ValidateCertificate(new[] { result }, "https://letsencrypt.sjkp.dk");
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
            ValidateCertificate(new[] { result }, "https://letsencrypt.ai4bots.com");
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

        [DeploymentItem("certArray.json")]
        [DeploymentItem("certArrayWithValue.json")]
        [TestMethod]
        public void ExtractCertificates()
        {
            var t1 = File.ReadAllText("certArray.json");
            var t2 = File.ReadAllText("certArrayWithValue.json");
            var res1 = CertificateManager.ExtractCertificates(t1);
            var res2 = CertificateManager.ExtractCertificates(t2);

            Assert.AreEqual("A19D760D4D50552DA48B1D493738BD754E5EA8DA", res1.FirstOrDefault().Thumbprint);
            Assert.AreEqual("A19D760D4D50552DA48B1D493738BD754E5EA8DA", res2.FirstOrDefault().Thumbprint);
        }

        
        private void ValidateCertificate(IEnumerable<CertificateInstallModel> certs, string uri)
        {
            //Do webrequest to get info on secure site            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            response.Close();

            //retrieve the ssl cert and assign it to an X509Certificate object
            X509Certificate cert = request.ServicePoint.Certificate;

            //convert the X509Certificate to an X509Certificate2 object by passing it into the constructor
            X509Certificate2 cert2 = new X509Certificate2(cert);

            string cn = cert2.Issuer;
            Assert.AreEqual("CN=Fake LE Intermediate X1", cn);
            string tb = cert2.Thumbprint;
            Assert.AreEqual(certs.FirstOrDefault().CertificateInfo.Certificate.Thumbprint, tb);
            string cedate = cert2.GetExpirationDateString();
            string cpub = cert2.GetPublicKeyString();
        }
    }
}

