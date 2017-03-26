using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.SiteExtension.Models;
using System.Configuration;
using System.IO;

namespace LetsEncrypt.SiteExtension.Core.Services
{
    public class KuduFileSystemAuthorizationChallengeProvider : BaseAuthorizationChannelgeProvider
    {
        private readonly KuduRestClient kuduClient;

        public KuduFileSystemAuthorizationChallengeProvider(AppSettingsAuthConfig config)
        {
            var website = ArmHelper.GetWebSiteManagementClient(config);
            this.kuduClient = KuduHelper.GetKuduClient(website, config);
        }
        public override Task CleanupChallengeFile(HttpChallenge challenge)
        {
            return Task.CompletedTask;
        }

        public override async Task EnsureWebConfig()
        {
            await WriteFile(WebRootPath() + "/.well-known/acme-challenge", webConfig);
        }

        public override async Task PersistsChallengeFile(HttpChallenge challenge)
        {
            var answerPath = GetAnswerPath(challenge);
            await WriteFile(answerPath, challenge.FileContent);
        }

        private async Task WriteFile(string answerPath, string content)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var streamwriter = new StreamWriter(ms);
                streamwriter.Write(content);
                streamwriter.Flush();
                await kuduClient.PutFile(answerPath, ms);          
            }
        }

        private static string GetAnswerPath(HttpChallenge httpChallenge)
        {
            // We need to strip off any leading '/' in the path
            var filePath = httpChallenge.FilePath;
            if (filePath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                filePath = filePath.Substring(1);
            var answerPath = WebRootPath() + "/" +filePath;
            return answerPath;
        }

        private static string WebRootPath()
        {
            return ConfigurationManager.AppSettings["letsencrypt:WebRootPath"] ?? "site/wwwroot";
        }
    }
}
