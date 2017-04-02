using LetsEncrypt.SiteExtension.Core.Models;
using LetsEncrypt.SiteExtension.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core.Services
{
    public interface ICertificateService
    {
        void Install(ICertificateInstallModel model);

        List<string> RemoveExpired(int removeXNumberOfDaysBeforeExpiration = 0);
    }
}
