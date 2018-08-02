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


        public FileSystemBase(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;

        }

        public async Task<CertificateInfo> GetCertificate(string name, string password)
        {
            if (! await this.fileSystem.Exists(name))
                return null;
            var pfx = await this.fileSystem.Read(name);
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
            this.fileSystem.Write(certificate.Name, certificate.PfxCertificate);
            return Task.FromResult(0);
        }
    }
}
