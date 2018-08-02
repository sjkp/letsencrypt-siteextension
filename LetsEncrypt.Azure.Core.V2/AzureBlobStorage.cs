using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public class AzureBlobStorage : IFileSystem
    {
        private CloudStorageAccount storageAccount;

        public AzureBlobStorage(string connectionString)
        {
            this.storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        public async Task<bool> Exists(string v)
        {
            CloudBlockBlob blob = await GetBlob(v);
            return await blob.ExistsAsync();
        }

        private async Task<CloudBlockBlob> GetBlob(string v)
        {
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference("letsencrypt");
            await container.CreateIfNotExistsAsync();

            
            var blob = container.GetBlockBlobReference(v);

            return blob;
        }

        public async Task<string> ReadAllText(string v)
        {
            var blob = await GetBlob(v);
            return await blob.DownloadTextAsync();
        }

        public async Task WriteAllText(string v, string pemKey)
        {
            var blob = await GetBlob(v);
            await blob.UploadTextAsync(pemKey);
        }

        public async Task<byte[]> Read(string v)
        {
            var blob = await GetBlob(v);
            using (var ms = new MemoryStream())
            using (var data = await blob.OpenReadAsync())
            {
                await data.CopyToAsync(ms);
                return ms.ToArray();
            }           
        }

        public async Task Write(string v, byte[] data)
        {
            var blob = await GetBlob(v);
            using (var ms = new MemoryStream(data))
                await blob.UploadFromStreamAsync(ms);
        }
    }
}
