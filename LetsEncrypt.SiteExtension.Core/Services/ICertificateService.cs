using LetsEncrypt.Azure.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.Services
{
    public interface ICertificateService
    {
        Task Install(ICertificateInstallModel model);

        Task<List<string>> RemoveExpired(int removeXNumberOfDaysBeforeExpiration = 0);
    }
}
