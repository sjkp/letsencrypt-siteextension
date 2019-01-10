using System;
using System.Net.Http;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LetsEncrypt.Azure.Core.ArmHelper;

namespace LetsEncrypt.SiteExtension.Test
{
    [TestClass]
    public class HttpTets
    {
        [TestMethod]
        [Ignore]
        public async Task RetryTest()
        {
            var client = HttpClientFactory.Create(new HttpClientHandler(), new TimeoutHandler());
            
            var retry = ArmHelper.ExponentialBackoff();
            await retry.ExecuteAsync(async () =>
            {
                await client.PostAsJsonAsync("https://en8zkq5hogjyi.x.pipedream.net", new { text = "hello" });
            });
        }
    }
}
