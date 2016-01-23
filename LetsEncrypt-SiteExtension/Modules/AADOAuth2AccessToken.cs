using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ARMExplorer.Modules
{
    public class AADOAuth2AccessToken
    {
        public static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        //public string token_type { get; set; }
        //public string expires_in { get; set; }
        public string expires_on { get; set; }
        public string resource { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        //public string scope { get; set; }
        //public string id_token { get; set; }

        public string TenantId { get; set; }

        public static AADOAuth2AccessToken GetAccessTokenByCode(string tenantId, string code, string redirectUri)
        {
            // "token_endpoint":"https://login.windows-ppe.net/common/oauth2/token"
            var tokenRequestUri = OpenIdConfiguration.Current.GetTokenEndpoint(tenantId);

            var payload = new StringBuilder("grant_type=authorization_code");
            payload.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(redirectUri));
            payload.AppendFormat("&code={0}", WebUtility.UrlEncode(code));
            payload.AppendFormat("&client_id={0}", WebUtility.UrlEncode(ARMOAuthModule.AADClientId));
            payload.AppendFormat("&client_secret={0}", WebUtility.UrlEncode(ARMOAuthModule.AADClientSecret));

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("client-request-id", Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("User-Agent", "ManagePortal");

                using (var response = client.PostAsync(tokenRequestUri, content).Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw HandleOAuthError(response, tokenRequestUri);
                    }

                    var result = response.Content.ReadAsAsync<AADOAuth2AccessToken>().Result;
                    result.TenantId = tenantId;
                    return result;
                }
            }
        }

        public static AADOAuth2AccessToken GetAccessTokenByRefreshToken(string tenantId, string refreshToken, string resource)
        {
            // "token_endpoint":"https://login.windows-ppe.net/common/oauth2/token"
            var tokenRequestUri = OpenIdConfiguration.Current.GetTokenEndpoint(tenantId);

            var payload = new StringBuilder("grant_type=refresh_token");
            payload.AppendFormat("&refresh_token={0}", WebUtility.UrlEncode(refreshToken));
            payload.AppendFormat("&client_id={0}", WebUtility.UrlEncode(ARMOAuthModule.AADClientId));
            payload.AppendFormat("&client_secret={0}", WebUtility.UrlEncode(ARMOAuthModule.AADClientSecret));
            payload.AppendFormat("&resource={0}", WebUtility.UrlEncode(resource));

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("client-request-id", Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("User-Agent", "ManagePortal");

                using (var response = client.PostAsync(tokenRequestUri, content).Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw HandleOAuthError(response, tokenRequestUri);
                    }

                    var result = response.Content.ReadAsAsync<AADOAuth2AccessToken>().Result;
                    if (String.IsNullOrEmpty(result.refresh_token))
                    {
                        result.refresh_token = refreshToken;
                    }

                    result.TenantId = tenantId;
                    return result;
                }
            }
        }

        // valid for at least 10 mins
        public bool IsValid()
        {
            var secs = Int32.Parse(expires_on);
            return EpochTime.AddSeconds(secs) > DateTime.UtcNow.AddMinutes(10);
        }

        public byte[] ToBytes()
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(AADOAuth2AccessToken));
                serializer.WriteObject(stream, this);
                return stream.ToArray();
            }
        }

        public static AADOAuth2AccessToken FromBytes(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var serializer = new DataContractJsonSerializer(typeof(AADOAuth2AccessToken));
                return (AADOAuth2AccessToken)serializer.ReadObject(stream);
            }
        }

        static Exception HandleOAuthError(HttpResponseMessage response, string requestUri)
        {
            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
            {
                var error = response.Content.ReadAsAsync<AADOAuth2Error>().Result;
                if (error != null && !String.IsNullOrEmpty(error.error_description))
                {
                    return new InvalidOperationException(String.Format("Failed with {0}  POST {1}", error.error_description, requestUri));
                }
            }

            return new InvalidOperationException(String.Format("Failed with {0}  POST {1}", response.StatusCode, requestUri));
        }

        public class AADOAuth2Error
        {
            public string error { get; set; }
            public string error_description { get; set; }
        }
    }
}