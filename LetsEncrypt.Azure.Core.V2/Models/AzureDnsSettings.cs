using System;
using System.Collections.Generic;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class AzureDnsSettings
    {   
        public AzureDnsSettings()
        {
            this.RelativeRecordSetName = "@";
        }

        public AzureDnsSettings(string resourceGroupName, string zoneName, AzureServicePrincipal servicePrincipal, AzureSubscription azureSubscription, string relativeRecordName = "@")
        {
            this.AzureSubscription = azureSubscription;
            this.AzureServicePrincipal = servicePrincipal;
            this.ResourceGroupName = resourceGroupName;
            this.ZoneName = zoneName;
            this.RelativeRecordSetName = resourceGroupName;
        }

        public AzureServicePrincipal AzureServicePrincipal {get;set;}
        public AzureSubscription AzureSubscription { get; set; }
        
        public string ResourceGroupName { get;  set; }

        public string RelativeRecordSetName { get; set; }

        public string ZoneName { get; set; }

    }
}
