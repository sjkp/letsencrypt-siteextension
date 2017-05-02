using LetsEncrypt.Azure.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class WebAppEnviromentVariablesTest
    {
        [TestMethod]
        public void WetSiteOwner()
        {
            var variables = new WebAppEnviromentVariables();

            Assert.AreEqual(new Guid("688bf064-900b-4e8f-9598-2d9be0718133"), variables.SubscriptionId);
            Assert.AreEqual("Tiimo+.Web-.Dev1", variables.ResourceGroupName);
        }
    }
}
