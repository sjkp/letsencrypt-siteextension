using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.Management.WebSites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core
{
    public static class KuduHelper
    {
        public static KuduRestClient GetKuduClient(this WebSiteManagementClient client, IAzureWebAppEnvironment settings)
        {
            var user = client.WebApps.GetPublsihingCredentialSiteOrSlot(settings.ResourceGroupName, settings.WebAppName, settings.SiteSlotName);

            return new KuduRestClient(settings, user.PublishingUserName, user.PublishingPassword);
        }
    }
}
