using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class PublishingCrendentialsTest
    {

        [TestMethod]
        public async Task GetPublishingCredentials()
        { 

            var model = new AppSettingsAuthConfig();
            var helper = await ArmHelper.GetWebSiteManagementClient(model);

            var kuduClient = KuduHelper.GetKuduClient(helper, model);

            //var res = await kuduClient.GetScmInfo();

            var dir = await kuduClient.GetFile("site/wwwroot/host.json");
            using (var ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms);
                sw.WriteLine("Hell asd asd asd ");
                sw.Flush();

                await kuduClient.PutFile("site/wwwroot/.well-known/acme-challenge2/test.json", ms);
            }
            
        }
    }
}
