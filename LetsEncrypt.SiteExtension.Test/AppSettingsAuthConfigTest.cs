using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LetsEncrypt.SiteExtension.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class AppSettingsAuthConfigTest
    {
        [TestMethod]
        public void IsValid()
        {
            List<ValidationResult> res = null;
            var config = new AppSettingsAuthConfig();

            

            Assert.IsFalse(config.IsValid(out res));
            Console.WriteLine(string.Join(" ,", res.Select(s => s.ErrorMessage)));

        }
    }
}
