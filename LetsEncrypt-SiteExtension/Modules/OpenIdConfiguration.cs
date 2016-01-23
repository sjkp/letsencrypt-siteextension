using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;

namespace ARMExplorer.Modules
{
    [DataContract]
    public class OpenIdConfiguration
    {
        private const string OpenIdConfigurationUrl = "https://login.windows.net/common/.well-known/openid-configuration";
        private static readonly DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(OpenIdConfiguration));
        private static OpenIdConfiguration _current = null;
        private static TimeSpan _downloadInterval = TimeSpan.FromHours(6);
        private static DateTime _lastAttemptTime;

        private OpenIdIssuerKeys _issuerKeys;

        [DataMember(Name = "issuer")]
        public string Issuer { get; set; }

        [DataMember(Name = "authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }

        [DataMember(Name = "token_endpoint")]
        public string TokenEndpoint { get; set; }

        [DataMember(Name = "token_endpoint_auth_methods_supported")]
        public string[] TokenEndpointAuthMethodsSupported { get; set; }

        [DataMember(Name = "jwks_uri")]
        public string JwksUri { get; set; }

        [DataMember(Name = "response_types_supported")]
        public string[] ResponseTypesSupported { get; set; }

        [DataMember(Name = "subject_types_supported")]
        public string[] SubjectTypesSupported { get; set; }

        [DataMember(Name = "id_token_signing_alg_values_supported")]
        public string[] IdTokenSigningAlgValuesSupported { get; set; }

        [DataMember(Name = "microsoft_multi_refresh_token")]
        public bool MicrosoftMultiRefreshToken { get; set; }

        [DataMember(Name = "check_session_iframe")]
        public string CheckSessionIframe { get; set; }

        [DataMember(Name = "end_session_endpoint")]
        public string EndSessionEndpoint { get; set; }

        public OpenIdIssuerKeys IssuerKeys
        {
            get
            {
                if (_issuerKeys == null)
                {
                    _issuerKeys = OpenIdIssuerKeys.Download(this.JwksUri);
                }
                return _issuerKeys;
            }
            set
            {
                _issuerKeys = value;
            }
        }

        public static OpenIdConfiguration Current
        {
            get
            {
                // no cache or _downloadInterval has passed since _lastAttemptTime
                if (_current == null || _lastAttemptTime.Add(_downloadInterval) < DateTime.UtcNow)
                {
                    // Only attempt to download if settings exist.
                    if (!String.IsNullOrEmpty(OpenIdConfigurationUrl))
                    {
                        // set _lastAttemptTime so there is only one download at most.
                        // this is loosly caching (prefer stale over throwing exception).  
                        // if failed, attempt again in the next interval.
                        _lastAttemptTime = DateTime.UtcNow;

                        try
                        {
                            OpenIdConfiguration current = Download(OpenIdConfigurationUrl);
                            OpenIdIssuerKeys keys = (current != null) ? current.IssuerKeys : null;
                            if (keys != null && keys.Keys != null && keys.Keys.Length > 0)
                            {
                                _current = current;
                            }
                        }
                        catch (Exception)
                        {
                            if (_current == null)
                            {
                                throw;
                            }
                        }
                    }
                }

                return _current;
            }
            set
            {
                _current = value;
                _lastAttemptTime = DateTime.UtcNow;
            }
        }

        public static OpenIdConfiguration Download(string url)
        {
            var request = WebRequest.Create(url);
            using (var response = request.GetResponse())
            {
                return (OpenIdConfiguration)_serializer.ReadObject(response.GetResponseStream());
            }
        }

        public string GetAuthorizationEndpoint(string tenantId)
        {
            if (!AuthorizationEndpoint.Contains("/common/"))
            {
                throw new InvalidOperationException("Invalid authorization_endpoint: " + AuthorizationEndpoint);
            }

            return String.IsNullOrEmpty(tenantId) ? AuthorizationEndpoint : AuthorizationEndpoint.Replace("/common/", String.Format("/{0}/", tenantId));
        }

        public string GetTokenEndpoint(string tenantId)
        {
            if (!TokenEndpoint.Contains("/common/"))
            {
                throw new InvalidOperationException("Invalid token_endpoint: " + TokenEndpoint);
            }

            return String.IsNullOrEmpty(tenantId) ? TokenEndpoint : TokenEndpoint.Replace("/common/", String.Format("/{0}/", tenantId));
        }
    }

    [DataContract]
    public class OpenIdIssuerKeys
    {
        private static readonly DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(OpenIdIssuerKeys));

        [DataMember(Name = "keys")]
        public OpenIdIssuerKey[] Keys { get; set; }

        public static OpenIdIssuerKeys Download(string url)
        {
            var request = WebRequest.Create(url);
            using (var response = request.GetResponse())
            {
                return (OpenIdIssuerKeys)_serializer.ReadObject(response.GetResponseStream());
            }
        }
    }

    [DataContract]
    public class OpenIdIssuerKey
    {
        private SecurityToken[] _securityTokens;

        [DataMember(Name = "kty")]
        public string KeyType { get; set; }

        [DataMember(Name = "use")]
        public string Usage { get; set; }

        [DataMember(Name = "x5t")]
        public string X509Thumbprint { get; set; }

        [DataMember(Name = "x5c")]
        public string[] X509RawData { get; set; }

        public SecurityToken[] GetSecurityTokens()
        {
            if (_securityTokens == null)
            {
                var list = new List<SecurityToken>();
                foreach (string rawData in this.X509RawData)
                {
                    var cer = new X509Certificate2(Convert.FromBase64String(rawData));
                    list.Add(new X509SecurityToken(cer));
                }

                _securityTokens = list.ToArray();
            }

            return _securityTokens;
        }
    }
}