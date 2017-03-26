using LetsEncrypt.SiteExtension.Models;
using Microsoft.Azure.Management.WebSites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.SiteExtension.Core
{
    public static class KuduHelper
    {
        public static KuduRestClient GetKuduClient(this WebSiteManagementClient client, IAuthSettings settings)
        {
            var user = client.Sites.GetPublsihingCredentialSiteOrSlot(settings.ResourceGroupName, settings.WebAppName, settings.SiteSlotName);

            return new KuduRestClient(settings.WebAppName, user.PublishingUserName, user.PublishingPassword);
        }
    }
}
