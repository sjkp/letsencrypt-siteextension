using ACMESharp;
using ACMESharp.ACME;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using LetsEncrypt.SiteExtension.Models;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        static AppSettingsAuthConfig settings = new AppSettingsAuthConfig();
        const string webConfig = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <system.webServer>
    <handlers>
      <clear />
      <add name=""ACMEStaticFile"" path=""*"" verb=""*"" modules=""StaticFileModule"" resourceType=""Either"" requireAccess=""Read"" />
    </handlers>
    <staticContent>
      <remove fileExtension=""."" />
      <mimeMap fileExtension=""."" mimeType=""text/plain"" />
    </staticContent>
  </system.webServer>
</configuration>";
        private static WebSiteManagementClient webSiteClient;

        /// <summary>
        /// Used for automatic installation of hostnames bindings and certificate 
        /// upon first installation on the site extension and if hostnames are specified in app settings
        /// </summary>
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
                        Trace.TraceInformation("Hostname already configured skipping installation");
                        continue;
                    }
                    Trace.TraceInformation("Setting up hostname and lets encrypt certificate for " + hostname);
                    client.Sites.CreateOrUpdateSiteOrSlotHostNameBinding(settings.ResourceGroupName, settings.WebAppName, settings.SiteSlotName, hostname, new HostNameBinding()
                    {
                        CustomHostNameDnsRecordType = CustomHostNameDnsRecordType.CName,
                        HostNameType = HostNameType.Verified,
                        SiteName = settings.WebAppName,
                        Location = s.Location
                    });                    
                }
                if (settings.Hostnames.Any())
                {
                    RequestAndInstallInternal(new Target()
                    {
                        BaseUri = settings.BaseUri,
                        ClientId = settings.ClientId,
                        ClientSecret = settings.ClientSecret,
                        Email = settings.Email,
                        Host = settings.Hostnames.First(),
                        ResourceGroupName = settings.ResourceGroupName,
                        SubscriptionId = settings.SubscriptionId,
                        Tenant = settings.Tenant,
                        WebAppName = settings.WebAppName,
                        ServicePlanResourceGroupName = settings.ServicePlanResourceGroupName,
                        AlternativeNames = settings.Hostnames.Skip(1).ToList(),
                        SiteSlotName = settings.SiteSlotName,
                        UseIPBasedSSL = settings.UseIPBasedSSL
                    });
                }
            }
        }

        public IEnumerable<Target> RenewCertificate(bool debug = false)
        {
            Trace.TraceInformation("Checking certificate");
            var settings = new AppSettingsAuthConfig();
            var ss = SettingsStore.Instance.Load();
            using (var client = ArmHelper.GetWebSiteManagementClient(settings))
            {
                var certs = client.Certificates.GetCertificates(settings.ServicePlanResourceGroupName).Value;
                var expiringCerts = certs.Where(s => s.ExpirationDate < DateTime.UtcNow.AddDays(settings.RenewXNumberOfDaysBeforeExpiration) && (s.Issuer.Contains("Let's Encrypt") || s.Issuer.Contains("Fake LE")));

                if (expiringCerts.Count() == 0)
                {
                    Trace.TraceInformation(string.Format("No certificates installed issued by Let's Encrypt that are about to expire within the next {0} days. Skipping.", settings.RenewXNumberOfDaysBeforeExpiration));
                }

                foreach (var toExpireCert in expiringCerts)
                {
                    Trace.TraceInformation("Starting renew of certificate " + toExpireCert.Name + " expiration date " + toExpireCert.ExpirationDate);
                    var site = client.Sites.GetSite(settings.ResourceGroupName, settings.WebAppName);
                    var sslStates = site.HostNameSslStates.Where(s => s.Thumbprint == toExpireCert.Thumbprint);
                    if (!sslStates.Any())
                    {
                        Trace.TraceInformation(String.Format("Certificate {0} was not assigned any hostname, skipping update", toExpireCert.Thumbprint));
                        continue;
                    }
                    var target = new Target()
                    {
                        WebAppName = settings.WebAppName,
                        Tenant = settings.Tenant,
                        SubscriptionId = settings.SubscriptionId,
                        ClientId = settings.ClientId,
                        ClientSecret = settings.ClientSecret,
                        ResourceGroupName = settings.ResourceGroupName,
                        Email = settings.Email ?? ss.FirstOrDefault(s => s.Name == "email").Value,
                        Host = sslStates.First().Name,
                        BaseUri = settings.BaseUri ?? ss.FirstOrDefault(s => s.Name == "baseUri").Value,
                        ServicePlanResourceGroupName = settings.ServicePlanResourceGroupName,
                        AlternativeNames = sslStates.Skip(1).Select(s => s.Name).ToList(),
                        UseIPBasedSSL = settings.UseIPBasedSSL,
                        SiteSlotName = settings.SiteSlotName,
                        DisableWebConfigUpdate = settings.DisableWebConfigUpdate
                    };
                    if (!debug)
                    {
                        RequestAndInstallInternal(target);
                    }
                    yield return target;
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
            return ConfigurationManager.AppSettings["letsencrypt:WebRootPath"] ?? Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), "site", "wwwroot");
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
        }

        public static string GetCertificate(Target binding)
        {

            var dnsIdentifier = binding.Host;
            var cp = CertificateProvider.GetProvider();
            var rsaPkp = new RsaPrivateKeyParams();
            try
            {
                if (settings.RSAKeyLength >= 1024)
                {
                    rsaPkp.NumBits = settings.RSAKeyLength;
                    Trace.TraceInformation("RSAKeyBits: {0}", settings.RSAKeyLength);
                }
                else
                {
                    Trace.TraceWarning("RSA Key Bits less than 1024 is not secure. Letting ACMESharp default key bits. http://openssl.org/docs/manmaster/crypto/RSA_generate_key_ex.html");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Unable to set RSA Key Bits, Letting ACMESharp default key bits, Error: {0}", ex);                
                Console.WriteLine($"Unable to set RSA Key Bits, Letting ACMESharp default key bits, Error: {ex.Message.ToString()}");
            }

            var rsaKeys = cp.GeneratePrivateKey(rsaPkp);
            var csrDetails = new CsrDetails
            {
                CommonName = dnsIdentifier,
            };
            if (binding.AlternativeNames != null)
            {
                if (binding.AlternativeNames.Count > 0)
                {
                    csrDetails.AlternativeNames = binding.AlternativeNames;
                }
            }
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

            Console.WriteLine($"\nRequesting Certificate");
            Trace.TraceInformation("Requesting Certificate");
            var certRequ = client.RequestCertificate(derB64u);

            Trace.TraceInformation("certRequ {0}", certRequ);

            Console.WriteLine($" Request Status: {certRequ.StatusCode}");
            Trace.TraceInformation("Request Status: {0}", certRequ.StatusCode);

            if (certRequ.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var keyGenFile = Path.Combine(configPath, $"{dnsIdentifier}-gen-key.json");
                var keyPemFile = Path.Combine(configPath, $"{dnsIdentifier}-key.pem");
                var csrGenFile = Path.Combine(configPath, $"{dnsIdentifier}-gen-csr.json");
                var csrPemFile = Path.Combine(configPath, $"{dnsIdentifier}-csr.pem");
                var crtDerFile = Path.Combine(configPath, $"{dnsIdentifier}-crt.der");
                var crtPemFile = Path.Combine(configPath, $"{dnsIdentifier}-crt.pem");
                string crtPfxFile = null;

                crtPfxFile = Path.Combine(configPath, $"{dnsIdentifier}-all.pfx");



                using (var fs = new FileStream(keyGenFile, FileMode.Create))
                    cp.SavePrivateKey(rsaKeys, fs);
                using (var fs = new FileStream(keyPemFile, FileMode.Create))
                    cp.ExportPrivateKey(rsaKeys, EncodingFormat.PEM, fs);
                using (var fs = new FileStream(csrGenFile, FileMode.Create))
                    cp.SaveCsr(csr, fs);
                using (var fs = new FileStream(csrPemFile, FileMode.Create))
                    cp.ExportCsr(csr, EncodingFormat.PEM, fs);

                Console.WriteLine($" Saving Certificate to {crtDerFile}");
                Trace.TraceInformation("Saving Certificate to {0}", crtDerFile);
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




                Console.WriteLine($" Saving Certificate to {crtPfxFile}");
                Trace.TraceInformation("Saving Certificate to {0}", crtPfxFile);
                using (FileStream source = new FileStream(isuPemFile, FileMode.Open),
                        target = new FileStream(crtPfxFile, FileMode.Create))
                {
                    try
                    {
                        var isuCrt = cp.ImportCertificate(EncodingFormat.PEM, source);
                        cp.ExportArchive(rsaKeys, new[] { crt, isuCrt }, ArchiveFormat.PKCS12, target, settings.PFXPassword);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error exporting archive {0}", ex);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error exporting archive: {ex.Message.ToString()}");
                        Console.ResetColor();
                    }
                }


                cp.Dispose();

                return crtPfxFile;
            }
            if ((int)certRequ.StatusCode == 429)
            {
                Trace.TraceError("Unable to request certificate, too many certificate requests to Let's Encrypt certificate servers for the domain within the last 7 days. Please try again later. (If you are testing, please use the staging enviroment where you can request unlimited number of certificates. During the beta period only 5 certificate requests per domain per week are allowed to the production environment.)");
                throw new Exception("Unable to request certificate, too many certificate requests to Let's Encrypt certificate servers for the domain within the last 7 days. Please try again later. (If you are testing, please use the staging enviroment where you can request unlimited number of certificates. During the beta period only 5 certificate requests per domain per week are allowed to the production environment.)");
            }

            Trace.TraceError("Request status = {0}", certRequ.StatusCode);
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

                        Console.WriteLine($" Saving Issuer Certificate to {cacertPemFile}");
                        Trace.TraceInformation("Saving Issuer Certificate to {0}", cacertPemFile);
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
                certificate = new X509Certificate2(pfxFilename, settings.PFXPassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                certificate.FriendlyName = $"{binding.Host} {DateTime.Now}";

                Install(binding, pfxFilename, certificate);

                return certificate.Thumbprint;
            }

            throw new Exception("Unable to complete challenge with Lets Encrypt servers error was: " + auth.Status);
        }

        public static void Install(Target target, string pfxFilename, X509Certificate2 certificate)
        {
            Console.WriteLine(String.Format("Installing certificate {0} on azure", pfxFilename));
            Trace.TraceInformation(String.Format("Installing certificate {0} on azure", pfxFilename));
            var bytes = File.ReadAllBytes(pfxFilename);
            var pfx = Convert.ToBase64String(bytes);

            var s = webSiteClient.Sites.GetSiteOrSlot(target.ResourceGroupName, target.WebAppName, target.SiteSlotName);
            webSiteClient.Certificates.CreateOrUpdateCertificate(target.ServicePlanResourceGroupName, certificate.Subject.Replace("CN=", ""), new Certificate()
            {
                PfxBlob = pfx,
                Password = settings.PFXPassword,
                Location = s.Location,
            });
            foreach (var dnsName in target.AllDnsIdentifiers)
            {
                var sslState = s.HostNameSslStates.FirstOrDefault(g => g.Name == dnsName);

                if (sslState == null)
                {
                    sslState = new HostNameSslState()
                    {
                        Name = target.Host,
                        SslState = target.UseIPBasedSSL ? SslState.IpBasedEnabled : SslState.SniEnabled,
                    };
                    s.HostNameSslStates.Add(sslState);
                }
                else
                {
                    //First time setting the HostNameSslState it is set to disabled.
                    sslState.SslState = target.UseIPBasedSSL ? SslState.IpBasedEnabled : SslState.SniEnabled;
                }
                sslState.ToUpdate = true;
                sslState.Thumbprint = certificate.Thumbprint;
            }
            webSiteClient.Sites.BeginCreateOrUpdateSiteOrSlot(target.ResourceGroupName, target.WebAppName, target.SiteSlotName, s);

        }

        public static AuthorizationState Authorize(Target target)
        {            
            List<AuthorizationState> authStatus = new List<AuthorizationState>();
            var webRootPath = WebRootPath();
            var directory = Path.Combine(webRootPath, ".well-known", "acme-challenge");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var webConfigPath = Path.Combine(directory, "web.config");
            if (target.DisableWebConfigUpdate == false && (!File.Exists(webConfigPath) || File.ReadAllText(webConfigPath) != webConfig))
            {
                Trace.TraceInformation($"Writing web.config to {webConfigPath}");
                File.WriteAllText(webConfigPath, webConfig);
            }

            foreach (var dnsIdentifier in target.AllDnsIdentifiers)
            {
                //var dnsIdentifier = target.Host;                
                Console.WriteLine($"\nAuthorizing Identifier {dnsIdentifier} Using Challenge Type {AcmeProtocol.CHALLENGE_TYPE_HTTP}");
                Trace.TraceInformation("Authorizing Identifier {0} Using Challenge Type {1}", dnsIdentifier, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                var authzState = client.AuthorizeIdentifier(dnsIdentifier);
                var challenge = client.DecodeChallenge(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                var httpChallenge = challenge.Challenge as HttpChallenge;

                // We need to strip off any leading '/' in the path
                var filePath = httpChallenge.FilePath;
                if (filePath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                    filePath = filePath.Substring(1);
                var answerPath = Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath, filePath));

                Console.WriteLine($" Writing challenge answer to {answerPath}");
                Trace.TraceInformation("Writing challenge answer to {0}", answerPath);
                
                File.WriteAllText(answerPath, httpChallenge.FileContent);                

                var answerUri = new Uri(httpChallenge.FileUrl);
                Console.WriteLine($" Answer should now be browsable at {answerUri}");
                Trace.TraceInformation("Answer should now be browsable at {0}", answerUri);

                try
                {
                    var retry = 10;
                    while (true)
                    {
                        using (var handler = new WebRequestHandler())
                        {
                            //Allow self-signed certs otherwise staging wont work
                            handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                            using (var client = new HttpClient(handler))
                            {
                                Thread.Sleep(1000);
                                var x = client.GetAsync(answerUri).Result;
                                Trace.TraceInformation("Checking status {0}", x.StatusCode);
                                if (x.StatusCode == HttpStatusCode.OK)
                                    break;
                                if (retry-- == 0)
                                    break;
                                Trace.TraceInformation("Retrying {0}", retry);
                            }
                        }
                    }
                    Console.WriteLine(" Submitting answer");
                    Trace.TraceInformation("Submitting answer");
                    authzState.Challenges = new AuthorizeChallenge[] { challenge };
                    client.SubmitChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP, true);

                    // have to loop to wait for server to stop being pending. 
                    retry = 0;
                    while (authzState.Status == "pending" && retry < 6)
                    {
                        retry++;
                        Console.WriteLine(" Refreshing authorization attempt " + retry);
                        Trace.TraceInformation("Refreshing authorization attempt " + retry);
                        Thread.Sleep(4000); // this has to be here to give ACME server a chance to think
                        var newAuthzState = client.RefreshIdentifierAuthorization(authzState);
                        if (newAuthzState.Status != "pending")
                            authzState = newAuthzState;
                    }

                    Console.WriteLine($" Authorization Result: {authzState.Status}");
                    Trace.TraceInformation("Auth Result {0}", authzState.Status);
                    if (authzState.Status == "invalid")
                    {
                        Trace.TraceError("Authorization Failed {0}", authzState.Status);
                        Trace.TraceInformation("Full Error Details {0}", JsonConvert.SerializeObject(authzState));                        
                        Console.WriteLine($"The ACME server was probably unable to reach {answerUri}");
                        Trace.TraceError("Unable to reach {0}", answerUri);
                        Console.WriteLine("\nCheck in a browser to see if the answer file is being served correctly.");
                        throw new Exception($"The Lets Encrypt ACME server was probably unable to reach {answerUri} view error report from Lets Encrypt at {authzState.Uri} for more information");
                    }
                    authStatus.Add(authzState);
                }
                finally
                {
                    if (authzState.Status == "valid")
                    {
                        Console.WriteLine(" Deleting answer");
                        Trace.TraceInformation("Deleting answer");
                        File.Delete(answerPath);
                    }
                }
            }
            foreach (var authState in authStatus)
            {
                if (authState.Status != "valid")
                {
                    return authState;
                }
            }
            return new AuthorizationState { Status = "valid" };
        }
    }
}
