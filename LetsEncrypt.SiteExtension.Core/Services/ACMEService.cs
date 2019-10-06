using Certes;
using Certes.Acme;
using LetsEncrypt.Azure.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace LetsEncrypt.Azure.Core.Services
{
    public class AcmeService
    {
        private readonly Uri acmeenvironment;
        private readonly string configPath;
        private readonly IAcmeConfig config;
        private readonly IAuthorizationChallengeProvider authorizeChallengeProvider;

        public AcmeService(IAcmeConfig config, IAuthorizationChallengeProvider authorizeChallengeProvider)
        {
            if (string.IsNullOrEmpty(config.BaseUri))
            {
                this.acmeenvironment = (config.UseProduction ? WellKnownServers.LetsEncryptV2 : WellKnownServers.LetsEncryptStagingV2);
            }
            else
            {
                this.acmeenvironment = new Uri(config.BaseUri);
            }

            if (string.Equals(this.acmeenvironment.Host, "acme-v01.api.letsencrypt.org", StringComparison.InvariantCultureIgnoreCase) 
                || string.Equals(this.acmeenvironment.Host, "acme-staging.api.letsencrypt.org", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"Please use let's encrypt version 2 API, baseUri was set to {this.acmeenvironment}");
            }


            this.configPath = ConfigPath(this.acmeenvironment.AbsoluteUri);
            this.config = config;
            this.authorizeChallengeProvider = authorizeChallengeProvider;
        }

        public async Task<CertificateInfo> RequestCertificate()
        {
            var context = await GetOrCreateAcmeContext(this.acmeenvironment, config.RegistrationEmail);

            IdnMapping idn = new IdnMapping();
            var domains = config.Hostnames.Select(s => idn.GetAscii(s)).ToList();
            var order = await context.NewOrder(domains);
            
            var response = await authorizeChallengeProvider.Authorize(order, domains);

            if (response == "valid")
            {

                var privateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
                var cert = await order.Generate(new Certes.CsrInfo
                {
                    CountryName = "",
                    State = "",
                    Locality = "",
                    Organization = "",
                    OrganizationUnit = "",
                    CommonName = config.Host,
                }, privateKey);

                var certPem = cert.ToPem();

                var pfxBuilder = cert.ToPfx(privateKey);
                var pfx = pfxBuilder.Build(config.Host, config.PFXPassword);


                return new CertificateInfo()
                    {
                        Certificate = new X509Certificate2(pfx, config.PFXPassword, X509KeyStorageFlags.DefaultKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable),
                        Name = $"{config.Host} {DateTime.Now}",
                        Password = config.PFXPassword,
                        PfxCertificate = pfx
                    };

            }
            throw new Exception("Unable to complete challenge with Lets Encrypt servers error was: " + response);
        }

        private async Task<AcmeContext> GetOrCreateAcmeContext(Uri acmeDirectoryUri, string email)
        {
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }

            AcmeContext acme = null;
            string filename = $"account{email}--{acmeDirectoryUri.Host}";
            var filePath = Path.Combine(configPath, filename);
            if (!File.Exists(filePath) || string.IsNullOrEmpty(File.ReadAllText(filePath)))             
            {
                
                acme = new AcmeContext(acmeDirectoryUri);
                var account = acme.NewAccount(email, true);

                // Save the account key for later use
                var pemKey = acme.AccountKey.ToPem();
                File.WriteAllText(filePath, pemKey);
                await Task.Delay(10000); //Wait a little before using the new account.
                acme = new AcmeContext(acmeDirectoryUri, acme.AccountKey, new AcmeHttpClient(acmeDirectoryUri, new HttpClient()));
            }
            else 
            {
                var secret = File.ReadAllText(filePath);
                var accountKey = KeyFactory.FromPem(secret);
                acme = new AcmeContext(acmeDirectoryUri, accountKey, new AcmeHttpClient(acmeDirectoryUri, new HttpClient()));
            }

            return acme;
        }

        static string CleanFileName(string fileName) => Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));

        private static string ConfigPath(string baseUri)
        {
            if (Util.IsAzure)
            {
                return Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), "siteextensions", "letsencrypt", "config", CleanFileName(baseUri));
            }
            else
            {
                var folder = HostingEnvironment.MapPath("~/App_Data") ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data");

                return Path.Combine(folder, "siteextensions", "letsencrypt", "config", CleanFileName(baseUri));
            }
        }
    }
}
