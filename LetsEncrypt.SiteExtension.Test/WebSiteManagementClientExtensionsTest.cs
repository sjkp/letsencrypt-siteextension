using LetsEncrypt.SiteExtension.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class WebSiteManagementClientExtensionsTest
    {
        [TestMethod]
        public void ServerFarmResourceGroup()
        {
            var serverFarmId = "/subscriptions/d492eeec-afe0-41e8-85d4-8141473d7e55/resourceGroups/LetsEncrypt-SiteExtension/providers/Microsoft.Web/serverfarms/sjkp.testplan";

            var res = WebSiteManagementClientExtensions.ServerResourceGroupFromServerFarmId(serverFarmId);

            Assert.AreEqual("LetsEncrypt-SiteExtension", res);
        }
    }
}
