using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class CertificateControllerTest
    {
        [TestMethod]
        public async Task TestRenewCertificate()
        {
            var config = new AppSettingsAuthConfig();            
            var client = ArmHelper.GetWebSiteManagementClient(config);
            var kuduClient = KuduHelper.GetKuduClient(client, config);

            var res = await kuduClient.HttpClient.PostAsync("https://webappcfmv5fy7lcq7o-vnext.scm.azurewebsites.net/letsencrypt/api/certificates/renew?api-version=2017-09-01", new StringContent(""));
            Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);

            var model = JsonConvert.DeserializeObject<CertificateInstallModel[]>(await res.Content.ReadAsStringAsync());

            Assert.AreEqual(1, model.Count());
        }
    }
}
