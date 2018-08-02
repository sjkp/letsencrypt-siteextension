using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Collections.Generic;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2
{
    public class AzureHelper
    {
        public static AzureCredentials GetAzureCredentials(AzureServicePrincipal servicePrincipal, AzureSubscription azureSubscription)
        {
            if (servicePrincipal == null)
            {
                throw new ArgumentNullException(nameof(servicePrincipal));
            }

            if (azureSubscription == null)
            {
                throw new ArgumentNullException(nameof(azureSubscription));
            }

            return new AzureCredentials(servicePrincipal.ServicePrincipalLoginInformation,
               azureSubscription.Tenant, Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.FromName(azureSubscription.AzureRegion));
        }
    }
}
