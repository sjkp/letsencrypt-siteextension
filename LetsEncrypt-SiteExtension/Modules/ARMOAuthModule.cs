using ARMExplorer.Controllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel;
using System.IdentityModel.Selectors;
using System.IdentityModel.Services;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;

namespace ARMExplorer.Modules
{
    public class ARMOAuthModule : IHttpModule
    {
        public const string ManagementResource = "https://management.core.windows.net/";
        public const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        public const string NonceClaimType = "nonce";
        public const string OAuthTokenCookie = "OAuthToken";
        public const string DeleteCookieFormat = "{0}=deleted; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT";
        public const int CookieChunkSize = 2000;

        public static readonly CookieTransform[] DefaultCookieTransforms = new CookieTransform[]
        {
            new DeflateCookieTransform(),
            new MachineKeyTransform()
        };

        public static string AADClientId
        {
            get { return "d1b853e2-6e8c-4e9e-869d-60ce913a280c"; }//7f257375-267c-4431-afe0-12e4bcf4ab17 Environment.GetEnvironmentVariable("AADClientId"); }
        }

        public static string AADClientSecret
        {
            get { return "hVAAmWMFjX0Z0T4F9JPlslfg8roQNRHgIMYIXAIAm8s=";} //"cUj20YGncR+5Xcjh059XnvV2vQPKRkAFmqExGrHqT9A= "; }//Environment.GetEnvironmentVariable("AADClientSecret"); }
        }

        public bool Enabled
        {
            get { return !String.IsNullOrEmpty(AADClientId) && !String.IsNullOrEmpty(AADClientSecret); }
        }

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.AuthenticateRequest += AuthenticateRequest;
            context.BeginRequest += BeginRequest;
            context.PreSendRequestHeaders += PreRequestHeaders;
        }

        public void PreRequestHeaders(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            application.Response.Headers["instance-name"] = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? application.Request.Url.Host;
        }

        private bool TryGetTenantForSubscription(string subscriptionId, out string tenantId)
        {
            tenantId = string.Empty;
            HyakUtils.CSMUrl = HyakUtils.CSMUrl ?? Utils.GetCSMUrl(HttpContext.Current.Request.Url.Host);
            var requestUri = string.Format(Utils.subscriptionTemplate, HyakUtils.CSMUrl, subscriptionId, Utils.CSMApiVersion);
            var request = WebRequest.CreateHttp(requestUri);
            using (var response = request.GetResponseWithoutExceptions())
            {
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    Trace.TraceError(string.Format("Expected status {0} != {1} GET {2}", HttpStatusCode.Unauthorized, response.StatusCode, requestUri));
                    return false;
                }

                var header = response.Headers["WWW-Authenticate"];
                if (header == null || string.IsNullOrEmpty(header))
                {
                    Trace.TraceError(string.Format("Missing WWW-Authenticate response header GET {0}", requestUri));
                    return false;
                }

                // WWW-Authenicate: Bearer authorization_uri="https://login.windows.net/{tenantId}", error="invalid_token", error_description="The access token is missing or invalid."
                var index = header.IndexOf("authorization_uri=", StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                {
                    Trace.TraceError(string.Format("Invalid WWW-Authenticate response header {0} GET {1}", header, requestUri));
                    return false;
                }

                tenantId =
                    header.Substring(index).Split(new[] { '\"', '=' }, StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1)
                    .Take(1)
                    .Select(s => new Uri(s).AbsolutePath.Trim('/'))
                    .First();
                return !string.IsNullOrEmpty(tenantId);
            }
        }

        private bool TryGetCorrectTenant(out string correctTenant)
        {
            correctTenant = string.Empty;
            var path = HttpContext.Current.Request.RawUrl;
            var index = path.IndexOf("/subscriptions/", StringComparison.OrdinalIgnoreCase);

            if (index == -1) return false;

            var subscription = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();

            if (subscription == null) return false;

            return TryGetTenantForSubscription(subscription, out correctTenant);
        }

