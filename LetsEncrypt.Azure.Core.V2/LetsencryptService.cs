using LetsEncrypt.Azure.Core.V2.CertificateStores;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LetsEncrypt.Azure.Core.V2
{
    public static class LetsencryptService
    {
        public static IServiceCollection AddAcmeClient<TDnsProvider>(this IServiceCollection serviceCollection, object dnsProviderConfig, string azureStorageConnectionString = null) where TDnsProvider : class, IDnsProvider
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (dnsProviderConfig == null)
            {
                throw new ArgumentNullException(nameof(dnsProviderConfig));
            }
            if (string.IsNullOrEmpty(azureStorageConnectionString))
            {
                serviceCollection
                    .AddTransient<IFileSystem, FileSystem>()
                    .AddTransient<ICertificateStore, FileSystemCertificateStore>();
            }
            else
            {
                serviceCollection
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
                .AddTransient<AzureWebAppService>();
        }
    }
}
