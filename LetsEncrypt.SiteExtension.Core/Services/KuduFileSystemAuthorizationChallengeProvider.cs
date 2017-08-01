using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp;
using ACMESharp.ACME;
using System.Configuration;
using System.IO;
using LetsEncrypt.Azure.Core.Models;
using System.Diagnostics;

namespace LetsEncrypt.Azure.Core.Services
{
    public class KuduFileSystemAuthorizationChallengeProvider : BaseAuthorizationChallengeProvider
    {
        private readonly KuduRestClient kuduClient;
        private readonly IAuthorizationChallengeProviderConfig config;

        public KuduFileSystemAuthorizationChallengeProvider(IAzureEnvironment azureEnvironment, IAuthorizationChallengeProviderConfig config)
        {
            this.config = config;
            var website = ArmHelper.GetWebSiteManagementClient(azureEnvironment);
            this.kuduClient = KuduHelper.GetKuduClient(website, azureEnvironment);
        }
        public override Task CleanupChallengeFile(HttpChallenge challenge)
        {
            return Task.CompletedTask;
        }

        public override async Task EnsureWebConfig()
        {
            if (config.DisableWebConfigUpdate)
            {
                Trace.TraceInformation($"Disabled updating web.config at {WebRootPath() }");
                return;
            }
            await WriteFile(WebRootPath() + "/.well-known/acme-challenge/web.config", webConfig);
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
            
            var webrootPath = ConfigurationManager.AppSettings["letsencrypt:WebRootPath"];
            if (string.IsNullOrEmpty(webrootPath))
                return "site/wwwroot";
            //Ensure this is a backwards compatible with the LocalFileSystemProvider that was the only option before
            return webrootPath.Replace(Environment.ExpandEnvironmentVariables("%HOME%"), "").Replace('\\', '/');
        }
    }
}
