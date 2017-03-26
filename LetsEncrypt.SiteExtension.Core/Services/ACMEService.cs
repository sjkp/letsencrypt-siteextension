using ACMESharp;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using LetsEncrypt.SiteExtension.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace LetsEncrypt.SiteExtension.Core.Services
{

    public class AcmeService
    {
        private readonly string configPath;
        private string baseURI;
        private readonly AcmeConfig config;
        private readonly IAuthorizeChallengeProvider authorizeChallengeProvider;

        public AcmeService(AcmeConfig config, IAuthorizeChallengeProvider authorizeChallengeProvider)
        {
            this.baseURI = config.Endpoint ?? "https://acme-staging.api.letsencrypt.org/";
            this.configPath = ConfigPath(this.baseURI);
            this.config = config;
            this.authorizeChallengeProvider = authorizeChallengeProvider;
        }

        public async Task<CertificateInfo> RequestCertificate()
        {   using (var signer = new RS256Signer())
            using (var client = Register(signer))
            {
                var auth = await this.authorizeChallengeProvider.Authorize(client, config.AllDnsIdentifiers);

                //GetCertificate
                if (auth.Status == "valid")
                {
                    var pfxFilename = GetCertificate(client);

                    X509Certificate2 certificate;
                    certificate = new X509Certificate2(pfxFilename, config.PFXPassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                    certificate.FriendlyName = $"{config.Host} {DateTime.Now}";

                    var bytes = File.ReadAllBytes(pfxFilename);
                    var pfx = Convert.ToBase64String(bytes);

                    return new CertificateInfo()
                    {
                        Certificate = certificate,
                        PfxCertificateBase64 = pfx,
                        Name = pfxFilename
                    };
                }

                throw new Exception("Unable to complete challenge with Lets Encrypt servers error was: " + auth.Status);
            }
        }

        public AcmeClient Register(RS256Signer signer)
        {
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            var email = config.RegistrationEmail;
            try
            {

                signer.Init();

                var signerPath = Path.Combine(configPath, "Signer");
                if (File.Exists(signerPath))
                {
                    Trace.TraceInformation($"Loading Signer from {signerPath}");
                    using (var signerStream = File.OpenRead(signerPath))
                        signer.Load(signerStream);
                }

                var client = new AcmeClient(new Uri(this.baseURI), new AcmeServerDirectory(), signer);

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
                return client;


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


        public string GetCertificate(AcmeClient client)
        {

            var dnsIdentifier = config.Host;
            var cp = CertificateProvider.GetProvider();
            var rsaPkp = new RsaPrivateKeyParams();
            try
            {
                if (config.RSAKeyLength >= 1024)
                {
                    rsaPkp.NumBits = config.RSAKeyLength;
                    Trace.TraceInformation("RSAKeyBits: {0}", config.RSAKeyLength);
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
            if (config.AlternativeNames != null)
            {
                if (config.AlternativeNames.Count > 0)
                {
                    csrDetails.AlternativeNames = config.AlternativeNames;
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
                var keyGenFile = Path.Combine(this.configPath, $"{dnsIdentifier}-gen-key.json");
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
                        cp.ExportArchive(rsaKeys, new[] { crt, isuCrt }, ArchiveFormat.PKCS12, target, config.PFXPassword);
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

        public string GetIssuerCertificate(CertificateRequest certificate, CertificateProvider cp)
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

                            var uri = new Uri(new Uri(this.baseURI), upLink.Uri);
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
