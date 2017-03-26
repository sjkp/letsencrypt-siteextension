using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.SiteExtension.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core.Services
{
    public class LocalFileSystemAuthorizationChallengeProvider : BaseAuthorizationChannelgeProvider
    {       
        private readonly AppSettingsAuthConfig config;

        public LocalFileSystemAuthorizationChallengeProvider(AppSettingsAuthConfig config)
        {
            this.config = config;
        }


       

        private static string WebRootPath()
        {
            return ConfigurationManager.AppSettings["letsencrypt:WebRootPath"] ?? Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), "site", "wwwroot");
        }

        private string ChallengeDirectory
        {
            get
            {
                var webRootPath = WebRootPath();
                var directory = Path.Combine(webRootPath, ".well-known", "acme-challenge");
                return directory;
            }
        }

        public override Task EnsureDirectory()
        {
            
            if (!Directory.Exists(ChallengeDirectory))
            {
                Directory.CreateDirectory(ChallengeDirectory);
            }
            return Task.CompletedTask;
        }

        public override Task EnsureWebConfig()
        {
            var webConfigPath = Path.Combine(ChallengeDirectory, "web.config");
            if (config.DisableWebConfigUpdate)
            {
                Trace.TraceInformation($"Disabled updating web.config at {webConfigPath}");
            }
            else
            {
                if ((!File.Exists(webConfigPath) || File.ReadAllText(webConfigPath) != webConfig))
                {
                    Trace.TraceInformation($"Writing web.config to {webConfigPath}");
                    File.WriteAllText(webConfigPath, webConfig);
                }
            }
            return Task.CompletedTask;
        }

        public override Task PersistsChallengeFile(HttpChallenge httpChallenge)
        {
            string answerPath = GetAnswerPath(httpChallenge);

            Console.WriteLine($" Writing challenge answer to {answerPath}");
            Trace.TraceInformation("Writing challenge answer to {0}", answerPath);

            File.WriteAllText(answerPath, httpChallenge.FileContent);
            return Task.CompletedTask;
        }

        private static string GetAnswerPath(HttpChallenge httpChallenge)
        {
            // We need to strip off any leading '/' in the path
            var filePath = httpChallenge.FilePath;
            if (filePath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                filePath = filePath.Substring(1);
            var answerPath = Environment.ExpandEnvironmentVariables(Path.Combine(WebRootPath(), filePath));
            return answerPath;
        }

        public override Task CleanupChallengeFile(HttpChallenge challenge)
        {
            File.Delete(GetAnswerPath(challenge));
            return Task.CompletedTask;
        }
    }
}