        private void PutOnCorrectTenant(string currentTenant)
        {
            if (string.IsNullOrEmpty(currentTenant)) return;

            string correctTenant;
            if (TryGetCorrectTenant(out correctTenant))
            {
                if (!string.IsNullOrEmpty(currentTenant) && !correctTenant.Equals(currentTenant, StringComparison.OrdinalIgnoreCase))
                {
                    HttpContext.Current.Response.Redirect(string.Format("/api/tenants/{0}?cx={1}", correctTenant, WebUtility.UrlEncode(HttpContext.Current.Request.RawUrl)), endResponse: true);
                }
            }
        }

        public void BeginRequest(object sender, EventArgs e)
        {
            try
            {
                var application = (HttpApplication)sender;
                if (!application.Request.Url.IsLoopback)
                {
                    PutOnCorrectTenant(application.Request.Headers["X-MS-OAUTH-TENANTID"]);
                }
            }
            catch (Exception ex)
            {
                //TelemetryHelper.LogException(ex);
                //TODO
            }
        }

        public void AuthenticateRequest(object sender, EventArgs e)
        {
            ClaimsPrincipal principal = null;
            var application = (HttpApplication)sender;
            var request = application.Request;
            var response = application.Response;

            // only perform authentication if localhost
            if (!request.Url.IsLoopback)
            {
                var displayName = request.Headers["X-MS-CLIENT-DISPLAY-NAME"];
                var principalName = request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"];
                if (!string.IsNullOrWhiteSpace(principalName) ||
                    !string.IsNullOrWhiteSpace(displayName))
                {
                    principal = new GenericPrincipal(new GenericIdentity(principalName ?? displayName), new[] { "User" });
                }
                else
                {
                    principal = new ClaimsPrincipal(new ClaimsIdentity("SCM"));
                }
                HttpContext.Current.User = principal;
                Thread.CurrentPrincipal = principal;
                return;
            }

            response.Headers["Strict-Transport-Security"] = "max-age=0";

            if (request.Url.Scheme != "https")
            {
                response.Redirect(String.Format("https://{0}{1}", request.Url.Authority, request.Url.PathAndQuery), endResponse: true);
                return;
            }

            if (request.Url.PathAndQuery.StartsWith("/logout", StringComparison.OrdinalIgnoreCase))
            {
                RemoveSessionCookie(application);

                var logoutUrl = GetLogoutUrl(application);
                response.Redirect(logoutUrl, endResponse: true);
                return;
            }

            string tenantId;
            if (SwitchTenant(application, out tenantId))
            {
                RemoveSessionCookie(application);

                var loginUrl = GetLoginUrl(application, tenantId, "/token");
                response.Redirect(loginUrl, endResponse: true);
                return;
            }

            var id_token = request.Form["id_token"];
            var code = request.Form["code"];
            var state = request.Form["state"];

            if (!String.IsNullOrEmpty(id_token) && !String.IsNullOrEmpty(code))
            {
                principal = AuthenticateIdToken(application, id_token);
                var tenantIdClaim = principal.Claims.FirstOrDefault(c => c.Type == TenantIdClaimType);
                if (tenantIdClaim == null)
                {
                    throw new InvalidOperationException("Missing tenantid claim");
                }

                var base_uri = request.Url.GetLeftPart(UriPartial.Authority);
                var redirect_uri = base_uri + "/manage";
                var token = AADOAuth2AccessToken.GetAccessTokenByCode(tenantIdClaim.Value, code, redirect_uri);
                WriteOAuthTokenCookie(application, token);
                response.Redirect(base_uri + state, endResponse: true);
                return;
            }
            else
            {
                var token = ReadOAuthTokenCookie(application);
                if (token != null)
                {
                    if (!token.IsValid())
                    {
                        token = AADOAuth2AccessToken.GetAccessTokenByRefreshToken(token.TenantId, token.refresh_token, ManagementResource);
                        WriteOAuthTokenCookie(application, token);
                    }

                    principal = new ClaimsPrincipal(new ClaimsIdentity("AAD"));
                    request.ServerVariables["HTTP_X_MS_OAUTH_TOKEN"] = token.access_token;
                }
            }

            if (principal == null)
            {
                var loginUrl = GetLoginUrl(application);
                response.Redirect(loginUrl, endResponse: true);
                return;
            }

            HttpContext.Current.User = principal;
            Thread.CurrentPrincipal = principal;
        }

