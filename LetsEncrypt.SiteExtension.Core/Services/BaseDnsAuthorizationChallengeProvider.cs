using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.Azure.Core.Models;
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
    public abstract class BaseDnsAuthorizationChallengeProvider : IAuthorizationChallengeProvider
    {
        public async Task<AuthorizationState> Authorize(AcmeClient client, List<string> allDnsIdentifiers)
        {
            List<AuthorizationState> authStatus = new List<AuthorizationState>();

            foreach (var dnsIdentifier in allDnsIdentifiers)
            {
                Trace.TraceInformation("Authorizing Identifier {0} Using Challenge Type {1}", dnsIdentifier, AcmeProtocol.CHALLENGE_TYPE_DNS);
                var authzState = client.AuthorizeIdentifier(dnsIdentifier);                
                var challenge = client.DecodeChallenge(authzState, AcmeProtocol.CHALLENGE_TYPE_DNS);
                var dnsChallenge = challenge.Challenge as DnsChallenge;

                await PersistsChallenge(dnsChallenge);
                
                Trace.TraceInformation($" DNS Answer should now be available as {dnsChallenge.RecordName} with value {dnsChallenge.RecordValue}");

                try
                {
                    var retry = 10;
                    Trace.TraceInformation("Submitting answer");
                    authzState.Challenges = new AuthorizeChallenge[] { challenge };
                    client.SubmitChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_DNS, true);

                    // have to loop to wait for server to stop being pending. 
                    retry = 0;
                    while (authzState.Status == "pending" && retry < 6)
                    {
                        retry++;
                        Trace.TraceInformation("Refreshing authorization attempt " + retry);
                        await Task.Delay(2000*retry);  // this has to be here to give ACME server a chance to think
                        var newAuthzState = client.RefreshIdentifierAuthorization(authzState);
                        if (newAuthzState.Status != "pending")
                            authzState = newAuthzState;
                    }

                    Trace.TraceInformation("Auth Result {0}", authzState.Status);
                    if (authzState.Status == "invalid" || authzState.Status == "pending")
                    {
                        Trace.TraceError("Authorization Failed {0}", authzState.Status);
                        Trace.TraceInformation("Full Error Details {0}", JsonConvert.SerializeObject(authzState));
                        Trace.TraceError("Unable to find TXT record {0}", dnsChallenge.RecordValue);
                        throw new Exception($"The Lets Encrypt ACME server was probably unable to find TXT record with value {dnsChallenge.RecordValue} view error report from Lets Encrypt at {authzState.Uri} for more information");
                    }
                    authStatus.Add(authzState);
                }
                finally
                {
                    if (authzState.Status == "valid")
                    {
                        Console.WriteLine(" Deleting answer");
                        Trace.TraceInformation("Deleting answer");
                        await CleanupChallenge(dnsChallenge);
                    }
                }
            }
            foreach (var authState in authStatus)
            {
                if (authState.Status != "valid")
                {
                    return authState;
                }
            }
            return new AuthorizationState { Status = "valid" };
        }

        public abstract Task CleanupChallenge(DnsChallenge httpChallenge);

        public abstract Task PersistsChallenge(DnsChallenge httpChallenge);
    }
}
