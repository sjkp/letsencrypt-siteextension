using ARMExplorer.Controllers;
using ARMExplorer.Modules;
using LetsEncrypt.SiteExtension;
using LetsEncrypt.SiteExtension.Core;
using LetsEncrypt.SiteExtension.Models;
using Microsoft.Azure.Graph.RBAC;
using Microsoft.Azure.Graph.RBAC.Models;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.WebSites;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LetsEncrypt.SiteExtension.Controllers
{
    public class HomeController : Controller
    {
        // GET: Authentication
        public ActionResult Index()
        {
            var model = (AuthenticationModel)new AppSettingsAuthConfig();
            return View(model);
        }

        [HttpPost]
        public ActionResult Index(AuthenticationModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var client = ArmHelper.GetWebSiteManagementClient(model))
                    {
                        //Update web config.
                        var site = client.Sites.GetSite(model.ResourceGroupName, model.WebAppName);
                        var webappsettings = client.Sites.ListSiteAppSettings(model.ResourceGroupName, model.WebAppName);
                        if (model.UpdateAppSettings)
                        {
                            var newAppSettingsValues = new Dictionary<string, string>{
                                { AppSettingsAuthConfig.clientIdKey, model.ClientId.ToString() },
                                { AppSettingsAuthConfig.clientSecretKey, model.ClientSecret.ToString()},
                                { AppSettingsAuthConfig.subscriptionIdKey, model.SubscriptionId.ToString() },
                                { AppSettingsAuthConfig.tenantKey, model.Tenant },
                                { AppSettingsAuthConfig.resourceGroupNameKey, model.ResourceGroupName },
                                { AppSettingsAuthConfig.servicePlanResourceGroupNameKey, model.ServicePlanResourceGroupName },
                                { AppSettingsAuthConfig.useIPBasedSSL, model.UseIPBasedSSL.ToString().ToLowerInvariant() }
                            };
                            foreach (var appsetting in newAppSettingsValues)
                            {
                                if (!webappsettings.Properties.ContainsKey(appsetting.Key))
                                {
                                    webappsettings.Properties.Add(appsetting.Key, appsetting.Value);
                                }
                                else
                                {
                                    webappsettings.Properties[appsetting.Key] = appsetting.Value;
                                }
                            }
                            client.Sites.UpdateSiteAppSettings(model.ResourceGroupName, model.WebAppName, webappsettings);
                                
                               
                        } else { 
                            var appSetting = new AppSettingsAuthConfig();
                            if (!ValidateModelVsAppSettings("ClientId", model.ClientId.ToString(), appSetting.ClientId.ToString()) ||
                            !ValidateModelVsAppSettings("ClientSecret", appSetting.ClientSecret, model.ClientSecret) ||
                            !ValidateModelVsAppSettings("ResourceGroupName", appSetting.ResourceGroupName, model.ResourceGroupName) ||
                            !ValidateModelVsAppSettings("SubScriptionId", appSetting.SubscriptionId.ToString(), model.SubscriptionId.ToString()) ||
                            !ValidateModelVsAppSettings("Tenant", appSetting.Tenant, model.Tenant) ||
                            !ValidateModelVsAppSettings("ServicePlanResourceGroupName", appSetting.ServicePlanResourceGroupName, model.ServicePlanResourceGroupName) ||
                            !ValidateModelVsAppSettings("UseIPBasedSSL", appSetting.UseIPBasedSSL.ToString().ToLowerInvariant(), model.UseIPBasedSSL.ToString().ToLowerInvariant()))
                            {
                                model.ErrorMessage = "One or more app settings are different from the values entered, do you want to update the app settings?";
                                return View(model);
                            }
                        }
                        

                    }
                    return RedirectToAction("Hostname");
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                    model.ErrorMessage = ex.ToString();
                }

            }

            return View(model);
        }

        private bool ValidateModelVsAppSettings(string name, string appSettingValue, string modelValue)
        {
            if (appSettingValue != modelValue)
            {
                ModelState.AddModelError(name, string.Format("The {0} registered under application settings {1} does not match the {0} you entered here {2}", name, appSettingValue, modelValue));
                return false;
            }
            return true;
        }

        public ActionResult Hostname(string id)
        {
            var settings = new AppSettingsAuthConfig();
            var client = ArmHelper.GetWebSiteManagementClient(settings);

            var site = client.Sites.GetSite(settings.ResourceGroupName, settings.WebAppName);
            var model = new HostnameModel();
            model.HostNames = site.HostNames;
            model.HostNameSslStates = site.HostNameSslStates;
            model.Certificates = client.Certificates.GetCertificates(settings.ServicePlanResourceGroupName).Value;
            model.InstalledCertificateThumbprint = id;
            if (model.HostNames.Count == 1)
            {
                model.ErrorMessage = "No custom host names registered. At least one custom domain name must be registed for the web site to request a letsencrypt certificate.";
            }

            return View(model);
        }

        public ActionResult Install()
        {
            SetViewBagHostnames();
            var emailSettings = SettingsStore.Instance.Load().FirstOrDefault(s => s.Name == "email");
            string email = string.Empty;
            if (emailSettings != null)
            {
                email = emailSettings.Value;
            }
            return View(new RequestAndInstallModel() {
                Email = email
            }
            );
        }

        private void SetViewBagHostnames()
        {
            var settings = new AppSettingsAuthConfig();
            var client = ArmHelper.GetWebSiteManagementClient(settings);

            var site = client.Sites.GetSite(settings.ResourceGroupName, settings.WebAppName);
            var model = new HostnameModel();
            ViewBag.HostNames = site.HostNames.Where(s => !s.EndsWith("azurewebsites.net")).Select(s => new SelectListItem()
            {
                Text = s,
                Value = s
            });
        }

        [HttpPost]
        public ActionResult Install(RequestAndInstallModel model)
        {
            if (ModelState.IsValid)
            {
                var s = SettingsStore.Instance.Load();
                s.Add(new LetsEncrypt.SiteExtension.Models.SettingEntry()
                {
                    Name = "email",
                    Value = model.Email
                });
                var baseUri = model.UseStaging == false ? "https://acme-v01.api.letsencrypt.org/" : "https://acme-staging.api.letsencrypt.org/";
                s.Add(new LetsEncrypt.SiteExtension.Models.SettingEntry()
                {
                    Name = "baseUri",
                    Value = baseUri
                });
                SettingsStore.Instance.Save(s);
                var settings = new AppSettingsAuthConfig();
                var target = new Target()
                {
                    ClientId = settings.ClientId,
                    ClientSecret = settings.ClientSecret,
                    Email = model.Email,
                    Host = model.Hostnames.First(),
                    WebAppName = settings.WebAppName,
                    ResourceGroupName = settings.ResourceGroupName,
                    SubscriptionId = settings.SubscriptionId,
                    Tenant = settings.Tenant,
                    BaseUri = baseUri,
                    ServicePlanResourceGroupName = settings.ServicePlanResourceGroupName,
                    AlternativeNames = model.Hostnames.Skip(1).ToList(),
                    UseIPBasedSSL = settings.UseIPBasedSSL
                };
                var thumbprint = CertificateManager.RequestAndInstallInternal(target);
                if (thumbprint != null)
                    return RedirectToAction("Hostname", new { id = thumbprint });
            }
            SetViewBagHostnames();
            return View(model);
        }

        public ActionResult AddHostname()
        {
            var settings = new AppSettingsAuthConfig();
            using (var client = ArmHelper.GetWebSiteManagementClient(settings))
            {
                var s = client.Sites.GetSite(settings.ResourceGroupName, settings.WebAppName);
                foreach (var hostname in settings.Hostnames)
                {
                    client.Sites.CreateOrUpdateSiteHostNameBinding(settings.ResourceGroupName, settings.WebAppName, hostname, new Microsoft.Azure.Management.WebSites.Models.HostNameBinding()
                    {
                        CustomHostNameDnsRecordType = Microsoft.Azure.Management.WebSites.Models.CustomHostNameDnsRecordType.CName,
                        HostNameType = Microsoft.Azure.Management.WebSites.Models.HostNameType.Verified,
                        SiteName = settings.WebAppName,
                        Location = s.Location
                    });
                }
            }
            return View();
        }      

        public ActionResult CreateServicePrincipal()
        {
            var head = Request.Headers.GetValues(Utils.X_MS_OAUTH_TOKEN).FirstOrDefault();

            var client = new SubscriptionClient(new TokenCredentials(head));
            client.SubscriptionId = Guid.NewGuid().ToString();
            var tenants = client.Tenants.List();

            
            var subs = client.Subscriptions.List();
            var cookie = ARMOAuthModule.ReadOAuthTokenCookie(HttpContext.ApplicationInstance);

            //var graphToken = AADOAuth2AccessToken.GetAccessTokenByRefreshToken(cookie.TenantId, cookie.refresh_token, "https://graph.windows.net/");

            var settings = ActiveDirectoryServiceSettings.Azure;
            var authContext = new AuthenticationContext(settings.AuthenticationEndpoint + "common");
            var graphToken = authContext.AcquireToken("https://management.core.windows.net/", new ClientCredential("d1b853e2-6e8c-4e9e-869d-60ce913a280c", "hVAAmWMFjX0Z0T4F9JPlslfg8roQNRHgIMYIXAIAm8s="));


            var graphClient = new GraphRbacManagementClient(new TokenCredentials(graphToken.AccessToken));

            graphClient.SubscriptionId = subs.FirstOrDefault().SubscriptionId;
            graphClient.TenantID = tenants.FirstOrDefault().TenantId;
            //var servicePrincipals = graphClient.ServicePrincipal.List();
            try
            {
                var res = graphClient.Application.Create(new Microsoft.Azure.Graph.RBAC.Models.ApplicationCreateParameters()
                {
                    DisplayName = "Test Application created by ARM",
                    Homepage = "https://test.sjkp.dk",
                    AvailableToOtherTenants = false,
                    IdentifierUris = new string[] { "https://absaad12312.sjkp.dk" },
                    ReplyUrls = new string[] { "https://test.sjkp.dk" },
                    PasswordCredentials = new PasswordCredential[] { new PasswordCredential() {
                    EndDate = DateTime.UtcNow.AddYears(1),
                    KeyId = Guid.NewGuid().ToString(),
                    Value = "s3nheiser",
                    StartDate = DateTime.UtcNow
                } },
                });

            }
            catch (CloudException ex)
            {
                var s = ex.Body.Message;
                var s2 = ex.Response.Content.AsString();

            }

            return View();
        }
    }
}