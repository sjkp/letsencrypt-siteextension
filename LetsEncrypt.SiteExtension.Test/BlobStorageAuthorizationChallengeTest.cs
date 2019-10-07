using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class BlobStorageAuthorizationChallengeTest
    {
        const string FileContent = "test";
        const string FilePath = "/.well-known/acme-challenge/aBAasda234";
        [TestMethod]
        public async Task TestPersistDelete()
        {
            var testObj = new BlobStorageAuthorizationChallengeProvider(ConfigurationManager.AppSettings[AppSettingsAuthConfig.authorizationChallengeBlobStorageAccount]);

            
            await testObj.PersistsChallengeFile(FilePath, FileContent);
            await testObj.CleanupChallengeFile(FilePath);
        }

        [TestMethod]
        public async Task TestWebPersistDelete()
        {
            var testObj = new BlobStorageAuthorizationChallengeProvider(ConfigurationManager.AppSettings[AppSettingsAuthConfig.authorizationChallengeBlobStorageAccount], "$web");

            
            await testObj.PersistsChallengeFile(FilePath,FileContent);
            await testObj.CleanupChallengeFile(FilePath);
        }
    }
}
