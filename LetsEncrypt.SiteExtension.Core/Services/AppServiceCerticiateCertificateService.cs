using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.Models;

namespace LetsEncrypt.Azure.Core.Services
{
    class AppServiceCerticiateCertificateService : ICertificateService
    {
        public async Task Install(ICertificateInstallModel model)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> RemoveExpired(int removeXNumberOfDaysBeforeExpiration = 0)
        {
            throw new NotImplementedException();
        }
    }
}
