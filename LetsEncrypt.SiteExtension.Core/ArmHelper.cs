using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.WebSites;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Polly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core
{
    public static class ArmHelper
    {
        public static async Task<WebSiteManagementClient> GetWebSiteManagementClient(IAzureWebAppEnvironment model)
        {            
            AuthenticationResult token = await GetToken(model);
            var creds = new TokenCredentials(token.AccessToken);

            var websiteClient = new WebSiteManagementClient(model.ManagementEndpoint, creds);
            websiteClient.SubscriptionId = model.SubscriptionId.ToString();
            return websiteClient;
        }

        public static async Task<DnsManagementClient> GetDnsManagementClient(IAzureDnsEnvironment model)
        {
            AuthenticationResult token = await GetToken(model);
            var creds = new TokenCredentials(token.AccessToken);

            var dnsClient = new DnsManagementClient(model.ManagementEndpoint, creds);
            dnsClient.SubscriptionId = model.SubscriptionId.ToString();
            return dnsClient;
        }

        private static async Task<AuthenticationResult> GetToken(IAzureEnvironment model)
        {
            var authContext = new AuthenticationContext(model.AuthenticationEndpoint + model.Tenant);

            var token = await authContext.AcquireTokenAsync(model.TokenAudience.ToString(), new ClientCredential(model.ClientId.ToString(), model.ClientSecret));
            return token;
        }

        public static async Task<HttpClient> GetHttpClient(IAzureWebAppEnvironment model)
        {        
            AuthenticationResult token = await GetToken(model);

            var client = HttpClientFactory.Create(new HttpClientHandler(), new TimeoutHandler());
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token.AccessToken);
            client.BaseAddress = model.ManagementEndpoint;

            return client;
        }

        public static Polly.Retry.RetryPolicy<HttpResponseMessage> ExponentialBackoff(int retryCount = 3, int firstBackOffDelay = 2)
        {
            return Policy
          .HandleResult<HttpResponseMessage>((resp) =>
          {
              return IsTransient(resp.StatusCode);
              
          })
          .WaitAndRetryAsync(retryCount, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(firstBackOffDelay, retryAttempt))
          );
        }

        private static bool IsTransient(HttpStatusCode statusCode)
        {
            return new HttpStatusCode[]
    {
            HttpStatusCode.BadGateway,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.Unauthorized
    }.Contains(statusCode);
        }

        public class TimeoutHandler : DelegatingHandler
        {
            private static TimeSpan Timeout = TimeSpan.FromSeconds(120);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(Timeout);
                var timeoutToken = cts.Token;

                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken);

                try
                {
                    return await base.SendAsync(request, linkedToken.Token);
                }
                catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }
            }
        }
    }
}