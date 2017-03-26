using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core.Models
{
    public class CertificateInfo
    {
        public X509Certificate2 Certificate { get; set; }
        public string Name { get; set; }
        public string PfxCertificateBase64 { get; set; }
    }
}