        public static string GetLoginUrl(HttpApplication application, string tenantId = null, string state = null)
        {
            const string scope = "user_impersonation openid";
            const string site_id = "500879";

            var config = OpenIdConfiguration.Current;
            var request = application.Context.Request;
            var response_type = "id_token code";
            var issuerAddress = config.GetAuthorizationEndpoint(tenantId);
            var redirect_uri = request.Url.GetLeftPart(UriPartial.Authority) + "/manage";
            var client_id = AADClientId;
            var nonce = GenerateNonce();
            var response_mode = "form_post";

            StringBuilder strb = new StringBuilder();
            strb.Append(issuerAddress);
            strb.AppendFormat("?response_type={0}", WebUtility.UrlEncode(response_type));
            strb.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(redirect_uri));
            strb.AppendFormat("&client_id={0}", WebUtility.UrlEncode(client_id));
            strb.AppendFormat("&resource={0}", WebUtility.UrlEncode(ManagementResource));
            strb.AppendFormat("&scope={0}", WebUtility.UrlEncode(scope));
            strb.AppendFormat("&nonce={0}", WebUtility.UrlEncode(nonce));
            strb.AppendFormat("&site_id={0}", WebUtility.UrlEncode(site_id));
            strb.AppendFormat("&response_mode={0}", WebUtility.UrlEncode(response_mode));
            strb.AppendFormat("&state={0}", WebUtility.UrlEncode(state ?? request.Url.PathAndQuery));

