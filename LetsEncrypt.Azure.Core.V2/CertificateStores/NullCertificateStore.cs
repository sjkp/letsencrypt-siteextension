using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.V2.Models;

namespace LetsEncrypt.Azure.Core.V2.CertificateStores
{
    internal class NullCertificateStore : ICertificateStore
    {
        public Task<CertificateInfo> GetCertificate(string name, string password)
        {
            return Task.FromResult<CertificateInfo>(null);
        }

        public Task<string> GetSecret(string name)
        {
            return Task.FromResult<string>(null);
        }

        public Task SaveCertificate(CertificateInfo certificate)
        {
            return Task.CompletedTask;
        }

        public Task SaveSecret(string name, string secret)
        {
            return Task.CompletedTask;
        }
    }
}