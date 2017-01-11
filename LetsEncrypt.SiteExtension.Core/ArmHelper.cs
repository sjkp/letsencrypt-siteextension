using LetsEncrypt.SiteExtension.Models;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.WebSites;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LetsEncrypt.SiteExtension
{
    public static class ArmHelper
    {
        public static WebSiteManagementClient GetWebSiteManagementClient(IAuthSettings model)
        {
            var settings = new AppSettingsAuthConfig();
            var authContext = new AuthenticationContext(settings.AuthenticationEndpoint + model.Tenant);

            var token = authContext.AcquireToken(settings.TokenAudience.ToString(), new ClientCredential(model.ClientId.ToString(), model.ClientSecret));
            var creds = new TokenCredentials(token.AccessToken);

            var websiteClient = new WebSiteManagementClient(settings.ManagementEndpoint, creds);
            websiteClient.SubscriptionId = model.SubscriptionId.ToString();
            return websiteClient;
        }
    }
}