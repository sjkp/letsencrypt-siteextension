using System;
using System.Collections.Generic;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.CertificateStores
{
    public class FileSystemCertificateStore : FileSystemBase
    {
        public FileSystemCertificateStore() : base(new FileSystem())
        {
        }
    }
}
