using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.Services
{
    public abstract class BaseHttpAuthorizationChallengeProvider : IAuthorizationChallengeProvider
    {
        protected const string webConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <system.webServer>
    <handlers>
      <clear />
      <add name=""ACMEStaticFile"" path=""*"" verb=""*"" modules=""StaticFileModule"" resourceType=""Either"" requireAccess=""Read"" />
    </handlers>
    <staticContent>
      <remove fileExtension=""."" />
      <mimeMap fileExtension=""."" mimeType=""text/plain"" />
    </staticContent>
  </system.webServer>
  <system.web>
    <authorization>
      <allow users=""*""/>
    </authorization>
  </system.web>
</configuration>";
        
        public virtual Task EnsureDirectory() {
            return Task.CompletedTask;
        }

        public virtual Task EnsureWebConfig()
        {
            return Task.CompletedTask;
        }

        public abstract Task PersistsChallengeFile(string path, string context);

        public abstract Task CleanupChallengeFile(string path);


        public async Task<(bool success, string errorMsg)> Authorize(IOrderContext order, List<string> allDnsIdentifiers)
        {
            await EnsureDirectory();
            await EnsureWebConfig();

            var auths = await order.Authorizations();
            var i = 0;
            foreach (var auth in auths)
            {                

                var authz = await auth.Http();
                var tokenRelativeUri = $".well-known/acme-challenge/{authz.Token}";
                await PersistsChallengeFile(tokenRelativeUri, authz.KeyAuthz);
                var challengeUri = new Uri($"http://{allDnsIdentifiers.ElementAt(i)}/{tokenRelativeUri}");
                var retry = await ValidateChallengeFile(challengeUri);
                if (retry == 0)
                    throw new Exception($"Unable to validate presence of http challenge at {challengeUri} ensure that it is browsable");

                var response = await authz.Validate(); 
                retry = 10;
                while ((response.Status == ChallengeStatus.Pending || response.Status == ChallengeStatus.Processing) && retry-- > 0)
                {
                    Trace.TraceInformation($"Dns challenge response status {response.Status} more info at {response.Url.ToString()} retrying in 5 sec");
                    await Task.Delay(5000);
                    response = await authz.Resource();
                }

                Console.WriteLine($" Authorization Result: {response.Status}");
                Trace.TraceInformation("Auth Result {0}", response.Status);
                if (response.Status != ChallengeStatus.Valid)
                {

                    return (false, JsonConvert.SerializeObject(response.Error));
                }
                i++;
            }
            return (true, string.Empty);
        }


        private static async Task<int> ValidateChallengeFile(Uri answerUri)
        {
            Console.WriteLine($" Answer should now be browsable at {answerUri}");
            Trace.TraceInformation("Answer should now be browsable at {0}", answerUri);


            var retry = 10;
            var handler = new WebRequestHandler();
            handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            var httpclient = new HttpClient(handler);
            while (true)
            {

                //Allow self-signed certs otherwise staging wont work


                await Task.Delay(1000);
                var x = await httpclient.GetAsync(answerUri);
                Trace.TraceInformation("Checking status {0}", x.StatusCode);
                if (x.StatusCode == HttpStatusCode.OK)
                    break;
                if (retry-- == 0)
                    break;
                Trace.TraceInformation("Retrying {0}", retry);
            }

            return retry;
        }
    }
}
