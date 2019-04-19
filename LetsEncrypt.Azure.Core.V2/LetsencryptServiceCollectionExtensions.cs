using LetsEncrypt.Azure.Core.V2.CertificateStores;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace LetsEncrypt.Azure.Core.V2
{
    public static class LetsencryptServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureBlobStorageCertificateStore(this IServiceCollection serviceCollection, string azureStorageConnectionString)
        {
            return serviceCollection
                    .AddTransient<IFileSystem, AzureBlobStorage>(s =>
                    {
                        return new AzureBlobStorage(azureStorageConnectionString);
                    })
                    .AddTransient<AzureBlobStorage, AzureBlobStorage>(s =>
                    {
                        return new AzureBlobStorage(azureStorageConnectionString);
                    })
                    .AddTransient<ICertificateStore, AzureBlobCertificateStore>();
        }

        public static IServiceCollection AddKeyVaultCertificateStore(this IServiceCollection serviceCollection, string vaultBaseUrl)
        {
            return serviceCollection
                .AddTransient<ICertificateStore>((serviceProvider) =>
            {
                return new AzureKeyVaultCertificateStore(serviceProvider.GetService<IKeyVaultClient>(), vaultBaseUrl);
            });
        }

        public static IServiceCollection AddFileSystemCertificateStore(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                    .AddTransient<IFileSystem, FileSystem>()
                    .AddTransient<ICertificateStore, FileSystemCertificateStore>();
        }

        public static IServiceCollection AddAcmeClient<TDnsProvider>(this IServiceCollection serviceCollection, object dnsProviderConfig) where TDnsProvider : class, IDnsProvider
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (dnsProviderConfig == null)
            {
                throw new ArgumentNullException(nameof(dnsProviderConfig));
            }

            if (!serviceCollection.Any(s => s.ServiceType == typeof(ICertificateStore)))
            {
                serviceCollection.AddTransient<ICertificateStore, NullCertificateStore>();
            }

            return serviceCollection
                .AddTransient<AcmeClient>()
                .AddTransient<DnsLookupService>()                
                .AddSingleton(dnsProviderConfig.GetType(), dnsProviderConfig)
                .AddTransient<IDnsProvider, TDnsProvider>();               
        }

        public static IServiceCollection AddAzureAppService(this IServiceCollection serviceCollection, params AzureWebAppSettings[] settings)
        {
            if (settings == null || settings.Length == 0)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            return serviceCollection
                .AddSingleton(settings)
                .AddTransient<AzureWebAppService>()
                .AddTransient<LetsencryptService>();
        }
    }
}
