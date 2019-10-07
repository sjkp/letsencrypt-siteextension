using System.Collections.Generic;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.Services
{
    public interface IAuthorizationChallengeProvider
    {
        /// <summary>
        /// Returns the authorization status from lets encrypt. 
        /// </summary>
        /// <param name="dnsIdentifiers"></param>
        /// <returns></returns>
        Task<string> Authorize(Certes.Acme.IOrderContext context, List<string> dnsIdentifiers);    
    }
}
