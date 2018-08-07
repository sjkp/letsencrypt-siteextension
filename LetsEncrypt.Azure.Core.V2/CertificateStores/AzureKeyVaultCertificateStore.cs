using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace LetsEncrypt.Azure.Core.V2.CertificateStores
{
    /// <summary>An azure key vault certificate store.</summary>
    /// <seealso cref="T:LetsEncrypt.Azure.Core.V2.CertificateStores.ICertificateStore"/>
    public class AzureKeyVaultCertificateStore : ICertificateStore
    {
        /// <summary>The key vault client.</summary>
        private readonly IKeyVaultClient keyVaultClient;

        /// <summary>The base URL for the key vault, typically https://{keyvaultname}.vault.azure.net</summary>
        public string vaultBaseUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultCertificateStore"/> class.
        /// </summary>
        /// <param name="keyVaultClient">The key vault client.</param>
        /// <param name="vaultBaseUrl">
        /// The base URL for the key vault, typically https://{keyvaultname}.vault.azure.net.
        /// </param>
        public AzureKeyVaultCertificateStore(IKeyVaultClient keyVaultClient, string vaultBaseUrl)
        {
            this.keyVaultClient = keyVaultClient;
            this.vaultBaseUrl = vaultBaseUrl;
        }

        public async Task<CertificateInfo> GetCertificate(string name, string password)
        {
            // This retrieves the secret/certificate with the private key
            var secret = await this.keyVaultClient.GetSecretAsync(this.vaultBaseUrl, name);
            X509Certificate2 certificate = new X509Certificate2(Convert.FromBase64String(secret.Value), password);

            // This retrieves the secret/certificate without the private key
            var certBundle = await this.keyVaultClient.GetCertificateAsync(this.vaultBaseUrl, name);
            var cert = new X509Certificate2(certBundle.Cer, password);

            return new CertificateInfo()
            {
                Certificate = certificate,
                Name = name,
                Password = password,
                PfxCertificate = certBundle.Cer,
            };
        }

        /// <summary>Saves a certificate.</summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>An asynchronous result.</returns>
        public Task SaveCertificate(CertificateInfo certificate)
        {
            return this.keyVaultClient.ImportCertificateAsync(this.vaultBaseUrl, certificate.Name, certificate.PfxCertificate.ToString(), certificate.Password);
        }
    }
}
