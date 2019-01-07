using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LetsEncrypt.Azure.Core.Services
{
    public class BlobStorageAuthorizationChallengeProvider : BaseHttpAuthorizationChallengeProvider
    {
        private readonly CloudStorageAccount storageAccount;
        private readonly string containerName;

        public BlobStorageAuthorizationChallengeProvider(string storageConnectionString, string container = null)
        {
            storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            this.containerName = string.IsNullOrEmpty(container) ? "letsencrypt-siteextension" : container;            
        }
   
        public override async Task CleanupChallengeFile(HttpChallenge challenge)
        {
            var blob = await GetBlob(challenge);
            await blob.DeleteIfExistsAsync();
        }

        public override async Task PersistsChallengeFile(HttpChallenge challenge)
        {
            CloudBlockBlob blob = await GetBlob(challenge);
            blob.Properties.ContentType = "text/plain";
            
            await blob.UploadTextAsync(challenge.FileContent);
            await blob.SetPropertiesAsync();
        }

        private async Task<CloudBlockBlob> GetBlob(HttpChallenge challenge)
        {
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            // We need to strip off any leading '/' in the path
            var filePath = challenge.FilePath;
            if (filePath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                filePath = filePath.Substring(1);
            var blob = container.GetBlockBlobReference(filePath);
            return blob;
        }
    }
}
