using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.SiteExtension.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class CertificateControllerTest
    {
        [TestCategory("Integration")]
        [TestMethod]
        public async Task TestRenewCertificate()
        {
            var config = new AppSettingsAuthConfig();            
            var client = await ArmHelper.GetWebSiteManagementClient(config);
            var kuduClient = KuduHelper.GetKuduClient(client, config);

            var res = await kuduClient.HttpClient.PostAsync("https://webappcfmv5fy7lcq7o.scm.azurewebsites.net/letsencrypt/api/certificates/renew?api-version=2017-09-01", new StringContent(""));
            Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);

            var model = JsonConvert.DeserializeObject<CertificateInstallModel[]>(await res.Content.ReadAsStringAsync());

            Assert.AreEqual(1, model.Count());

            File.WriteAllBytes(Path.GetFileName(model.First().CertificateInfo.Name), model.First().CertificateInfo.PfxCertificate);
        }



        [TestCategory("Integration")]
        [TestMethod]
        public async Task TestRequestAndInstallCertificate()
        {
            var config = new AppSettingsAuthConfig();
            var client = await ArmHelper.GetWebSiteManagementClient(config);
            var kuduClient = KuduHelper.GetKuduClient(client, config);

            var body = new HttpKuduInstallModel()
            {
                AzureEnvironment = new AzureWebAppEnvironment(config.Tenant, config.SubscriptionId, config.ClientId, config.ClientSecret, config.ResourceGroupName, config.WebAppName),
                AcmeConfig = new AcmeConfig()
                {
                    Host = "letsencrypt.sjkp.dk",
                    PFXPassword = "Simon123",
                    RegistrationEmail = "mail@sjkp.dk",
                    RSAKeyLength = 2048
                },
                AuthorizationChallengeProviderConfig = new AuthorizationChallengeProviderConfig(),
                CertificateSettings = new CertificateServiceSettings()
            };

            var res = await kuduClient.HttpClient.PostAsync(
                "https://webappcfmv5fy7lcq7o.scm.azurewebsites.net/letsencrypt/api/certificates/challengeprovider/http/kudu/certificateinstall/azurewebapp?api-version=2017-09-01",
                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            await ValidateResponse(body.AcmeConfig, res);
        }


        [TestCategory("Integration")]
        [TestMethod]
        public async Task TestRequestAndInstallDnsCertificate()
        {
            var config = new AppSettingsAuthConfig();
            var client =  await ArmHelper.GetWebSiteManagementClient(config);
            var kuduClient = KuduHelper.GetKuduClient(client, config);

            var body = new DnsAzureInstallModel()
            {
                AzureWebAppEnvironment = new AzureWebAppEnvironment(config.Tenant, config.SubscriptionId, config.ClientId, config.ClientSecret, config.ResourceGroupName, config.WebAppName),
                AcmeConfig = new AcmeConfig()
                {
                    Host = "letsencrypt.ai4bots.com",
                    PFXPassword = "Simon123",
                    RegistrationEmail = "mail@sjkp.dk",
                    RSAKeyLength = 2048
                },
                RelativeRecordSetName = "letsencrypt",
                ZoneName = "ai4bots.com",
                ResourceGroupName = "dns",
                SubscriptionId = new Guid("14fe4c66-c75a-4323-881b-ea53c1d86a9d"),
                CertificateSettings = new CertificateServiceSettings()
            };

            var res = await kuduClient.HttpClient.PostAsync(
                "https://webappcfmv5fy7lcq7o.scm.azurewebsites.net/letsencrypt/api/certificates/challengeprovider/dns/azure/certificateinstall/azurewebapp?api-version=2017-09-01",
                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            await ValidateResponse(body.AcmeConfig, res);
        }

        private static async Task ValidateResponse(IAcmeConfig acmeConfig, HttpResponseMessage res)
        {
            var bodyResp = await res.Content.ReadAsStringAsync();
            Console.WriteLine(bodyResp);
            Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);

            var certificateModel = JsonConvert.DeserializeObject<CertificateInstallModel>(bodyResp);
            var cert = new X509Certificate(certificateModel.CertificateInfo.PfxCertificate, acmeConfig.PFXPassword);
            Assert.IsTrue(cert.Subject.Contains(acmeConfig.Host));
            File.WriteAllBytes(acmeConfig.Host + ".pfx", certificateModel.CertificateInfo.PfxCertificate);
        }

        [TestCategory("Integration")]
        [TestMethod]
        public async Task TestRequestDnsCertificate()
        {
            var config = new AppSettingsAuthConfig();
            var client = await ArmHelper.GetWebSiteManagementClient(config);
            var kuduClient = KuduHelper.GetKuduClient(client, config);

            var body = new DnsAzureModel()
            {
                AzureDnsEnvironment = new AzureDnsEnvironment(config.Tenant, new Guid("14fe4c66-c75a-4323-881b-ea53c1d86a9d"), config.ClientId, config.ClientSecret, "dns", "ai4bots.com", "@"),
                AcmeConfig = new AcmeConfig()
                {
                    Host = "ai4bots.com",
                    PFXPassword = "Simon123",
                    RegistrationEmail = "mail@sjkp.dk",
                    RSAKeyLength = 2048
                }
            };

            var res = await kuduClient.HttpClient.PostAsync(
                "https://webappcfmv5fy7lcq7o.scm.azurewebsites.net/letsencrypt/api/certificates/challengeprovider/dns/azure?api-version=2017-09-01", 
                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            await ValidateResponse(body.AcmeConfig, res);
        }
    }
}
