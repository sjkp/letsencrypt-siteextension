using LetsEncrypt.SiteExtension.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core
{
    public class KuduRestClient
    {
        private readonly string baseUri;
        private HttpClient client;
        private string publishingPassword;
        private string publishingUserName;
        private string webAppName;

        public KuduRestClient(IAzureEnvironment azureEnvironment, string webAppName, string publishingUserName, string publishingPassword)
        {
            this.webAppName = webAppName;
            this.publishingUserName = publishingUserName;
            this.publishingPassword = publishingPassword;
            this.baseUri = $"https://{this.webAppName}.scm.{azureEnvironment.AzureWebSitesDefaultDomainName}";
            this.client = new HttpClient();
            client.BaseAddress = new System.Uri(baseUri);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", CreateToken());
        }

        private string CreateToken()
        {
            return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this.publishingUserName}:{this.publishingPassword}"));
        }

        public async Task<object> GetScmInfo()
        {
            var res = await client.GetStringAsync($"/api/scm/info");
            return JsonConvert.DeserializeObject<object>(res);
        }

        public async Task<Stream> GetFile(string path)
        {
            var res = await client.GetStreamAsync($"/api/vfs/{path}");

            return res;
        }

        public async Task<DirectoryInfo> GetDirectory(string path)
        {
            var res = await client.GetStringAsync($"/api/vfs/{path}/");
            return JsonConvert.DeserializeObject<DirectoryInfo>(res);
        }

        public async Task PutFile(string path, MemoryStream stream)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, this.baseUri + $"/api/vfs/{path}");


            stream.Position = 0;
            //var content = new ByteArrayContent(stream.ToArray());
            request.Content = new StreamContent(stream);
            request.Headers.Add("If-Match", "*");


            var res = await client.SendAsync(request);

            var body = await res.Content.ReadAsStringAsync();
        }
    }
}