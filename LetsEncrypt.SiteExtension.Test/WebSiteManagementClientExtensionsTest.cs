using LetsEncrypt.Azure.Core;
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

        [TestMethod]
        public void ServerFarmName()
        {
            var serverFarmId = "/subscriptions/3f09c367-93e0-4b61-bbe5-dcb5c686bf8a/resourceGroups/LetsEncrypt-SiteExtension/providers/Microsoft.Web/serverfarms/sjkp.testplan";
            var res = WebSiteManagementClientExtensions.ServerFarmNameFromServerFarmId(serverFarmId);

            Assert.AreEqual("sjkp.testplan", res);
        }
    }
}
