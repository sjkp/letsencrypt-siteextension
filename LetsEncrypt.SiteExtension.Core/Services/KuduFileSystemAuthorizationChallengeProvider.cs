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
    public class KuduFileSystemAuthorizationChallengeProvider : BaseHttpAuthorizationChallengeProvider
    {
        
        private readonly IAuthorizationChallengeProviderConfig config;
        private readonly IAzureWebAppEnvironment azureEnvironment;
        private readonly PathProvider pathProvider;

        public KuduFileSystemAuthorizationChallengeProvider(IAzureWebAppEnvironment azureEnvironment, IAuthorizationChallengeProviderConfig config)
        {
            this.config = config;
            
            this.azureEnvironment = azureEnvironment;
            this.pathProvider = new PathProvider(azureEnvironment);
        }

        public override Task CleanupChallengeFile(HttpChallenge challenge)
        {
            return Task.CompletedTask;
        }

        public override async Task EnsureWebConfig()
        {
            var dir = await this.pathProvider.ChallengeDirectory(true);
            if (config.DisableWebConfigUpdate)
            {
                Trace.TraceInformation($"Disabled updating web.config at {dir}");
                return;
            }
            await WriteFile(dir + "/web.config", webConfig);
        }

        public override async Task PersistsChallengeFile(HttpChallenge challenge)
        {
            var answerPath = await GetAnswerPath(challenge);
            await WriteFile(answerPath, challenge.FileContent);
        }

        private async Task WriteFile(string answerPath, string content)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var streamwriter = new StreamWriter(ms);
                streamwriter.Write(content);
                streamwriter.Flush();
                await (await GetKuduRestClient()).PutFile(answerPath, ms);          
            }
        }

        private async Task<string> GetAnswerPath(HttpChallenge httpChallenge)
        {
            var root = await this.pathProvider.WebRootPath(true);
            // We need to strip off any leading '/' in the path
            var filePath = httpChallenge.FilePath;
            if (filePath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                filePath = filePath.Substring(1);
            var answerPath = root + "/" +filePath;
            return answerPath;
        }


        private async Task<KuduRestClient> GetKuduRestClient()
        {
            var website = await ArmHelper.GetWebSiteManagementClient(azureEnvironment);
            return KuduHelper.GetKuduClient(website, azureEnvironment);
        }
    }
}
