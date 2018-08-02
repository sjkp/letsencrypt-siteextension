using LetsEncrypt.Azure.Core.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Letsencrypt.Azure.Core.Test
{
    [TestClass]
    public class AzureBlobStorageTest
    {
        [TestMethod]
        public async Task AzureBlobTest()
        {
            var config = new ConfigurationBuilder()
              .AddUserSecrets<AzureBlobStorageTest>()
              .Build();

            var storage = new AzureBlobStorage(config["AzureStorageConnectionString"]);

            var filename = Guid.NewGuid().ToString();

            Assert.IsFalse(await storage.Exists(filename));
            await ValidateBinaryWrite(storage, filename, "hello world");
            //Assert that we can overwrite existing
            await ValidateBinaryWrite(storage, filename, "hello world 2");

            await ValidateTextWrite(storage, filename + ".txt", "text content");

        }

        private static async Task ValidateBinaryWrite(AzureBlobStorage storage, string filename, string txtcontent)
        {
            await storage.Write(filename, Encoding.UTF8.GetBytes(txtcontent));

            Assert.IsTrue(await storage.Exists(filename));

            var content = await storage.Read(filename);

            Assert.AreEqual(txtcontent, Encoding.UTF8.GetString(content));
        }

        private static async Task ValidateTextWrite(AzureBlobStorage storage, string filename, string txtcontent)
        {
            await storage.WriteAllText(filename, txtcontent);

            Assert.IsTrue(await storage.Exists(filename));

            var content = await storage.ReadAllText(filename);

            Assert.AreEqual(txtcontent, content);
        }
    }
}
