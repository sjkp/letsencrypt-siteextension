using ACMESharp;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using LetsEncrypt.SiteExtension.Models;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core
{
    public class CertificateManager
    {
        static AcmeClient client;
        static string configPath = "";
        private static string BaseURI;
        const string webConfig = @"<?xml version = ""1.0"" encoding=""UTF-8""?>
 <configuration>
     <system.webServer>
         <staticContent>
             <mimeMap fileExtension = ""."" mimeType=""text/json"" />
         </staticContent>
     </system.webServer>
 </configuration>";
        private static WebSiteManagementClient webSiteClient;
        private static WebSiteManagementClient serverFarmClient;

        public void SetupHostnameAndCertificate()
        {
            Trace.TraceInformation("Setup hostname and certificates");
            var settings = new AppSettingsAuthConfig();
            using (var client = ArmHelper.GetWebSiteManagementClient(settings))
            {
                var s = client.Sites.GetSite(settings.ResourceGroupName, settings.WebAppName);
                foreach (var hostname in settings.Hostnames)
                {
                    if (s.HostNames.Any(existingHostname => string.Equals(existingHostname, hostname, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }
                    Trace.TraceInformation("Setting up hostname and lets encrypt certificate for " + hostname);
                    client.Sites.CreateOrUpdateSiteHostNameBinding(settings.ResourceGroupName, settings.WebAppName, hostname, new Microsoft.Azure.Management.WebSites.Models.HostNameBinding()
                    {
                        CustomHostNameDnsRecordType = CustomHostNameDnsRecordType.CName,
                        HostNameType = HostNameType.Verified,
                        SiteName = settings.WebAppName,
                        Location = s.Location
                    });

                    RequestAndInstallInternal(new Target()
                    {
                        BaseUri = settings.BaseUri,
                        ClientId = settings.ClientId,
                        ClientSecret = settings.ClientSecret,
                        Email = settings.Email,
                        Host = hostname,
                        ResourceGroupName = settings.ResourceGroupName,
                        SubscriptionId = settings.SubscriptionId,
                        Tenant = settings.Tenant,
                        WebAppName = settings.WebAppName,
                        ServicePlanResourceGroupName = settings.ServicePlanResourceGroupName,
                    });
                }
            }
        }

        public void RenewCertificate()
        {
            Trace.TraceInformation("Checking certificate");
            var settings = new AppSettingsAuthConfig();
            using (var client = ArmHelper.GetWebSiteManagementClient(settings))
            {
                var certs = client.Certificates.GetCertificates(settings.ResourceGroupName).Value;
                var expireringIn14Days = certs.Where(s => s.ExpirationDate < DateTime.UtcNow.AddDays(14) && s.Issuer.Contains("Let's Encrypt"));


                foreach (var toExpireCert in expireringIn14Days)
                {
                    Trace.TraceInformation("Starting renew of certificate " + toExpireCert.Name + " expiration date " + toExpireCert.ExpirationDate);
                    var site = client.Sites.GetSite(settings.ResourceGroupName, settings.WebAppName);
                    var sslState = site.HostNameSslStates.FirstOrDefault(s => s.Thumbprint == toExpireCert.Thumbprint);
                    if (sslState == null)
                    {
                        Trace.TraceInformation(String.Format("Certificate {0} was not assigned any hostname, skipping update", toExpireCert.Thumbprint));
                        continue;
                    }
                    var ss = SettingsStore.Instance.Load();
                    RequestAndInstallInternal(new Target()
                    {
                        WebAppName = settings.WebAppName,
                        Tenant = settings.Tenant,
                        SubscriptionId = settings.SubscriptionId,
                        ClientId = settings.ClientId,
                        ClientSecret = settings.ClientSecret,
                        ResourceGroupName = settings.ResourceGroupName,
                        Email = settings.Email ?? ss.FirstOrDefault(s => s.Name == "email").Value,
                        Host = sslState.Name,
                        BaseUri = settings.BaseUri ?? ss.FirstOrDefault(s => s.Name == "baseUri").Value,
                        ServicePlanResourceGroupName = settings.ServicePlanResourceGroupName,
                    });
                }
            }
        }

        private static string ConfigPath(string baseUri)
        {
            return Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), "siteextensions", "letsencrypt", "config", CleanFileName(baseUri));
        }

        static string CleanFileName(string fileName) => Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));

        private static string WebRootPath()
        {
            return Path.Combine(ConfigurationManager.AppSettings["letsencrypt:WebRootPath"] ?? Environment.ExpandEnvironmentVariables("%HOME%"), "site", "wwwroot");
        }

        public static string RequestAndInstallInternal(Target target)
        {
            BaseURI = target.BaseUri ?? "https://acme-staging.api.letsencrypt.org/";
            configPath = ConfigPath(BaseURI);
            try
            {
                webSiteClient = ArmHelper.GetWebSiteManagementClient(target);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unabled to create Azure Web Site Management client " + ex.ToString());
                throw;
            }

            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            var email = target.Email;
            try
            {
                using (var signer = new RS256Signer())
                {
                    signer.Init();

                    var signerPath = Path.Combine(configPath, "Signer");
                    if (File.Exists(signerPath))
                    {
                        Trace.TraceInformation($"Loading Signer from {signerPath}");
                        using (var signerStream = File.OpenRead(signerPath))
                            signer.Load(signerStream);
                    }

                    using (client = new AcmeClient(new Uri(BaseURI), new AcmeServerDirectory(), signer))
                    {
                        client.Init();
                        Trace.TraceInformation("\nGetting AcmeServerDirectory");
                        client.GetDirectory(true);

                        var registrationPath = Path.Combine(configPath, "Registration");
                        if (File.Exists(registrationPath))
                        {
                            Trace.TraceInformation($"Loading Registration from {registrationPath}");
                            using (var registrationStream = File.OpenRead(registrationPath))
                                client.Registration = AcmeRegistration.Load(registrationStream);
                        }
                        else
                        {


                            var contacts = new string[] { };
                            if (!String.IsNullOrEmpty(email))
                            {
                                email = "mailto:" + email;
                                contacts = new string[] { email };
                            }

                            Trace.TraceInformation("Calling Register");
                            var registration = client.Register(contacts);


                            Trace.TraceInformation($"Do you agree to {registration.TosLinkUri}? (Y/N) ");


                            Trace.TraceInformation("Updating Registration");
                            client.UpdateRegistration(true, true);

                            Trace.TraceInformation("Saving Registration");
                            using (var registrationStream = File.OpenWrite(registrationPath))
                                client.Registration.Save(registrationStream);

                            Trace.TraceInformation("Saving Signer");
                            using (var signerStream = File.OpenWrite(signerPath))
                                signer.Save(signerStream);
                        }

                        //                        if (Options.Renew)
                        //                        {
                        //                            CheckRenewals();
                        //#if DEBUG
                        //                            Trace.TraceInformation("Press enter to continue.");
                        //                            Trace.ReadLine();
                        //#endif
                        //                            return;
                        //                        }
                        return Auto(target);
                    }
                }
            }
            catch (Exception e)
            {
                var acmeWebException = e as AcmeClient.AcmeWebException;
                if (acmeWebException != null)
                {
                    Trace.TraceError(acmeWebException.Message);
                    Trace.TraceError("ACME Server Returned:");
                    Trace.TraceError(acmeWebException.Response.ContentAsString);
                }
                else
                {
                    Trace.TraceError(e.ToString());
                }
                throw;
            }
            return null;
        }

        public static string GetCertificate(Target binding)
        {
            var dnsIdentifier = binding.Host;

            var cp = CertificateProvider.GetProvider();
            var rsaPkp = new RsaPrivateKeyParams();

            var rsaKeys = cp.GeneratePrivateKey(rsaPkp);
            var csrDetails = new CsrDetails
            {
                CommonName = dnsIdentifier,
            };
            var csrParams = new CsrParams
            {
                Details = csrDetails,
            };
            var csr = cp.GenerateCsr(csrParams, rsaKeys, Crt.MessageDigest.SHA256);

            byte[] derRaw;
            using (var bs = new MemoryStream())
            {
                cp.ExportCsr(csr, EncodingFormat.DER, bs);
                derRaw = bs.ToArray();
            }
            var derB64u = JwsHelper.Base64UrlEncode(derRaw);

            Trace.TraceInformation($"\nRequesting Certificate");
            var certRequ = client.RequestCertificate(derB64u);

            Trace.TraceInformation($" Request Status: {certRequ.StatusCode}");

            //Trace.TraceInformation($"Refreshing Cert Request");
            //client.RefreshCertificateRequest(certRequ);

            if (certRequ.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var keyGenFile = Path.Combine(configPath, $"{dnsIdentifier}-gen-key.json");
                var keyPemFile = Path.Combine(configPath, $"{dnsIdentifier}-key.pem");
                var csrGenFile = Path.Combine(configPath, $"{dnsIdentifier}-gen-csr.json");
                var csrPemFile = Path.Combine(configPath, $"{dnsIdentifier}-csr.pem");
                var crtDerFile = Path.Combine(configPath, $"{dnsIdentifier}-crt.der");
                var crtPemFile = Path.Combine(configPath, $"{dnsIdentifier}-crt.pem");
                var crtPfxFile = Path.Combine(configPath, $"{dnsIdentifier}-all.pfx");

                using (var fs = new FileStream(keyGenFile, FileMode.Create))
                    cp.SavePrivateKey(rsaKeys, fs);
                using (var fs = new FileStream(keyPemFile, FileMode.Create))
                    cp.ExportPrivateKey(rsaKeys, EncodingFormat.PEM, fs);
                using (var fs = new FileStream(csrGenFile, FileMode.Create))
                    cp.SaveCsr(csr, fs);
                using (var fs = new FileStream(csrPemFile, FileMode.Create))
                    cp.ExportCsr(csr, EncodingFormat.PEM, fs);

                Trace.TraceInformation($" Saving Certificate to {crtDerFile}");
                using (var file = File.Create(crtDerFile))
                    certRequ.SaveCertificate(file);

                Crt crt;
                using (FileStream source = new FileStream(crtDerFile, FileMode.Open),
                        target = new FileStream(crtPemFile, FileMode.Create))
                {
                    crt = cp.ImportCertificate(EncodingFormat.DER, source);
                    cp.ExportCertificate(crt, EncodingFormat.PEM, target);
                }

                // To generate a PKCS#12 (.PFX) file, we need the issuer's public certificate
                var isuPemFile = GetIssuerCertificate(certRequ, cp);

                Trace.TraceInformation($" Saving Certificate to {crtPfxFile} (with no password set)");
                using (FileStream source = new FileStream(isuPemFile, FileMode.Open),
                        target = new FileStream(crtPfxFile, FileMode.Create))
                {
                    var isuCrt = cp.ImportCertificate(EncodingFormat.PEM, source);
                    cp.ExportArchive(rsaKeys, new[] { crt, isuCrt }, ArchiveFormat.PKCS12, target);
                }

                cp.Dispose();

                return crtPfxFile;
            }
            if ((int)certRequ.StatusCode == 429)
            {
                throw new Exception("Unable to request certificate, too many certificate requests to Let's Encrypt certificate servers for the domain within the last 7 days. Please try again later. (If you are testing, please use the staging enviroment where you can request unlimited number of certificates. During the beta period only 5 certificate requests per domain per week are allowed to the production environment.)"); 
            }

            throw new Exception($"Request status = {certRequ.StatusCode}");
        }

        public static string GetIssuerCertificate(CertificateRequest certificate, CertificateProvider cp)
        {
            var linksEnum = certificate.Links;
            if (linksEnum != null)
            {
                var links = new LinkCollection(linksEnum);
                var upLink = links.GetFirstOrDefault("up");
                if (upLink != null)
                {
                    var tmp = Path.GetTempFileName();
                    try
                    {
                        using (var web = new WebClient())
                        {
                            //if (v.Proxy != null)
                            //    web.Proxy = v.Proxy.GetWebProxy();

                            var uri = new Uri(new Uri(BaseURI), upLink.Uri);
                            web.DownloadFile(uri, tmp);
                        }

                        var cacert = new X509Certificate2(tmp);
                        var sernum = cacert.GetSerialNumberString();
                        var tprint = cacert.Thumbprint;
                        var sigalg = cacert.SignatureAlgorithm?.FriendlyName;
                        var sigval = cacert.GetCertHashString();

                        var cacertDerFile = Path.Combine(configPath, $"ca-{sernum}-crt.der");
                        var cacertPemFile = Path.Combine(configPath, $"ca-{sernum}-crt.pem");

                        if (!File.Exists(cacertDerFile))
                            File.Copy(tmp, cacertDerFile, true);

                        Trace.TraceInformation($" Saving Issuer Certificate to {cacertPemFile}");
                        if (!File.Exists(cacertPemFile))
                            using (FileStream source = new FileStream(cacertDerFile, FileMode.Open),
                                    target = new FileStream(cacertPemFile, FileMode.Create))
                            {
                                var caCrt = cp.ImportCertificate(EncodingFormat.DER, source);
                                cp.ExportCertificate(caCrt, EncodingFormat.PEM, target);
                            }

                        return cacertPemFile;
                    }
                    finally
                    {
                        if (File.Exists(tmp))
                            File.Delete(tmp);
                    }
                }
            }

            return null;
        }

        public static string Auto(Target binding)
        {
            var auth = Authorize(binding);
            if (auth.Status == "valid")
            {
                var pfxFilename = GetCertificate(binding);

                X509Certificate2 certificate;
                certificate = new X509Certificate2(pfxFilename, "", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                certificate.FriendlyName = $"{binding.Host} {DateTime.Now}";

                Install(binding, pfxFilename, certificate);

                return certificate.Thumbprint;
            }

            return null;
        }

        public static void Install(Target target, string pfxFilename, X509Certificate2 certificate)
        {
            Console.WriteLine(String.Format("Installing certificate {0} on azure", pfxFilename));
            var bytes = File.ReadAllBytes(pfxFilename);
            var pfx = Convert.ToBase64String(bytes);

            var s = webSiteClient.Sites.GetSite(target.ResourceGroupName, target.WebAppName);
            webSiteClient.Certificates.CreateOrUpdateCertificate(target.ServicePlanResourceGroupName, certificate.Subject.Replace("CN=", ""), new Certificate()
            {
                PfxBlob = pfx,
                Password = "",
                Location = s.Location,
            });
            var sslState = s.HostNameSslStates.FirstOrDefault(g => g.Name == target.Host);

            if (sslState == null)
            {
                sslState = new HostNameSslState()
                {
                    Name = target.Host,
                    SslState = SslState.SniEnabled,
                };
                s.HostNameSslStates.Add(sslState);
            }
            else
            {
                //First time setting the HostNameSslState it is set to disabled.
                sslState.SslState = SslState.SniEnabled;
            }
            sslState.ToUpdate = true;
            sslState.Thumbprint = certificate.Thumbprint;
            webSiteClient.Sites.BeginCreateOrUpdateSite(target.ResourceGroupName, target.WebAppName, s);

        }

        public static AuthorizationState Authorize(Target target)
        {
            var dnsIdentifier = target.Host;
            var webRootPath = WebRootPath();

            Trace.TraceInformation($"\nAuthorizing Identifier {dnsIdentifier} Using Challenge Type {AcmeProtocol.CHALLENGE_TYPE_HTTP}");
            var authzState = client.AuthorizeIdentifier(dnsIdentifier);
            
            var challenge = client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);
            var answerPath = Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath, challenge.OldChallengeAnswer.Key));

            Trace.TraceInformation($" Writing challenge answer to {answerPath}");
            var directory = Path.GetDirectoryName(answerPath);

            Directory.CreateDirectory(directory);
            File.WriteAllText(answerPath, challenge.OldChallengeAnswer.Value);
            File.WriteAllText(Path.Combine(directory, "web.config"), webConfig);


            var answerUri = new Uri(new Uri("http://" + dnsIdentifier), challenge.OldChallengeAnswer.Key);
            Trace.TraceInformation($" Answer should now be browsable at {answerUri}");

            try
            {
                Trace.TraceInformation(" Submitting answer");
                authzState.Challenges = new AuthorizeChallenge[] { challenge };
                client.SubmitAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP, true);

                // have to loop to wait for server to stop being pending.
                // TODO: put timeout/retry limit in this loop
                while (authzState.Status == "pending")
                {
                    Trace.TraceInformation(" Refreshing authorization");
                    Thread.Sleep(1000); // this has to be here to give ACME server a chance to think
                    var newAuthzState = client.RefreshIdentifierAuthorization(authzState);
                    if (newAuthzState.Status != "pending")
                        authzState = newAuthzState;
                }

                Trace.TraceInformation($" Authorization Result: {authzState.Status}");
                if (authzState.Status == "invalid")
                {
                    Trace.TraceError($"The ACME server was probably unable to reach {answerUri}");

                    Trace.TraceError("\nCheck in a browser to see if the answer file is being served correctly.");
                }

                return authzState;
            }
            finally
            {
                if (authzState.Status == "valid")
                {
                    Trace.TraceInformation(" Deleting answer");
                    File.Delete(answerPath);
                }
            }
        }
    }
}
