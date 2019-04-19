using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.V2.Models;

namespace LetsEncrypt.Azure.Core.V2.CertificateStores
{
    public abstract class FileSystemBase : ICertificateStore
    {
        private readonly IFileSystem fileSystem;
        private const string fileExtension = ".pfx";

        public FileSystemBase(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;

        }

        public async Task<CertificateInfo> GetCertificate(string name, string password)
        {
            var filename = name + fileExtension;
            if (! await this.fileSystem.Exists(filename))
                return null;
            var pfx = await this.fileSystem.Read(filename);
            return new CertificateInfo()
            {
                PfxCertificate = pfx,
                Certificate = new X509Certificate2(pfx, password, X509KeyStorageFlags.DefaultKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable),
                Name = name,
                Password = password
            };
        }

        public Task SaveCertificate(CertificateInfo certificate)
        {
            this.fileSystem.Write(certificate.Name+fileExtension, certificate.PfxCertificate);
            return Task.CompletedTask;
        }

        public async Task<string> GetSecret(string name)
        {
            var filename = name + fileExtension;
            if (!await this.fileSystem.Exists(filename))
                return null;
            return System.Text.Encoding.UTF8.GetString(await this.fileSystem.Read(filename));
        }

        public Task SaveSecret(string name, string secret)
        {
            this.fileSystem.Write(name + fileExtension, Encoding.UTF8.GetBytes(secret));
            return Task.CompletedTask;
        }
    }
}
