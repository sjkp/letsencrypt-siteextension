using ACMESharp.ACME;
using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.Azure.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class BlobStorageAuthorizationChallengeTest
    {
        [TestMethod]
        public async Task TestPersistDelete()
        {
            var testObj = new BlobStorageAuthorizationChallengeProvider(ConfigurationManager.AppSettings[AppSettingsAuthConfig.authorizationChallengeBlobStorageAccount]);

            ACMESharp.ACME.HttpChallenge challenge = new ACMESharp.ACME.HttpChallenge("http", new HttpChallengeAnswer())
            {
                FileContent = "test",
                FilePath = "/.well-known/acme-challenge/aBAasda234"
            };
            await testObj.PersistsChallengeFile(challenge);
            await testObj.CleanupChallengeFile(challenge);
        }

        [TestMethod]
        public async Task TestWebPersistDelete()
        {
            var testObj = new BlobStorageAuthorizationChallengeProvider(ConfigurationManager.AppSettings[AppSettingsAuthConfig.authorizationChallengeBlobStorageAccount], "$web");

            ACMESharp.ACME.HttpChallenge challenge = new ACMESharp.ACME.HttpChallenge("http", new HttpChallengeAnswer())
            {
                FileContent = "test",
                FilePath = "/.well-known/acme-challenge/aBAasda234"
            };
            await testObj.PersistsChallengeFile(challenge);
            await testObj.CleanupChallengeFile(challenge);
        }
    }
}
