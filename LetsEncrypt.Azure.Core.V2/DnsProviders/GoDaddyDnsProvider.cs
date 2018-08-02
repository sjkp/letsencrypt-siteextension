using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2.DnsProviders
{
    public class GoDaddyDnsProvider : IDnsProvider
    {
        private readonly HttpClient httpClient;

        public GoDaddyDnsProvider(GoDaddyDnsSettings settings)
        {
            this.httpClient = new HttpClient();
            this.httpClient.BaseAddress = new Uri($"https://api.godaddy.com/v1/domains/{settings.Domain}/");
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"sso-key {settings.ApiKey}:{settings.ApiSecret}");
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Shopper-Id", settings.ShopperId);
        }

        public int MinimumTtl => 600;

        public Task Cleanup(string recordSetName)
        {
            return Task.FromResult(0);
        }

        public async Task PersistChallenge(string recordSetName, string recordValue)
        {
            var body = await httpClient.GetStringAsync($"records/TXT/{recordSetName}");
            var acmeChallengeRecord = JsonConvert.DeserializeObject<DnsRecord[]>(body);


            acmeChallengeRecord = new[]{new DnsRecord
                {
                    data = recordValue,
                    name = recordSetName,
                    ttl = MinimumTtl,
                    type = "TXT"
                }};

            var res = await this.httpClient.PutAsync($"records/TXT/{recordSetName}", new StringContent(JsonConvert.SerializeObject(acmeChallengeRecord), Encoding.UTF8, "application/json"));
            body = await res.Content.ReadAsStringAsync();
            res.EnsureSuccessStatusCode();

        }

        public class GoDaddyDnsSettings
        {
            public string ApiKey { get; set; }
            public string ApiSecret { get; set; }
            public string ShopperId { get; set; }
            public string Domain { get; set; }
        }


        public class DnsRecord
        {
            public string data { get; set; }
            public string name { get; set; }
            public int ttl { get; set; }
            public string type { get; set; }
        }

    }
}
