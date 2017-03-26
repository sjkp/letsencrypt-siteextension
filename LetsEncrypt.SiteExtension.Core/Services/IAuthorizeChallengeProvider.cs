using ACMESharp;
using LetsEncrypt.SiteExtension.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core.Services
{
    public interface IAuthorizeChallengeProvider
    {
        Task<AuthorizationState> Authorize(AcmeClient client, List<string> dnsIdentifiers);
    }
}
