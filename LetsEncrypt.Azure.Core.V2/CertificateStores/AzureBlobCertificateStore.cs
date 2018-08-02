using System;
using System.Collections.Generic;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.CertificateStores
{
    public class AzureBlobCertificateStore : FileSystemBase
    {
        public AzureBlobCertificateStore(AzureBlobStorage azureBlobStorage) : base(azureBlobStorage)
        {
        }
    }
}
