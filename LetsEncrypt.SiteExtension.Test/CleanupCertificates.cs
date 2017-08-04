using LetsEncrypt.Azure.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class CleanupCertificates
    {
        AppSettingsAuthConfig config;

        [TestInitialize()]
        public void SetupCleanupCertificates()
        {
            config = new AppSettingsAuthConfig();
        }

        [TestMethod]
        public async Task MyTestMethod()
        {
            var httpClient = new HttpClient();
            var token = await GetAuthorizationToken(httpClient);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + token);

            var resourceGroups = await GetResourceGroups(httpClient);

            var tasks = new ConcurrentBag<Task>();
            resourceGroups.AsParallel().ForAll(rg =>
            {
                var t = GetCertificates(httpClient, rg).ContinueWith(async certificates =>
                {
                    var c = await certificates;
                    c.value.AsParallel().ForAll(async certs =>
                    {
                        Console.WriteLine(certs.properties.friendlyName + " " + certs.id);
                        await await httpClient.DeleteAsync($"https://management.azure.com{certs.id}?api-version=2016-03-01").ContinueWith(async deleteDone =>
                        {
                            Console.WriteLine("Deleted " + certs.id + (await deleteDone).StatusCode);
                        });                        
                    });
                    Console.WriteLine("Inner done");
                });
                tasks.Add(t);
                Console.WriteLine("Outer done");
            });
            Console.WriteLine("Wait all now");
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Wait 5 sec now");
            await Task.Delay(5000);
        }

        private async Task<CertificatesResponse> GetCertificates(HttpClient httpClient, string s)
        {
            Console.WriteLine(s);
            var res = await httpClient.GetStringAsync($"https://management.azure.com{s}/providers/Microsoft.Web/certificates?api-version=2016-03-01");



            return JsonConvert.DeserializeObject<CertificatesResponse>(res);
        }

        private async Task<string[]> GetResourceGroups(HttpClient httpClient)
        {
            var res = await httpClient.GetAsync($"https://management.azure.com/subscriptions/{config.SubscriptionId}/resourceGroups?api-version=2017-06-01");

            var result = JsonConvert.DeserializeObject<ResourceGroupResponse>(await res.Content.ReadAsStringAsync());

            return result.value.Select(s => s.id.ToString()).ToArray();
        }

        private async Task<string> GetAuthorizationToken(HttpClient client)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{config.Tenant}/oauth2/token");

            req.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
            req.Content = new StringContent($"grant_type=client_credentials&client_id={config.ClientId}&client_secret={config.ClientSecret}&resource=https%3A%2F%2Fmanagement.azure.com%2F", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await client.SendAsync(req);
            var result = await response.Content.ReadAsAsync<dynamic>();
            return result.access_token;
        }


        public class ResourceGroupResponse
        {
            public Value[] value { get; set; }

            public class Value
            {
                public string id { get; set; }
                public string name { get; set; }
                public string location { get; set; }
                public Properties properties { get; set; }
            }

            public class Properties
            {
                public string provisioningState { get; set; }
            }
        }


        public class CertificatesResponse
        {
            public Value[] value { get; set; }
            public string nextLink { get; set; }
            public string id { get; set; }

            public class Value
            {
                public string id { get; set; }
                public string name { get; set; }
                public string type { get; set; }
                public string location { get; set; }
                public Properties properties { get; set; }
            }

            public class Properties
            {
                public string friendlyName { get; set; }
                public string subjectName { get; set; }
                public string[] hostNames { get; set; }
                public object pfxBlob { get; set; }
                public object siteName { get; set; }
                public object selfLink { get; set; }
                public string issuer { get; set; }
                public DateTime issueDate { get; set; }
                public DateTime expirationDate { get; set; }
                public object password { get; set; }
                public string thumbprint { get; set; }
                public object valid { get; set; }
                public object toDelete { get; set; }
                public object cerBlob { get; set; }
                public object publicKeyHash { get; set; }
                public object hostingEnvironment { get; set; }
                public object hostingEnvironmentProfile { get; set; }
                public string keyVaultSecretStatus { get; set; }
                public string webSpace { get; set; }
                public object serverFarmId { get; set; }
                public object tags { get; set; }
            }
        }

        



    }
}