            return strb.ToString();
        }

        public static string GetLogoutUrl(HttpApplication application)
        {
            var config = OpenIdConfiguration.Current;
            var request = application.Context.Request;
            //var redirect_uri = new Uri(request.Url, LogoutComplete);

            StringBuilder strb = new StringBuilder();
            strb.Append(config.EndSessionEndpoint);
            //strb.AppendFormat("?post_logout_redirect_uri={0}", WebUtility.UrlEncode(redirect_uri.AbsoluteUri));

            return strb.ToString();
        }

        public static ClaimsPrincipal AuthenticateIdToken(HttpApplication application, string id_token)
        {
            var config = OpenIdConfiguration.Current;
            var handler = new JwtSecurityTokenHandler();
            handler.CertificateValidator = X509CertificateValidator.None;
            if (!handler.CanReadToken(id_token))
            {
                throw new InvalidOperationException("No SecurityTokenHandler can authenticate this id_token!");
            }

            var parameters = new TokenValidationParameters();
            parameters.AllowedAudience = AADClientId;
            // this is just for Saml
            // paramaters.AudienceUriMode = AudienceUriMode.Always;
            parameters.ValidateIssuer = false;

            var tokens = new List<SecurityToken>();
            foreach (var key in config.IssuerKeys.Keys)
            {
                tokens.AddRange(key.GetSecurityTokens());
            }
            parameters.SigningTokens = tokens;

            // validate
            var principal = (ClaimsPrincipal)handler.ValidateToken(id_token, parameters);

            // verify nonce
            VerifyNonce(principal.FindFirst(NonceClaimType).Value);

            return principal;
        }

        public static bool SwitchTenant(HttpApplication application, out string tenantId)
        {
            tenantId = null;

            var request = application.Request;
            if (request.Url.PathAndQuery.StartsWith("/api/tenants", StringComparison.OrdinalIgnoreCase))
            {
                var parts = request.Url.PathAndQuery.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    tenantId = parts[2];
                }
            }

            return tenantId != null;
        }

        public static byte[] EncodeCookie(AADOAuth2AccessToken token)
        {
            var bytes = token.ToBytes();
            for (int i = 0; i < DefaultCookieTransforms.Length; ++i)
            {
                bytes = DefaultCookieTransforms[i].Encode(bytes);
            }
            return bytes;
        }

        public static AADOAuth2AccessToken DecodeCookie(byte[] bytes)
        {
            try
            {
                for (int i = DefaultCookieTransforms.Length - 1; i >= 0; --i)
                {
                    bytes = DefaultCookieTransforms[i].Decode(bytes);
                }
                return AADOAuth2AccessToken.FromBytes(bytes);
            }
            catch (Exception)
            {
                // bad cookie
                return null;
            }
        }

        // NOTE: generate nonce
        public static string GenerateNonce()
        {
            return Guid.NewGuid().ToString();
        }

        // NOTE: verify nonce
        public static void VerifyNonce(string nonce)
        {
        }

        public static AADOAuth2AccessToken ReadOAuthTokenCookie(HttpApplication application)
        {
            var request = application.Context.Request;

            // read oauthtoken cookie
            var cookies = request.Cookies;
            var strb = new StringBuilder();
            int index = 0;
            while (true)
            {
                var cookieName = OAuthTokenCookie;
                if (index > 0)
                {
                    cookieName += index.ToString(CultureInfo.InvariantCulture);
                }

                var cookie = cookies[cookieName];
                if (cookie == null)
                {
                    break;
                }

                strb.Append(cookie.Value);
                ++index;
            }

            if (strb.Length == 0)
            {
                return null;
            }

            var bytes = Convert.FromBase64String(strb.ToString());
            var oauthToken = DecodeCookie(bytes);
            if (oauthToken == null || !oauthToken.IsValid())
            {
                try
                {
                    if (oauthToken != null)
                    {
                        oauthToken = AADOAuth2AccessToken.GetAccessTokenByRefreshToken(oauthToken.TenantId, oauthToken.refresh_token, oauthToken.resource);
                    }
                }
                catch (Exception)
                {
                    oauthToken = null;
                }

                if (oauthToken == null)
                {
                    RemoveSessionCookie(application);

                    return null;
                }

                WriteOAuthTokenCookie(application, oauthToken);
            }

            return oauthToken;
        }

        public static void WriteOAuthTokenCookie(HttpApplication application, AADOAuth2AccessToken oauthToken)
        {
            var request = application.Context.Request;
            var response = application.Context.Response;

            var bytes = EncodeCookie(oauthToken);
            var cookie = Convert.ToBase64String(bytes);
            var chunkCount = cookie.Length / CookieChunkSize + (cookie.Length % CookieChunkSize == 0 ? 0 : 1);
            for (int i = 0; i < chunkCount; ++i)
            {
                var setCookie = new StringBuilder();
                setCookie.Append(OAuthTokenCookie);
                if (i > 0)
                {
                    setCookie.Append(i.ToString(CultureInfo.InvariantCulture));
                }

                setCookie.Append('=');

                int startIndex = i * CookieChunkSize;
                setCookie.Append(cookie.Substring(startIndex, Math.Min(CookieChunkSize, cookie.Length - startIndex)));
                setCookie.Append("; path=/; secure; HttpOnly");
                response.Headers.Add("Set-Cookie", setCookie.ToString());
            }

            var cookies = request.Cookies;
            var index = chunkCount;
            while (true)
            {
                var cookieName = OAuthTokenCookie;
                if (index > 0)
                {
                    cookieName += index.ToString(CultureInfo.InvariantCulture);
                }

                if (cookies[cookieName] == null)
                {
                    break;
                }

                // remove old cookie
                response.Headers.Add("Set-Cookie", String.Format(DeleteCookieFormat, cookieName));
                ++index;
            }
        }

        public static void RemoveSessionCookie(HttpApplication application)
        {
            var request = application.Context.Request;
            var response = application.Context.Response;

            var cookies = request.Cookies;
            foreach (string name in new[] { OAuthTokenCookie })
            {
                int index = 0;
                while (true)
                {
                    string cookieName = name;
                    if (index > 0)
                    {
                        cookieName += index.ToString(CultureInfo.InvariantCulture);
                    }

                    if (cookies[cookieName] == null)
                    {
                        break;
                    }

                    // remove old cookie
                    response.Headers.Add("Set-Cookie", String.Format(DeleteCookieFormat, cookieName));
                    ++index;
                }
            }
        }
    }
}