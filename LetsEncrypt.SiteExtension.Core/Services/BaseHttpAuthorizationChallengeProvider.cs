using ACMESharp;
using ACMESharp.ACME;
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
using System.Text;
using System.Threading;
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
        private ACMESharp.AcmeClient client;
        private IOrderContext order;

        public virtual Task EnsureDirectory() {
            return Task.CompletedTask;
        }

        public virtual Task EnsureWebConfig()
        {
            return Task.CompletedTask;
        }

        public abstract Task PersistsChallengeFile(string path, string context);

        public abstract Task CleanupChallengeFile(string path);

        public void RegisterClient(object client)
        {
            if (client is ACMESharp.AcmeClient)
                this.client = client as ACMESharp.AcmeClient;
            else if (client as IOrderContext != null)
                this.order = client as IOrderContext;
            else
                throw new InvalidProgramException($"Unable to determine type of AcmeClient {client.GetType()}");
        }

        public async Task<string> Authorize(List<string> allDnsIdentifiers)
        {
            if (client != null)
            {
                return await this.AuthorizeOld(allDnsIdentifiers);
            }
            if (order != null)
            {
                return await this.AuthorizeNew(allDnsIdentifiers);
            }

            throw new Exception("order and client are both null");
        }

        private async Task<string> AuthorizeNew(List<string> allDnsIdentifiers)
        {
            await EnsureDirectory();
            await EnsureWebConfig();

            var auths = await order.Authorizations();
            foreach (var auth in auths)
            {                

                var authz = await auth.Http();
                var tokenRelativeUri = $".well-known/acme-challenge/{authz.Token}";
                await PersistsChallengeFile(tokenRelativeUri, authz.KeyAuthz);
                var challengeUri = new Uri($"http://{allDnsIdentifiers.First()}/{tokenRelativeUri}");
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
                    return response.Status.ToString();
                }
            }
            return "valid";
        }

        public async Task<string> AuthorizeOld(List<string> allDnsIdentifiers)
        {
            List<AuthorizationState> authStatus = new List<AuthorizationState>();

            await EnsureDirectory();
            await EnsureWebConfig();

            foreach (var dnsIdentifier in allDnsIdentifiers)
            {
                //var dnsIdentifier = target.Host;                
                Console.WriteLine($"\nAuthorizing Identifier {dnsIdentifier} Using Challenge Type {AcmeProtocol.CHALLENGE_TYPE_HTTP}");
                Trace.TraceInformation("Authorizing Identifier {0} Using Challenge Type {1}", dnsIdentifier, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                var authzState = client.AuthorizeIdentifier(dnsIdentifier);                
                var challenge = client.DecodeChallenge(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                var httpChallenge = challenge.Challenge as HttpChallenge;

                await PersistsChallengeFile(httpChallenge.FilePath, httpChallenge.FileContent);
                try
                {
                    var answerUri = new Uri(httpChallenge.FileUrl);
                    int retry = await ValidateChallengeFile(answerUri);
                    Console.WriteLine(" Submitting answer");
                    Trace.TraceInformation("Submitting answer");
                    authzState.Challenges = new AuthorizeChallenge[] { challenge };
                    client.SubmitChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP, true);

                    // have to loop to wait for server to stop being pending. 
                    retry = 0;
                    while (authzState.Status == "pending" && retry < 6)
                    {
                        retry++;
                        Console.WriteLine(" Refreshing authorization attempt " + retry);
                        Trace.TraceInformation("Refreshing authorization attempt " + retry);
                        await Task.Delay(2000 * retry);  // this has to be here to give ACME server a chance to think
                        var newAuthzState = client.RefreshIdentifierAuthorization(authzState);
                        if (newAuthzState.Status != "pending")
                            authzState = newAuthzState;
                    }

                    Console.WriteLine($" Authorization Result: {authzState.Status}");
                    Trace.TraceInformation("Auth Result {0}", authzState.Status);
                    if (authzState.Status == "invalid" || authzState.Status == "pending")
                    {
                        Trace.TraceError("Authorization Failed {0}", authzState.Status);
                        Trace.TraceInformation("Full Error Details {0}", JsonConvert.SerializeObject(authzState));
                        Console.WriteLine($"The ACME server was probably unable to reach {answerUri}");
                        Trace.TraceError("Unable to reach {0}", answerUri);
                        Console.WriteLine("\nCheck in a browser to see if the answer file is being served correctly.");
                        throw new Exception($"The Lets Encrypt ACME server was probably unable to reach {answerUri} view error report from Lets Encrypt at {authzState.Uri} for more information");
                    }
                    authStatus.Add(authzState);
                }
                finally
                {
                    if (authzState.Status == "valid")
                    {
                        Console.WriteLine(" Deleting answer");
                        Trace.TraceInformation("Deleting answer");
                        await CleanupChallengeFile(httpChallenge.FilePath);
                    }
                }
            }
            foreach (var authState in authStatus)
            {
                if (authState.Status != "valid")
                {
                    return authState.Status;
                }
            }
            return "valid";
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
