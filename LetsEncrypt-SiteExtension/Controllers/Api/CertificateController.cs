using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.Models;
using LetsEncrypt.SiteExtension.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace LetsEncrypt.SiteExtension.Controllers.Api
{
    [ValidateApiVersion]
    public class CertificateController : ApiController
    {
        /// <summary>
        /// Renews the already configured Let's Encrypt certificate for the web app. 
        /// The settings for the renewal process must exists as app settings, or a
        /// manually installed certificate must have been installed at least once.
        /// </summary>
        /// <param name="apiversion"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/certificates/renew")]    
        [ResponseType(typeof(List<CertificateInstallModel>))]    
        public async Task<IHttpActionResult> RenewExisting([FromUri(Name = "api-version")]string apiversion = null)
        {            
            Trace.TraceInformation("Renew certificate");
            var config = new AppSettingsAuthConfig();
            var res = await new CertificateManager(new AppSettingsAuthConfig()).RenewCertificate(renewXNumberOfDaysBeforeExpiration: config.RenewXNumberOfDaysBeforeExpiration);
            Trace.TraceInformation($"Completed renewal of '{res.Count()}' certificates");

            return Ok(res);
        }

        /// <summary>
        /// Installs a Let's Encrypt certificate onto a azure web using http challenge and the 
        /// kudu file API to place the challenge file on the web app.
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="apiversion"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/certificates/challengeprovider/http/kudu/certificateinstall/azurewebapp")]
        [ResponseType(typeof(CertificateInstallModel))]
        public async Task<IHttpActionResult> GenerateAndInstall(HttpKuduInstallModel model, [FromUri(Name = "api-version")]string apiversion = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var mgr = CertificateManager.CreateKuduWebAppCertificateManager(model.AzureEnvironment, model.AcmeConfig, model.CertificateSettings, model.AuthorizationChallengeProviderConfig);

            return Ok(await mgr.AddCertificate());
        }

        /// <summary>
        /// Requests a Let's Encrypt certificate using the DNS challenge, using Azure DNS. The 
        /// certificate is installed using the k
        /// </summary>
        /// <param name="model"></param>
        /// <param name="apiversion"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/certificates/challengeprovider/dns/azure/certificateinstall/azurewebapp")]
        [ResponseType(typeof(CertificateInstallModel))]
        public async Task<IHttpActionResult> GenerateAndInstall(DnsAzureInstallModel model, [FromUri(Name = "api-version")]string apiversion = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var mgr = CertificateManager.CreateAzureDnsWebAppCertificateManager(model.AzureEnvironment, model.AcmeConfig, model.CertificateSettings, model.AzureDnsEnvironment);

            return Ok(await mgr.AddCertificate());
        }
    }
}