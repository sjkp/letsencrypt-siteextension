using LetsEncrypt.Azure.Core.V2.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2.CertificateStores
{
    public interface ICertificateStore
    {
        Task<string> GetSecret(string name);
        Task SaveSecret(string name, string secret);

        Task<CertificateInfo> GetCertificate(string name, string password);
        Task SaveCertificate(CertificateInfo certificate);
    }
}
