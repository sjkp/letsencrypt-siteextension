using System;
using LetsEncrypt.Azure.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class RunFromPackageTest
    {
        [TestMethod]
        public void TestRunFromPackage()
        {
            var settings = new AppSettingsAuthConfig();

            Assert.AreEqual(false, settings.RunFromPackage);
        }
    }
}
