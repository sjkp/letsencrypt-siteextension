using System;
using System.Threading.Tasks;
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
   
        public override async Task CleanupChallengeFile(string filePath)
        {
            var blob = await GetBlob(filePath);
            await blob.DeleteIfExistsAsync();
        }

        public override async Task PersistsChallengeFile(string filePath, string fileContent)
        {
            CloudBlockBlob blob = await GetBlob(filePath);
            blob.Properties.ContentType = "text/plain";
            
            await blob.UploadTextAsync(fileContent);
            await blob.SetPropertiesAsync();
        }

        private async Task<CloudBlockBlob> GetBlob(string filePath)
        {
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            if (!"$web".Equals(containerName, StringComparison.InvariantCultureIgnoreCase) && container.Properties.PublicAccess != BlobContainerPublicAccessType.Blob)
            {
                await container.SetPermissionsAsync(new BlobContainerPermissions()
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
            }
            // We need to strip off any leading '/' in the path
            if (filePath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                filePath = filePath.Substring(1);
            var blob = container.GetBlockBlobReference(filePath);
            return blob;
        }
    }
}
